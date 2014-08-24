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
				return;

			Console.WriteLine("Esemény:" + e.ReceivedBytes);


			string senderId = Encoding.UTF8.GetString(e.ReceivedBytes.Take(8).ToArray());
			string targetId = Encoding.UTF8.GetString(e.ReceivedBytes.Skip(8).Take(8).ToArray());

			// ha nem mihozzánk érkezik az üzenet, eldobjuk
			if (targetId != System.Configuration.ConfigurationManager.AppSettings["loginid"]) return;

			// 4 csatornás lámpavezérlőre felkészítve
			if (senderId.StartsWith("LC1"))
			{
				int chCount = 2;

				string channels = Encoding.UTF8.GetString(e.ReceivedBytes.Skip(11).Take(chCount).ToArray());
				byte[] powerValues = e.ReceivedBytes.Skip(11 + chCount).Take(chCount).ToArray();
				byte[] dimValues = e.ReceivedBytes.Skip(11 + 2* chCount).Take(chCount).ToArray();

				using (var dal = new DataAccesLayer())
				{
					// TODO: minden tulajdonságot felvenni.
					for (int i = 0; i < chCount; i++)
			{
						dal.UpdateDeviceStatus(senderId, i+1, channels[i].Equals('1'), powerValues[i], dimValues[i]);
			}
					
					
				}
		}
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
				new ServiceHandler(client);
			}
		}
	}
}
