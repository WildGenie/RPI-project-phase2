using LogHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
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
        public string Id { get; private set; }

        public EndPoint EndPoint { get { return tcpChannel.Client.RemoteEndPoint; } }

        public string DemandedRaspberryId { get; set; }
        
        public List<Message> MessageBuffer { get; set; }

        public bool Connected { get { return tcpChannel.Connected; } }

        public delegate void RemoveClientEH(ClientHandler ch);
        public event RemoveClientEH RemoveClient;

        public ClientHandler(TcpClient tcpChannel, SslStream sslStream, RemoveClientEH removeClient)
        {
            this.tcpChannel = tcpChannel;
            this.sslStream = sslStream;
            this.Id = this.tcpChannel.Client.RemoteEndPoint.ToString();
            this.RemoveClient += removeClient;
        }

        public ClientHandler()
        {
            // TODO: ebből még baj lesz, miért nem oldom meg normálisan
        }

        public bool Write(byte[] message, int numberOfBytesRead)
        {
            try
            {
                if (!tcpChannel.Connected)
                {
                    Log.WriteLine("Client is not connected {0} in {1}", Id, "ClientHandler.Write()");
                    return false;
                }
                if (sslStream.CanWrite)
                {
                    sslStream.Write(message, 0, numberOfBytesRead);
                    sslStream.Flush();
                    Log.WriteLine("SentToClient {1} {0} in {2}", Encoding.UTF8.GetString(message, 0, numberOfBytesRead), Id, "ClientHandler.Write()");

                    return true;
                }
                else
                {
                    Log.WriteLine("Cannot write to sslStream at {0} in {1}", Id, "ClientHandler.Write()");
                    return false;
                }
                
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception while trying to write to Client {0} in {1}", Id, "ClientHandler.Write()");
                Log.WriteLine(ex.ToString());
            }
            return false;
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
                Log.WriteLine("Exception while reading from Client {0} in {1}", Id, "ClientHandler.Read()");
                Log.WriteLine(ex.ToString());
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
            catch(Exception ex)
            {
                Log.WriteLine("Exception while closing Client connection {0} in {1}", Id, "ClientHandler.Close()");
                Log.WriteLine(ex.ToString());
            }
        }
    }
}
