using System.Net;
using System.Net.Sockets;

namespace iContrAll.TcpServer
{
    /// <summary>
    /// Interface for transparent management of connected clients (from local network or remote connection)
    /// </summary>
    interface IConnectedDevice
    {
        EndPoint RemoteEndPoint { get; }
        bool CanRead { get; }
        int Read(byte[] buffer, int offset, int size);
        void Write(byte[] result);
        void SendAsync(SocketAsyncEventArgs asyncEventArgs);
        void Close();
    }
}
