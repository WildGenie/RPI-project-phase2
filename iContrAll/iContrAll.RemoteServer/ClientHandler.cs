using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace iContrAll.RemoteServer
{
    public class ClientHandler
    {
        private TcpClient tcpChannel;
        private SslStream sslStream;

        // IP + port  vagy ami az üzenetben volt
        public string Identifier { get; private set; }

        public string DemandedRaspberryId { get; set; }
        
        public List<Message> MessageBuffer { get; set; }

        public bool Connected { get { return tcpChannel.Connected; } }

        public delegate void RemoveClientEH(ClientHandler ch);
        public event RemoveClientEH RemoveClient;

        public ClientHandler(TcpClient tcpChannel, SslStream sslStream)
        {
            this.tcpChannel = tcpChannel;
            this.sslStream = sslStream;
            this.Identifier = this.tcpChannel.Client.RemoteEndPoint.ToString();
        }

        public bool Write(byte[] message, int numberOfBytesRead)
        {
            try
            {
                if (!tcpChannel.Connected)
                {
                    Console.WriteLine("Client is not connected {0}", Identifier);
                    return false;
                }
                if (sslStream.CanWrite)
                {
                    sslStream.Write(message, 0, numberOfBytesRead);
                    Console.WriteLine("SentToClient {0}", Encoding.UTF8.GetString(message, 0, numberOfBytesRead));

                    return true;
                }
                else
                {
                    Console.WriteLine("Cannot write to sslStream at {0}", Identifier);
                    return false;
                }
                
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception while trying to write to Client {0}", Identifier);
                Console.WriteLine(ex.Message);
                if (ex.InnerException!=null)
                    Console.WriteLine(ex.InnerException);
                return false;
            }
        }
        
        public int Read(byte[] buffer)
        {
            int numberOfBytesRead = -1;
            try
            {
                if (sslStream.CanRead)
                    numberOfBytesRead = sslStream.Read(buffer, 0, buffer.Length);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception while reading from Client {0}", Identifier);
                Console.WriteLine(ex.Message);
                if (ex.InnerException!=null)
                { Console.WriteLine(ex.InnerException.Message); }
            }
            return numberOfBytesRead;
        }

        public void Close()
        {
            try
            {
                if (this.RemoveClient!=null)
                {
                    this.RemoveClient(this);
                }
                sslStream.Close();
                tcpChannel.Close();
            }
            catch(Exception)
            {
                Console.WriteLine("Exception while closing Client connection {0}", Identifier);
            }
        }
    }
}
