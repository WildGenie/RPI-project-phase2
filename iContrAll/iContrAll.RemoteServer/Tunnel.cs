using LogHelper;
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
        public RaspberryHandler Raspberry { get; set; }
        public ClientHandler Client { get; set; }

        Thread raspberryThread;
        Thread clientThread;

        public Tunnel(RaspberryHandler rh, ClientHandler ch)
        {
            this.Raspberry = rh;
            this.Client = ch;

            Log.WriteLine("Tunnel CREATED: Raspberry {0} - Client {1}", rh.Id, ch.Id);

            foreach (var m in ch.MessageBuffer)
            {
                byte[] buffer = BuildMessage((byte)m.Type, Encoding.UTF8.GetBytes(m.Content));
                rh.Write(buffer, buffer.Length);
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
                        Log.WriteLine("MessageFromClient {1}: {0} in {2}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead), Client.Id, "Tunnel.ListenForClientMessages()");
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Log.WriteLine("The size of the message has exceeded the maximum size allowed in {0}", "Tunnel.ListenForClientMessages()");
                    Log.WriteLine(ex.ToString());
                    continue;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception while reading from socket in Tunnel.ListenForClientMessages()");// {0}");//, sslStream..Endpoint);
                    Log.WriteLine(ex.ToString());
                    break;
                }

                if (numberOfBytesRead <= 0)
                {
                    //Log.WriteLine("NumberOfBytesRead: {0} from {1}", numberOfBytesRead, tcpClient.RemoteEndPoint.ToString());
                    Log.WriteLine("NumberOfBytesRead from {2}: {0} in {1}", numberOfBytesRead, "Tunnel.ListenForClientMessages()", Client.Id);
                    Log.WriteLine("\t-> Closing connection between {0} and {1}", Client.Id, Raspberry.Id);
                    // kliens megszüntette a kapcsolatot, számoljuk fel ezt a tunnelt
                    // értesíteni kell a Raspberry-t és a klienst is eltávolítjuk a szerver nyilvántartásából
                    Raspberry.Close();
                    Client.Close();
                    Log.WriteLine("Tunnel CLOSED: Raspberry {0} - Client {1}", Raspberry.Id, Client.Id);
                    break;
                }

                try
                {
                    bool successWrite = Raspberry.Write(buffer, numberOfBytesRead);
                    if (!successWrite)
                    {
                        Log.WriteLine("Raspberry {0} is not connected in {1}.", Raspberry.Id, "Tunnel.ListenForClientMessages()");
                        Log.WriteLine("\t-> Closing connection between {0} and {1}", Client.Id, Raspberry.Id);
                        // Close tunnel, close connection to raspberry.
                        Client.Close();
                        Raspberry.Close();
                        Log.WriteLine("Tunnel CLOSED: Raspberry {0} - Client {1}", Raspberry.Id, Client.Id);
                        break;
                    }
                    Log.WriteLine("SentToRaspberry {1} {0} in {2}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead), Raspberry.Id, "Tunnel.ListenForClientMessages()");
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception in tunnel between\n\tClient: {0} and Rasbperry: {1}\n\tin {2}", Client.Id, Raspberry.Id, "Tunnel.ListenForClientMessages()");
                    Log.WriteLine(ex.ToString());
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
                    numberOfBytesRead = Raspberry.Read(buffer);
                    if (numberOfBytesRead >= 0)
                        Log.WriteLine("MessageFromRaspberry {1}: {0} in {2}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead), Raspberry.Id, "Tunnel.ListenForRaspberryMessages()");
                        //Log.WriteLine("MessageFromRaspberry: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead));
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    Log.WriteLine("The size of the message has exceeded the maximum size allowed in {0}", "Tunnel.ListenForRaspberryMessages()");
                    Log.WriteLine(ex.ToString());
                    continue;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception while reading from socket in Tunnel.ListenForRasberryMessages()");
                    Log.WriteLine(ex.ToString());
                    break;
                }

                if (numberOfBytesRead <= 0)
                {
                    Log.WriteLine("NumberOfBytesRead from {2}: {0} in {1}", numberOfBytesRead, "Tunnel.ListenForRaspberryMessages()", Raspberry.Id);
                    Log.WriteLine("\t-> Closing connection between {0} and {1}", Raspberry.Id, Client.Id);
                    Raspberry.Close();
                    Client.Close();
                    Log.WriteLine("Tunnel CLOSED: Raspberry {0} - Client {1}", Raspberry.Id, Client.Id);
                    break;
                }

                try
                {
                    bool successWrite = Client.Write(buffer, numberOfBytesRead);
                    
                    if (!successWrite)
                    {
                        Log.WriteLine("Client {0} is not connected in {1}.", Client.Id, "Tunnel.ListenForRasberryMessages()");
                        Log.WriteLine("\t-> Closing connection between {0} and {1}", Raspberry.Id, Client.Id);
                        // Close tunnel, close connection to raspberry.
                        Client.Close();
                        Raspberry.Close();
                        Log.WriteLine("Tunnel CLOSED: Raspberry {0} - Client {1}", Raspberry.Id, Client.Id);
                        break;
                    }
                    Log.WriteLine("SentToClient {1} {0} in {2}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead), Raspberry.Id, "Tunnel.ListenForRaspberryMessages()");
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception in tunnel between\n\tClient: {0} and Rasbperry: {1}\n\tin {2}", Client.Id, Raspberry.Id, "Tunnel.ListenForRaspberryMessages()");
                    Log.WriteLine(ex.ToString());
                    break;
                }
            }
        }
    }
}
