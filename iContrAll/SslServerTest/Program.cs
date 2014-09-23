using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace SslServerTest
{
    class Program
    {
        static X509Certificate serverCertificate = null;
        static void Main(string[] args)
        {
            serverCertificate = new X509Certificate2("server.p12", "allcontri");

            TcpListener listener = new TcpListener(IPAddress.Any, 1124);
            listener.Start();

            while (true)
            {
                Console.WriteLine("Waiting for a client to connect...");
                TcpClient client = listener.AcceptTcpClient();
                ProcessClient(client);
            }
        }

        private static void ProcessClient(TcpClient client)
        {
            SslStream sslStream = new SslStream(client.GetStream());

            try
            {
                sslStream.AuthenticateAsServer(serverCertificate, true, SslProtocols.Tls, true);
                //sslStream.ReadTimeout = 5000;
                //sslStream.WriteTimeout = 5000;

                Console.WriteLine("Waiting for client message...");

                string messageData = ReadMessage(sslStream);
                Console.WriteLine("Received: {0}", messageData);

                byte[] message = Encoding.UTF8.GetBytes("Hello from server.<EOF>");
                Console.WriteLine("Sending hello message");
                sslStream.Write(message);
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                sslStream.Close();
                client.Close();
                return;
            }
            finally
            {
                // The client stream will be closed with the sslStream 
                // because we specified this behavior when creating 
                // the sslStream.
                sslStream.Close();
                client.Close();
            }
        }

        private static string ReadMessage(SslStream sslStream)
        {
            // Read the  message sent by the client. 
            // The client signals the end of the message using the 
            // "<EOF>" marker.
            byte[] buffer = new byte[2048];
            StringBuilder messageData = new StringBuilder();
            int bytes = -1;
            do
            {
                // Read the client's test message.
                bytes = sslStream.Read(buffer, 0, buffer.Length);

                // Use Decoder class to convert from bytes to UTF8 
                // in case a character spans two buffers.
                Decoder decoder = Encoding.UTF8.GetDecoder();
                char[] chars = new char[decoder.GetCharCount(buffer, 0, bytes)];
                decoder.GetChars(buffer, 0, bytes, chars, 0);
                messageData.Append(chars);
                // Check for EOF or an empty message. 
                if (messageData.ToString().IndexOf("<EOF>") != -1)
                {
                    break;
                }
            } while (bytes != 0);

            return messageData.ToString();
        }
    }
}
