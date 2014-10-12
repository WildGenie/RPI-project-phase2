using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
            // this.RemoteEndPoint = client.Client.RemoteEndPoint;
        }

        public int Read(byte[] buffer, int offset, int size)
        {
            //return this.tcpClient.GetStream().Read(buffer, offset, size);
            return this.sslStream.Read(buffer, offset, size);
        }

        public void Write(byte[] result)
        {
            this.sslStream.Write(result, 0, result.Length);
        }
        public void SendAsync(SocketAsyncEventArgs asyncEventArgs)
        {
            sslStream.WriteAsync(asyncEventArgs.Buffer, 0, asyncEventArgs.Buffer.Length);
        }

        public void Close()
        {
            this.sslStream.Close();
            this.tcpClient.GetStream().Close();
            this.tcpClient.Close();
        }



    }
}
