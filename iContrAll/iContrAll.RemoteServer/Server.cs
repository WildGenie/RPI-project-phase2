using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace iContrAll.RemoteServer
{
    class Server
    {
        private TcpListener raspberryListener;
        private TcpListener clientListener;
        private Thread raspberryListenerThread;
        private Thread clientListenerThread;
        private int raspberryPort;
        private int clientPort;
        //private List<ServiceHandler> clientList = new List<ServiceHandler>();

        public Server(int raspberryPort = 1122, int clientPort = 1123)
        {
            this.raspberryPort = raspberryPort;
            this.clientPort = clientPort;
            this.raspberryListener = new TcpListener(IPAddress.Any, this.raspberryPort);
            this.clientListener = new TcpListener(IPAddress.Any, this.clientPort);

            this.clientListenerThread = new Thread(new ThreadStart(ListenForClients));
            this.clientListenerThread.Start();

            this.raspberryListenerThread = new Thread(new ThreadStart(ListenForRaspberrys));
            this.raspberryListenerThread.Start();
        }

        private void ListenForRaspberrys()
        {
            try
            {
                this.raspberryListener.Start();
                Console.WriteLine("Server is listening on port {0}...", this.raspberryListener.LocalEndpoint);

                while (true)
                {
                    //blocks until a client has connected to the server
                    TcpClient raspberry = this.raspberryListener.AcceptTcpClient();
                    Console.WriteLine("Raspberry connected: {0}", raspberry.Client.RemoteEndPoint);

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(raspberry);

                    //ServiceHandler sh = new ServiceHandler(client);
                    //sh.RemoveClient += RemoveAClient;

                    //// TODO: start() a servicehandlernek
                    //lock (clientListSyncObject)
                    //{
                    //    clientList.Add(sh);
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ListenForClients()");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void ListenForClients()
        {
            try
            {
                this.clientListener.Start();
                Console.WriteLine("Server is listening on port {0}...", this.clientListener.LocalEndpoint);

                while (true)
                {
                    //blocks until a client has connected to the server
                    TcpClient client = this.clientListener.AcceptTcpClient();
                    Console.WriteLine("Client connected: {0} at {1}", client.Client.RemoteEndPoint, DateTime.Now);

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Start(client);

                    //ServiceHandler sh = new ServiceHandler(client);
                    //sh.RemoveClient += RemoveAClient;

                    //// TODO: start() a servicehandlernek
                    //lock (clientListSyncObject)
                    //{
                    //    clientList.Add(sh);
                    //}
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception in ListenForClients()");
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
            }
        }

        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();

            byte[] message = new byte[16536];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 16536);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0)
                {
                    //the client has disconnected from the server
                    break;
                }

                //message has successfully been received
                
                Console.WriteLine("Message: " + Encoding.UTF8.GetString(message, 0, bytesRead)+ " " + DateTime.Now);
                //for (int i = 0; i<bytesRead; i++)
                //{
                //    Console.Write(message[i] + "|");
                //}
                //Console.WriteLine();
            }

            tcpClient.Close();
        }
    }
}
