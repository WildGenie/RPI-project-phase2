using iContrAll.SPIRadio;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Linq;
using System.Text;


namespace iContrAll.TcpServer
{
    class Server
    {
        private TcpListener tcpListener;
        private Thread listenThread;
        private int port;

        private RadioCommunication radio;

        public Server(int port)
        {
            this.radio = new RadioCommunication();
            if(radio.InitRadio())
            {
                Console.WriteLine("Radio init sikeres");
            }
            radio.RadioMessageReveived += ProcessReceivedRadioMessage;

            this.port = port;
            this.tcpListener = new TcpListener(IPAddress.Any, this.port);

            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();
        }

        private void initRadio()
        {
            this.radio = null;
            this.radio = new RadioCommunication();
            if (radio.InitRadio())
            {
                Console.WriteLine("Radio init sikeres");
            }
            radio.RadioMessageReveived += ProcessReceivedRadioMessage;
        }

        private void ProcessReceivedRadioMessage(RadioMessageEventArgs e)
        {
            if (e.ErrorCode == -1)
            {
                this.initRadio();
                return;
            }
            
            if (e.ReceivedMessage == null)
                return;

            Console.WriteLine("Esemény:" + e.ReceivedMessage);

            //// specified for 4 channel light controller
            //string senderId = Encoding.UTF8.GetString(e.ReceivedMessage.Take(8).ToArray());
            //string targetId = Encoding.UTF8.GetString(e.ReceivedMessage.Skip(8).Take(8).ToArray());

            //// drop packages we are not interested
            //if (targetId != System.Configuration.ConfigurationManager.AppSettings["loginid"]) return;

            //string channels = Encoding.UTF8.GetString(e.ReceivedBytes.Skip(11).Take(4).ToArray());

            //byte[] voltageValues = e.ReceivedBytes.Skip(15).Take(4).ToArray();
            //byte[] dimValues = e.ReceivedBytes.Skip(19).Take(4).ToArray();

            //using (var dal = new DataAccesLayer())
            //{
            //    // TODO: minden tulajdonságot felvenni.
            //    dal.UpdateDeviceState(senderId);
            //}

        }

        private void ListenForClients()
        {
            this.tcpListener.Start();
            Console.WriteLine("Server is listening on port {0}...", this.tcpListener.LocalEndpoint);

            while (true)
            {
                //blocks until a client has connected to the server
                TcpClient client = this.tcpListener.AcceptTcpClient();
                Console.WriteLine("Client connected: {0}", client.Client.RemoteEndPoint);

                // TODO: start() a servicehandlernek
                ServiceHandler sh = new ServiceHandler(client, radio);
            }
        }
    }
}
