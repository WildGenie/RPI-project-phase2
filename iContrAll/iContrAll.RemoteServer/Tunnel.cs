using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;

namespace iContrAll.RemoteServer
{
    class Tunnel
    {
        public SslStream Rasberry { get; set; }
        public ClientHandler Client { get; set; }

        Thread raspberryThread;
        Thread clientThread;

        public Tunnel(SslStream rh, ClientHandler ch)
        {
            this.Rasberry = rh;
            this.Client = ch;

            foreach (var m in ch.MessageBuffer)
            {
                byte[] buffer = BuildMessage((byte)m.Type, Encoding.UTF8.GetBytes(m.Content));
                if (rh.CanWrite)
                    rh.Write(buffer);
            }

            raspberryThread = new Thread(ListenForRasberryMessages);
            clientThread = new Thread(ListenForClientMessages);

            clientThread.Start();
            raspberryThread.Start();
        }

        private byte[] BuildMessage(int msgNumber, byte[] message)
        {
            byte[] msgNbrArray = new byte[4];
            Array.Copy(BitConverter.GetBytes(msgNumber), msgNbrArray, msgNbrArray.Length);

            byte[] lengthArray = new byte[4];
            Array.Copy(BitConverter.GetBytes(message.Length), lengthArray, lengthArray.Length);

            byte[] answer = new byte[4 + 4 + message.Length];

            System.Buffer.BlockCopy(msgNbrArray, 0, answer, 0, msgNbrArray.Length);
            System.Buffer.BlockCopy(lengthArray, 0, answer, msgNbrArray.Length, lengthArray.Length);
            System.Buffer.BlockCopy(message, 0, answer, msgNbrArray.Length + lengthArray.Length, message.Length);

            return answer;
        }

        private void ListenForClientMessages(object obj)
        {
            byte[] buffer = new byte[32768];
            int numberOfBytesRead = -1;

            while(true)
            {
                try
                {
                    // Read the client's test message.
                    numberOfBytesRead = Client.Read(buffer);
                    if (numberOfBytesRead >= 0)
                        Console.WriteLine("MessageFromClient: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead));
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception while reading from socket Tunnel.ListenForClientMessages");// {0}");//, sslStream..Endpoint);
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    break;
                }

                if (numberOfBytesRead <= 0)
                {
                    //Console.WriteLine("NumberOfBytesRead: {0} from {1}", numberOfBytesRead, tcpClient.RemoteEndPoint.ToString());
                    Console.WriteLine("NumberOfBytesRead: {0}", numberOfBytesRead);//, tcpClient.RemoteEndPoint.ToString());
                    break;
                }

                try
                {
                    Rasberry.Write(buffer, 0, numberOfBytesRead);
                    Console.WriteLine("SentToRaspberry: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception at Rasberry.Write() in Tunnel.ListenForClientMessages(): {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    break;
                }
            }


            
        }

        private void ListenForRasberryMessages(object obj)
        {
            byte[] buffer = new byte[32768];
            int numberOfBytesRead = -1;

            while (true)
            {
                try
                {
                    numberOfBytesRead = Rasberry.Read(buffer, 0, buffer.Length);
                    Console.WriteLine("MessageFromRaspberry: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead));
                }
                catch (ArgumentOutOfRangeException)
                {
                    Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                    continue;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception while reading from socket in Tunnel.ListenForRasberryMessages");
                    // Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                    Console.WriteLine(ex.Message);
                    if (ex.InnerException != null)
                        Console.WriteLine(ex.InnerException.Message);

                    break;
                    
                }

                if (numberOfBytesRead <= 0)
                {
                    Console.WriteLine("NumberOfBytesRead: {0}", numberOfBytesRead);//, tcpClient.RemoteEndPoint.ToString());
                    break;
                }

                try
                {
                    bool successWrite = Client.Write(buffer, numberOfBytesRead);
                    
                    if (!successWrite)
                    {
                        Console.WriteLine("Client {0} is not connected.", Client.Identifier);
                        Console.WriteLine("Closing connection");
                        // Close tunnel, close connection to raspberry.
                        Client.Close();
                        Rasberry.Close();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Exception in tunnel between Client: {0} and Rasbperry: {1}", Client.Identifier, "Raspberry shálálálá");
                    Console.WriteLine(ex.Message);
                    if (ex.InnerException!=null)
                        Console.WriteLine(ex.InnerException.Message);
                    break;
                }
            }
        }
    }
}
