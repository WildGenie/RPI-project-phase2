using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    class LocalConnectedDevice:IConnectedDevice
    {
        public EndPoint RemoteEndPoint { get; private set; }

        public bool CanRead
        {
            get { return this.tcpClient.GetStream().CanRead; }
        }

        private TcpClient tcpClient;

        public LocalConnectedDevice(TcpClient client)
        {
            this.tcpClient = client;
            this.RemoteEndPoint = client.Client.RemoteEndPoint;
        }

        public int Read(byte[] buffer, int offset, int size)
        {
            return this.tcpClient.GetStream().Read(buffer, offset, size);
        }

        public void Write(byte[] result)
        {
            this.tcpClient.GetStream().Write(result, 0, result.Length);
        }
        public void SendAsync(SocketAsyncEventArgs asyncEventArgs)
        {
            tcpClient.Client.SendAsync(asyncEventArgs);
        }

        public void Close()
        {
            this.tcpClient.GetStream().Close();
            this.tcpClient.Close();
        }


        
    }
}
