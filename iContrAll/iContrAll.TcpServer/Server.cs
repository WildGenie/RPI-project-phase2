using iContrAll.SPIRadio;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

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

        Timer remotePingTimer;

        Timer actionTimer;

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

            this.remotePingTimer = new Timer(PingRemoteServer);
            DateTime now = new RealTimeClock.RealTimeClock().GetDateTime();
            this.actionTimer = new Timer(RunTimedActions, null, 
                                         (60 - now.Second) * 1000 - now.Millisecond, 
                                         60000);
		}

        private void RunTimedActions(object state)
        {
            using(var dal = new DataAccesLayer())
            {
                string id = System.Configuration.ConfigurationManager.AppSettings["loginid"].Substring(2);

                DateTime now = new RealTimeClock.RealTimeClock().GetDateTime();

                foreach (var timer in dal.GetTimers())
                {
                    int n;
                    bool isStartTimeNumeric = int.TryParse(timer.StartTime, out n);
                    bool isEndTimeNumeric = int.TryParse(timer.EndTime, out n);
                    
                    if (isStartTimeNumeric && timer.StartTime.Length == 4)
                    {
                        int hour = 0;
                        int minute = 0;
                        if (int.TryParse(timer.StartTime.Substring(0, 2), out hour) && int.TryParse(timer.StartTime.Substring(2, 2), out minute))
                        {
                            if (hour == now.Hour && minute == now.Minute)
                            {
                                bool uOr1 = true;
                                if (timer.DeviceId.StartsWith("OC1"))
                                {
                                    uOr1 = false;
                                }

                                string message = string.Format("{0}{1}67ch{4}{2}={3}", id, timer.DeviceId, timer.DeviceChannel, uOr1?"1":"u", uOr1?"":"s");

                                RadioHelper.SendCommandOnRadio(message);
                            }
                        }
                    }

                    if (isEndTimeNumeric && timer.EndTime.Length == 4)
                    {
                        int hour = 0;
                        int minute = 0;
                        if (int.TryParse(timer.EndTime.Substring(0, 2), out hour) && int.TryParse(timer.EndTime.Substring(2, 2), out minute))
                        {
                            if (hour == now.Hour && minute == now.Minute)
                            {
                                bool dOr0 = true;
                                if (timer.DeviceId.StartsWith("OC1"))
                                {
                                    dOr0 = false;
                                }

                                string message = string.Format("{0}{1}67ch{4}{2}={3}", id, timer.DeviceId, timer.DeviceChannel, dOr0 ? "0" : "d", dOr0 ? "" : "s");

                                RadioHelper.SendCommandOnRadio(message);

                                dal.RemoveTimer(timer);
                            }
                        }
                    }

                    Thread.Sleep(250);
                }
            }
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

                    string timer = "cht" + (i + 1) + "=" + "X2200";

                    responseMsg += "&" + timer;
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

                    string timer = "cht" + (i + 1) + "=" + "X2200";

                    responseMsg += "&" + timer;

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
            //foreach (var i in asyncEvent.Buffer)
            //{
            //    Console.Write(i + "|");
            //}
            Console.WriteLine();

            lock (clientListSyncObject)
            {
                foreach (var c in clientList)
                {
                    //var asyncEvent = new SocketAsyncEventArgs();
                    //asyncEvent.SetBuffer(bytesToSend, 0, bytesToSend.Length);
                    c.SendRadioMessage(bytesToSend);
                }
            }
        }

        private const int bufferSize = 32768;

        private void RemoteServerManaging()
        {
            // blokkol, amíg nem sikerül csatlakozni
            ConnectToRemoteServer();

            var readBuffer = new byte[bufferSize];
            int numberOfBytesRead = -1;

            while (true)
            {
                try
                {
                    if ((numberOfBytesRead = sslStream.Read(readBuffer, 0, bufferSize)) > 0)
                    {
                        Console.WriteLine("Message (length={1}) received from: {0} at {2}", remoteServer.Client.RemoteEndPoint.ToString(), numberOfBytesRead, new RealTimeClock.RealTimeClock().GetDateTime().ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

                        byte[] readBytes = readBuffer.Take(numberOfBytesRead).ToArray();

                        foreach (var message in ProcessBuffer(readBytes))
                        {
                            ProcessMessage(message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("ZeroBytesRead: {0} from remoteServer {1}", numberOfBytesRead, remoteServer.Client.RemoteEndPoint.ToString());
                        remotePingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        sslStream.Close();
                        remoteServer.Close();
                        Thread.Sleep(10000);
                    }
                }
                //catch (ArgumentOutOfRangeException)
                //{
                //    Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                //    continue;
                //}
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while reading from socket {0} in Server.RemoteServerManaging", this.remoteServer.Client.RemoteEndPoint);
                    Console.WriteLine(ex.Message);
                    if (ex.InnerException != null)
                    { Console.WriteLine(ex.InnerException.Message); }
                    remotePingTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    sslStream.Close();
                    remoteServer.Close();
                    Thread.Sleep(10000);
                }
                finally
                {
                    // blokkol amíg nem csatlakozott újra
                    ConnectToRemoteServer();
                }
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

        private void ConnectToRemoteServer()
        {
            while (this.remoteServer == null || this.remoteServer.Client == null || !this.remoteServer.Connected)
            {
                //if (this.remoteServer == null) this.remoteServer = new TcpClient();
                this.remoteServer = new TcpClient();
                try
                {
                    this.remoteServer.Connect(remoteServerAddress, remoteServerPort);
                    Console.WriteLine("Connected to remoteserver");
                    this.sslStream = new SslStream(
                        remoteServer.GetStream(),
                        false,
                        new RemoteCertificateValidationCallback(ValidateServerCertificate),
                        new LocalCertificateSelectionCallback(CertificateSelectionCallback)
                    );

                    X509Certificate cert = new X509Certificate2(certificatePath, certificatePassphrase);
                    X509CertificateCollection certs = new X509CertificateCollection();
                    certs.Add(cert);

                    // The server name must match the name on the server certificate. 
                    try
                    {
                        sslStream.AuthenticateAsClient(
                            serverCertificateName,
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
                    }

                    // Sending the device id to the server inside the login message
                    byte[] message = Encoding.UTF8.GetBytes(System.Configuration.ConfigurationManager.AppSettings["loginid"]);
                    byte[] data = BuildMessage(-1, message);
                    //NetworkStream stream = remoteServer.GetStream();
                    sslStream.Write(data, 0, data.Length);
                    sslStream.Flush();
                }
                catch (ArgumentNullException e)
                {
                    Console.WriteLine("ArgumentNullException: {0}", e);
                    remoteServer.Close();

                }
                catch (SocketException)
                {
                    Console.WriteLine("SocketException: Cannot connect to remote server.");
                    remoteServer.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in ConnectToRemoteServer():");
                    Console.WriteLine(e.Message);
                    if (e.InnerException!=null)
                    {
                        Console.WriteLine(e.InnerException.Message);
                    }
                    remoteServer.Close();
                }

                if (this.remoteServer == null || this.remoteServer.Client == null || !this.remoteServer.Connected) 
                    Thread.Sleep(10000);

                //try
                //{
                //    if (!this.remoteServer.Connected) Thread.Sleep(10000);
                //}
                //catch(Exception e)
                //{
                //    Thread.Sleep(10000);
                //}
                
            }

            this.remotePingTimer.Change(60000, 60000);
        }

        private void PingRemoteServer(object state)
        {
            try
            {
                Console.WriteLine("Sending ping message");
                this.sslStream.Write(BuildMessage((int)MessageType.PingMessage, new byte[] { }));
                this.sslStream.Flush();
            }
            catch (Exception)
            {
                Console.WriteLine("RemoteServerPing failed");
                // stop the ping
                remotePingTimer.Change(Timeout.Infinite, Timeout.Infinite);
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
