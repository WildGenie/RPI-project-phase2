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

		public Server(int port)
		{

			Radio.Instance.RadioMessageReveived += ProcessReceivedRadioMessage;

			this.port = port;
			this.tcpListener = new TcpListener(IPAddress.Any, this.port);
            
			this.listenThread = new Thread(new ThreadStart(ListenForClients));
			this.listenThread.Start();
		}

        private void ProcessReceivedRadioMessage(RadioMessageEventArgs e)
        {
            if (e.ErrorCode == -1)
            {
                Console.WriteLine("Radio '-1' error code-dal jött vissza, EXCEPTION az INTERRUPT-BAN!");
                // this.initRadio();
                return;
            }

            if (e.ReceivedBytes == null)
            {
                Console.WriteLine("Esemény, de ReceivedBytes==null");
                return;
            }
            Console.WriteLine("Esemény:" + Encoding.UTF8.GetString(e.ReceivedBytes) + " hossz=" + e.ReceivedBytes.Length);


            string senderId = Encoding.UTF8.GetString(e.ReceivedBytes.Take(8).ToArray());
            string targetId = Encoding.UTF8.GetString(e.ReceivedBytes.Skip(8).Take(8).ToArray());
            Console.WriteLine(senderId + "=>" + targetId);
            // ha nem mihozzánk érkezik az üzenet, eldobjuk
            if (targetId != System.Configuration.ConfigurationManager.AppSettings["loginid"].Substring(2)) return;

            // debughoz
            foreach (var b in e.ReceivedBytes)
            {
                Console.Write(b+"|");
            }
            Console.WriteLine();


            // 2 csatornás lámpavezérlőre felkészítve
            if (senderId.StartsWith("LC1"))
            {
                int chCount = 2;

                string states = Encoding.UTF8.GetString(e.ReceivedBytes.Skip(19).Take(chCount).ToArray());

                Console.WriteLine(senderId + "=>" + targetId + ":" + states);

                byte[] powerValues = e.ReceivedBytes.Skip(19 + chCount).Take(chCount).ToArray();
                byte[] dimValues = e.ReceivedBytes.Skip(19 + 2 * chCount).Take(chCount).ToArray();

                using (var dal = new DataAccesLayer())
                {
                    for (int i = 0; i < chCount; i++)
                    {
                        dal.UpdateDeviceStatus(senderId, i + 1, states[i].Equals('1'), dimValues[i], powerValues[i]);
                    }
                }

                for (int i = 0; i < chCount; i++)
                {
                    string stateMsg = senderId + targetId+ "60" + "chs" + (i + 1) + "=";
                    stateMsg += states[i].Equals('1') ? '1' : '0';
                    Console.WriteLine("Response : " + stateMsg);
                    SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(stateMsg)));

                    string dimMsg = senderId + targetId + "60" + "chd" + (i + 1) + "=";
                    dimMsg += dimValues[i];

                    Console.WriteLine("Response : " + dimMsg);
                    SendToAllClient(BuildMessage(1, Encoding.UTF8.GetBytes(dimMsg)));
                }
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
