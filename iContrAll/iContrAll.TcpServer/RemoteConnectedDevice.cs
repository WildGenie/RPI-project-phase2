using System;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace iContrAll.TcpServer
{
    class RemoteConnectedDevice : IConnectedDevice
    {
        public EndPoint RemoteEndPoint { get; private set; }

        public bool CanRead
        {
            get { return this.tcpClient.GetStream().CanRead; }
        }

        private TcpClient tcpClient;
        private SslStream sslStream;

        public RemoteConnectedDevice(TcpClient client, SslStream sslStream, string remoteEndPoint)
        {
            this.tcpClient = client;
            this.sslStream = sslStream;
            string ip = remoteEndPoint.Substring(0, remoteEndPoint.IndexOf(':'));
            int port = int.Parse(remoteEndPoint.Substring(remoteEndPoint.IndexOf(':')+1));
            this.RemoteEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        }

        public int Read(byte[] buffer, int offset, int size)
        {
            //return this.tcpClient.GetStream().Read(buffer, offset, size);
            return this.sslStream.Read(buffer, offset, size);
        }

        public void Write(byte[] result)
        {
            Console.WriteLine("Sent to remote connection: {0}", Encoding.UTF8.GetString(result));
            this.sslStream.Write(result, 0, result.Length);
        }
        public void SendAsync(SocketAsyncEventArgs asyncEventArgs)
        {
            sslStream.WriteAsync(asyncEventArgs.Buffer, 0, asyncEventArgs.Buffer.Length);
        }

        public void Close()
        {
            this.sslStream.Close();
            this.tcpClient.Close();
        }
    }
}
