using iContrAll.SPIRadio;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;
using System.Globalization;

namespace iContrAll.TcpServer
{
	class Server
	{
		private TcpListener tcpListener;
		private Thread listenThread;
		private int port;
        private List<ServiceHandler> clientList = new List<ServiceHandler>();

        private Thread remoteServerThread;
        private TcpClient remoteServer;
        private SslStream sslStream;

        string remoteServerAddress;
        int remoteServerPort;
        string serverCertificateName;
        string certificatePath;
        string certificatePassphrase;

		public Server(int port, string remoteServerAddress, int remoteServerPort,
            string serverCertificateName, string certificatePath, string certificatePassphrase)
		{
            this.remoteServerAddress = remoteServerAddress;
            this.remoteServerPort = remoteServerPort;
            this.serverCertificateName = serverCertificateName;
            this.certificatePassphrase = certificatePassphrase;
            this.certificatePath = certificatePath;

			Radio.Instance.RadioMessageReveived += ProcessReceivedRadioMessage;

			this.port = port;
			this.tcpListener = new TcpListener(IPAddress.Any, this.port);
            
			this.listenThread = new Thread(new ThreadStart(ListenForLocalLANClients));
			this.listenThread.Start();

            this.remoteServerThread = new Thread(new ThreadStart(RemoteServerManaging));
            this.remoteServerThread.Start();
		}

