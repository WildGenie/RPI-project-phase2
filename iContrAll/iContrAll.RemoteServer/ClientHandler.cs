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
        public TcpClient TcpChannel { get; set; }
        public SslStream SslStream { get; set; }

        // IP + port  vagy ami az üzenetben volt
        public string Identifier { get; set; }

        public List<Message> MessageBuffer { get; set; }

        public void Write(byte[] message, int numberOfBytesRead)
        {
            SslStream.Write(message, 0, numberOfBytesRead);
            Console.WriteLine("SentToClient {0}", Encoding.UTF8.GetString(message, 0, numberOfBytesRead));
        }
        
        public byte[] Read()
        {
            byte[] buffer = new byte[32768];
            int numberOfBytesRead = SslStream.Read(buffer,0, buffer.Length);
            return buffer;
        }
    }
}
