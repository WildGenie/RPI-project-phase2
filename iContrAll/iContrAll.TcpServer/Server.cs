using iContrAll.SPIRadio;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;
using System.Collections.Generic;


namespace iContrAll.TcpServer
{
	class Server
	{
		private TcpListener tcpListener;
		private Thread listenThread;
		private int port;
        private List<ServiceHandler> clientList = new List<ServiceHandler>();

        private TcpClient remoteServer;

		public Server(int port)
		{
			Radio.Instance.RadioMessageReveived += ProcessReceivedRadioMessage;

			this.port = port;
			this.tcpListener = new TcpListener(IPAddress.Any, this.port);
            
			this.listenThread = new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
            //try
            //{
            //    this.remoteServer = new TcpClient();
            //    // TODO: ne beégetett cím legyen
            //    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse("79.172.214.136"), 1124);
            //    remoteServer.Connect(serverEndPoint);
            //    string message = "Test raspberry login";
            //    Byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            //    // Get a client stream for reading and writing. 
            //    //  Stream stream = client.GetStream();

            //    NetworkStream stream = remoteServer.GetStream();

            //    // Send the message to the connected TcpServer. 
            //    stream.Write(data, 0, data.Length);

            //    Console.WriteLine("Sent: {0}", message);

            //    // Close everything.
            //    stream.Close();
            //    remoteServer.Close();
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}

            //byte[] basicBytesToSend = Encoding.UTF8.GetBytes("00001111LC10000101x11xxxxxx");
            //byte[] bytesToSend = new byte[basicBytesToSend.Length + 4];
            //byte[] bullshitdimValues = new byte[4] { 100, 100, 0, 0 };

            //Array.Copy(basicBytesToSend, bytesToSend, basicBytesToSend.Length);
            //Array.Copy(bullshitdimValues, 0, bytesToSend, basicBytesToSend.Length, 4);
            //Radio.Instance.SendMessage(bytesToSend);
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


            // 2 csatornás lámpavezérlőre felkészítve
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
                    //stateMsg += states[i].Equals('1') ? '1' : '0';
                    //Console.WriteLine("Broadcast : " + stateMsg);
                    //SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(stateMsg)));
                    
                    //string dimMsg = senderId + targetId + "60" + ;
                    string dimm = "chd" + (i + 1) + "=" + ((dimValues[i] / 100) % 10).ToString() + ((dimValues[i] / 10) % 10).ToString() + (dimValues[i] % 10).ToString();
                    //dimMsg += dimm;
                    //Console.WriteLine("Broadcast : " + dimMsg);
                    //SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(dimMsg)));
                    responseMsg += "&" + dimm;

                    //string powerMsg = senderId + targetId + "60" + "chi" + (i + 1) + "=";
                    string power = "chi" + (i + 1) + "="+((powerValues[i] / 100) % 10).ToString() + ((powerValues[i] / 10) % 10).ToString() + (powerValues[i] % 10).ToString();
                    //powerMsg += power;
                    //Console.WriteLine("Broadcast : " + powerMsg);

                    responseMsg += "&" + power;
                    //SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(powerMsg)));
                }
                Console.WriteLine("SendToAllClient: " + responseMsg);
                SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(responseMsg)));
                
            }
            else Console.WriteLine("Nemjött be!");
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

		private void ListenForClients()
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

                    ServiceHandler sh = new ServiceHandler(client);
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
	}
}