        //private void ProcessReceivedRadioMessage(RadioMessageEventArgs e)
        private void ProcessReceivedRadioMessage(byte[] receivedBytes)
        {
            //if (e.ErrorCode == -1)
            //{
            //    Console.WriteLine("Radio '-1' error code-dal jött vissza, EXCEPTION az INTERRUPT-BAN!");
            //    // this.initRadio();
            //    return;
            //}

            if (receivedBytes == null)
            {
                Console.WriteLine("Esemény, de ReceivedBytes==null");
                return;
            }
            Console.WriteLine("Esemény:" + Encoding.UTF8.GetString(receivedBytes) + " hossz=" + receivedBytes.Length);


            string senderId = Encoding.UTF8.GetString(receivedBytes.Take(8).ToArray());
            string targetId = Encoding.UTF8.GetString(receivedBytes.Skip(8).Take(8).ToArray());
            Console.WriteLine(senderId + "=>" + targetId);
            // ha nem mihozzánk érkezik az üzenet, eldobjuk
            if (targetId != System.Configuration.ConfigurationManager.AppSettings["loginid"].Substring(2)) return;

            //// debughoz
            //foreach (var b in receivedBytes)
            //{
            //    Console.Write(b+"|");
            //}
            Console.WriteLine();


            if (senderId.StartsWith("LC1"))
            {
                int chCount = 4;

                string states = Encoding.UTF8.GetString(receivedBytes.Skip(19).Take(chCount).ToArray());

                Console.WriteLine(senderId + "=>" + targetId + ":" + states);

                byte[] powerValues = receivedBytes.Skip(19 + chCount).Take(chCount).ToArray();
                byte[] dimValues = receivedBytes.Skip(19 + 2 * chCount).Take(chCount).ToArray();

                using (var dal = new DataAccesLayer())
                {
                    for (int i = 0; i < chCount; i++)
                    {
                        dal.UpdateDeviceStatus(senderId, i + 1, states[i].Equals('1'), dimValues[i], powerValues[i]);
                    }
                }

                string responseMsg = senderId + targetId + "60";

                for (int i = 0; i < chCount; i++)
                {
                    // összefűzés
                    if (i != 0) responseMsg += '&';

                    string state = "chs" + (i + 1) + "=" + (states[i].Equals('1') ? '1' : '0');
                    responseMsg += state;
                    
                    string dimm = "chd" + (i + 1) + "=" + ((dimValues[i] / 100) % 10).ToString() + ((dimValues[i] / 10) % 10).ToString() + (dimValues[i] % 10).ToString();
                    
                    responseMsg += "&" + dimm;

                    string power = "chi" + (i + 1) + "="+((powerValues[i] / 100) % 10).ToString() + ((powerValues[i] / 10) % 10).ToString() + (powerValues[i] % 10).ToString();

                    responseMsg += "&" + power;
                }
                Console.WriteLine("SendToAllClient: " + responseMsg);
                SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(responseMsg)));
                
            }
            else
            // redőny
            if (senderId.StartsWith("OC1"))
            {
                int chCount = 2;

                string states = Encoding.UTF8.GetString(receivedBytes.Skip(19).Take(chCount).ToArray());

                Console.WriteLine(senderId + "=>" + targetId + ":" + states);

                byte[] dimValues = receivedBytes.Skip(19 + chCount).Take(chCount).ToArray();
                byte[] powerValues = new byte[] { 0, 0, 0, 0 }; // receivedBytes.Skip(19 + 2 * chCount).Take(chCount).ToArray();

                using (var dal = new DataAccesLayer())
                {
                    for (int i = 0; i < chCount; i++)
                    {
                        dal.UpdateDeviceStatus(senderId, i + 1, states[i].Equals('1'), dimValues[i], powerValues[i]);
                    }
                }

                string responseMsg = senderId + targetId + "50";

                for (int i = 0; i < chCount; i++)
                {
                    // összefűzés
                    if (i != 0) responseMsg += '&';

                    string state = "chs" + (i + 1) + "=" + (states[i].Equals('1') ? '1' : '0');
                    responseMsg += state;

                    string dimm = "chd" + (i + 1) + "=" + ((dimValues[i] / 100) % 10).ToString() + ((dimValues[i] / 10) % 10).ToString() + (dimValues[i] % 10).ToString();

                    responseMsg += "&" + dimm;

                    //string power = "chi" + (i + 1) + "=" + ((powerValues[i] / 100) % 10).ToString() + ((powerValues[i] / 10) % 10).ToString() + (powerValues[i] % 10).ToString();

                    //responseMsg += "&" + power;
                }

                Console.WriteLine("SendToAllClient: " + responseMsg);
                SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(responseMsg)));
            }
        }

        private byte[] BuildMessage(int msgNumber, byte[] message)
        {
            byte[] msgNbrArray = new byte[4];
            Array.Copy(BitConverter.GetBytes(msgNumber), msgNbrArray, msgNbrArray.Length);

            byte[] lengthArray = new byte[4];
            Array.Copy(BitConverter.GetBytes(message.Length), lengthArray, lengthArray.Length);

            byte[] answer = new byte[4 + 4 + message.Length];

            System.Buffer.BlockCopy(msgNbrArray, 0, answer, 0, msgNbrArray.Length);
            System.Buffer.BlockCopy(lengthArray, 0, answer, msgNbrArray.Length, lengthArray.Length);
            System.Buffer.BlockCopy(message, 0, answer, msgNbrArray.Length + lengthArray.Length, message.Length);

            return answer;
        }

        private object clientListSyncObject = new object();

		private void ListenForLocalLANClients()
		{
            try
            {
                this.tcpListener.Start();
                Console.WriteLine("Server is listening on port {0}...", this.tcpListener.LocalEndpoint);

                while (true)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this.tcpListener.AcceptTcpClient();
                    Console.WriteLine("Client connected: {0}", client.Client.RemoteEndPoint);

                    IConnectedDevice connectedDevice = new LocalConnectedDevice(client);

                    ServiceHandler sh = new ServiceHandler(connectedDevice);
                    sh.RemoveClient += RemoveAClient;

                    // TODO: start() a servicehandlernek
                    lock (clientListSyncObject)
                    {
                        clientList.Add(sh);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ListenForClients()");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
		}

        void RemoveAClient(EndPoint ep)
        {
            lock (clientListSyncObject)
            {
                ServiceHandler user = clientList.First(u => u.Endpoint == ep);
                if (user != null)
                    clientList.Remove(user);
            }
        }

        public void SendToAllClient(byte[] bytesToSend)
        {
            var asyncEvent = new SocketAsyncEventArgs();

            asyncEvent.SetBuffer(bytesToSend, 0, bytesToSend.Length);
            //foreach (var i in asyncEvent.Buffer)
            //{
            //    Console.Write(i + "|");
            //}
            Console.WriteLine();

            lock (clientListSyncObject)
            {
                foreach (var c in clientList)
                {
                    c.SendRadioMessage(asyncEvent);
                }
            }
        }

        private const int bufferSize = 32768;

        private void RemoteServerManaging()
        {
            if (!ConnectToRemoteServer()) return;
            var readBuffer = new byte[bufferSize];
            int numberOfBytesRead = -1;
            try
            {
                if (sslStream.CanRead)
                {
                    while (true)
                    {
                        try
                        {
                            numberOfBytesRead = sslStream.Read(readBuffer, 0, bufferSize);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                            continue;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception while reading from socket {0} in Server.RemoteServerManaging", this.remoteServer.Client.RemoteEndPoint);
                            Console.WriteLine(ex.Message);
                            if (ex.InnerException!=null)
                            { Console.WriteLine(ex.InnerException.Message); }

                            break;
                        }

                        if (numberOfBytesRead <= 0)
                        {
                            Console.WriteLine("NumberOfBytesRead: {0} from {1}", numberOfBytesRead, remoteServer.Client.RemoteEndPoint.ToString());
                            break;
                        }

                        Console.WriteLine("Message (length={1}) received from: {0} at {2}", remoteServer.Client.RemoteEndPoint.ToString(), numberOfBytesRead, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

                        byte[] readBytes = readBuffer.Take(numberOfBytesRead).ToArray();

                        foreach (var message in ProcessBuffer(readBytes))
                        {
                            ProcessMessage(message);
                        }
                    }
                }
            }
            finally
            {

                sslStream.Close(); // including clientStream.Close();
                remoteServer.Close();
                Console.WriteLine("RemoteServer zár");
            }
        }

        private List<Message> ProcessBuffer(byte[] readBuffer)
		{
			var returnList = new List<Message>();
			
			// felfűzzük az elejére a maradékot
			byte[] completeBuffer;
			//if (trailingBuffer.Length > 0)
			//{
			//    completeBuffer = new byte[trailingBuffer.Length + readBuffer.Length];
			//    Array.Copy(trailingBuffer, completeBuffer, trailingBuffer.Length);
			//    Array.Copy(readBuffer, 0, completeBuffer, trailingBuffer.Length, readBuffer.Length);
			//}
			//else 
			completeBuffer = readBuffer;

			while (completeBuffer.Length > 0)
			{
				if (completeBuffer.Length<4) break;

				byte[] messageTypeArray = new byte[4];
				Array.Copy(completeBuffer, messageTypeArray, 4);

				int messageType = BitConverter.ToInt32(messageTypeArray, 0);

				bool exists = false;

				// TODO: extract, do it only once!!
				foreach (var e in Enum.GetValues(typeof(MessageType)))
				{
					if ((int)e == messageType)
					{
						exists = true;
						break;
					}
				}

				if (!exists)
				{
					
					Console.WriteLine("Gyanus! {0}", messageType);
                    break;
					
					//if (completeBuffer.Length > 4)
					//{
					//    var temp = completeBuffer;
					//    temp.CopyTo(completeBuffer, 4);
					//    completeBuffer = temp;
					//}
				}

				if (completeBuffer.Length<8) break;

				byte[] messageLengthArray = new byte[4];
				Array.Copy(completeBuffer, 4, messageLengthArray, 0, 4);

				int messageLength = BitConverter.ToInt32(messageLengthArray, 0);

				// TODO: az összes ilyen esetkor (pl. kétszer feljebb) el kell tárolni a trailMessage-ben!
				if(completeBuffer.Length < 8 + messageLength) break;

				byte[] messageArray = new byte[messageLength];
				Array.Copy(completeBuffer, 8, messageArray, 0, messageLength);
				
				string message = Encoding.UTF8.GetString(messageArray);

				returnList.Add(new Message(messageType, messageLength, message));

				//Console.WriteLine(completeBuffer.Length + " - " + (8 + messageArray.Length));
				if (completeBuffer.Length >= 8 + messageArray.Length) // CONTINUE,
				{
					byte[] tempBuf = new byte[completeBuffer.Length - (8 + messageArray.Length)];
					// Console.WriteLine(completeBuffer.Length + " - " + (8 + messageArray.Length) + " = tempBuf.Length: " + tempBuf.Length);
					Array.Copy(completeBuffer, 8 + messageArray.Length, tempBuf, 0, completeBuffer.Length - (8 + messageArray.Length));
					completeBuffer = tempBuf;
				}
			}

			return returnList;
		}

        private void ProcessMessage(Message m)
		{
			byte messageType = (byte)m.Type;
			int messageLength = m.Length;
			string message = m.Content;

            Console.WriteLine("Message: Type={0}: {1}", (MessageType)messageType, message);

			if (m.Type == MessageType.CreateThreadFor)
            {
                TcpClient remoteConnection = new TcpClient(remoteServerAddress, remoteServerPort);
                
                var sslStreamForClient = new SslStream(
                    remoteConnection.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    new LocalCertificateSelectionCallback(CertificateSelectionCallback)
                );

                X509Certificate cert = new X509Certificate2(certificatePath, certificatePassphrase); //"/home/pi/SslClientTest2/bin/Debug/server.p12", "allcontri");
                X509CertificateCollection certs = new X509CertificateCollection();
                certs.Add(cert);

                // The server name must match the name on the server certificate. 
                try
                {
                    sslStreamForClient.AuthenticateAsClient(serverCertificateName,
                        certs,
                        SslProtocols.Tls,
                        false); // check cert revokation);

                    Console.WriteLine("Connected and authenticated to remoteserver to communicate with {0}", message);
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    remoteServer.Close();
                    return;
                }

                // Signing that this is the thread for the remote device communication
                byte[] messageContent = Encoding.UTF8.GetBytes(message);
                byte[] data = BuildMessage((int)MessageType.CreateThreadFor, messageContent);

                sslStreamForClient.Write(data, 0, data.Length);
                sslStreamForClient.Flush();

                RemoteConnectedDevice rcd = new RemoteConnectedDevice(remoteConnection, sslStreamForClient, message);

                ServiceHandler sh = new ServiceHandler(rcd);
                sh.RemoveClient += RemoveAClient;

                lock (clientListSyncObject)
                {
                    clientList.Add(sh);
                }
            }

		}

        private bool ConnectToRemoteServer()
        {
            try
            {
                this.remoteServer = new TcpClient(remoteServerAddress, remoteServerPort);
                Console.WriteLine("Connected to remoteserver");
                this.sslStream = new SslStream(
                    remoteServer.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    new LocalCertificateSelectionCallback(CertificateSelectionCallback)
                );

                X509Certificate cert = new X509Certificate2(certificatePath, certificatePassphrase); //"/home/pi/SslClientTest2/bin/Debug/server.p12", "allcontri");
                X509CertificateCollection certs = new X509CertificateCollection();
                certs.Add(cert);

                // The server name must match the name on the server certificate. 
                try
                {
                    sslStream.AuthenticateAsClient(serverCertificateName,
                        certs,
                        SslProtocols.Tls,
                        false); // check cert revokation);
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    remoteServer.Close();
                    return false;
                }

                // Sending the device id to the server inside the login message
                byte[] message = Encoding.UTF8.GetBytes(System.Configuration.ConfigurationManager.AppSettings["loginid"]);
                byte[] data = BuildMessage(-1, message);
                //NetworkStream stream = remoteServer.GetStream();
                sslStream.Write(data, 0, data.Length);
                sslStream.Flush();

                return true;
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
                return false;
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                return false;
            }
        }

        private void PingRemoteServer()
        {
            try
            {
                this.sslStream.Write(BuildMessage(-3, null));
                this.sslStream.Flush();
            }
            catch(Exception)
            {
                // we need to reconnect
                ConnectToRemoteServer();
            }
        }

        // The following method is invoked by the RemoteCertificateValidationDelegate. 
        public bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers. 
            // return false;
            return true;
        }

        X509Certificate CertificateSelectionCallback(object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            Console.WriteLine("CertificateSelectionCallback");
            return localCertificates[0];
        }
	}
}
