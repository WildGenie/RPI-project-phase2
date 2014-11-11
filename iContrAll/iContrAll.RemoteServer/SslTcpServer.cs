using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;

namespace iContrAll.RemoteServer
{
    public sealed class SslTcpServer
    {
        private static TcpListener raspberryListener;
        private static TcpListener clientListener;
        private static Thread raspberryListenerThread;
        private static Thread clientListenerThread;
        
        private static List<RaspberryHandler> raspberryList = new List<RaspberryHandler>();
        private static List<ClientHandler> clientList = new List<ClientHandler>();

        static X509Certificate serverCertificate = null;
        // The certificate parameter specifies the name of the file  
        // containing the machine certificate. 
        public static void RunServer(string certificate, string password, int raspberryPort = 1123, int clientPort = 1124)
        {
            try
            {
        		serverCertificate = new X509Certificate2(certificate, password);
                
                // Create a TCP/IP (IPv4) socket and listen for incoming connections.
                raspberryListener = new TcpListener(IPAddress.Any, raspberryPort);
                clientListener = new TcpListener(IPAddress.Any, clientPort);

                raspberryListenerThread = new Thread(new ThreadStart(listenForRaspberries));
                clientListenerThread = new Thread(new ThreadStart(listenForClients));

                raspberryListenerThread.Start();
                clientListenerThread.Start();
            }
            catch(System.Security.Cryptography.CryptographicException ex)
            {
                Console.WriteLine("CryptographicException: {0}", ex.Message);
                if (ex.InnerException != null)
                { 
                    Console.WriteLine(ex.InnerException.Message); 
                }
            }
            catch(ArgumentOutOfRangeException ex)
            {
                Console.WriteLine("ArgumentOutOfRangeException: a port nem megfelelő tartományban mozog.");
                Console.WriteLine(ex.Message);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception: {0}", ex.Message);
                if (ex.InnerException != null)
                {
                    Console.WriteLine(ex.InnerException.Message);
                }
            }
        }

        private static object listSyncObject = new object();

        private static void listenForClients()
        {
            try
            {
                clientListener.Start();
                while (true)
                {
                    Console.WriteLine("Waiting for a client to connect...");
                    // Application blocks while waiting for an incoming connection. 
                    // Type CNTL-C to terminate the server.
                    TcpClient client = clientListener.AcceptTcpClient();

                    Console.WriteLine("Client connected");

                    var handleClientThread = new Thread(HandleClient);
                    handleClientThread.Start(client);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception in listenForClients: {0}", ex.Message);
                if (ex.InnerException!=null)
                {
                    Console.WriteLine("InnerException: {0}", ex.InnerException.Message);
                }
            }

        }

        private static void listenForRaspberries()
        {
            try
            {
                raspberryListener.Start();
                while (true)
                {
                    Console.WriteLine("Waiting for a raspberry to connect...");
                    // Application blocks while waiting for an incoming connection. 
                    // Type CNTL-C to terminate the server.
                    TcpClient raspberry = raspberryListener.AcceptTcpClient();

                    Console.WriteLine("Raspberry connected");

                    var handleRaspberryThread = new Thread(HandleRaspberry);
                    handleRaspberryThread.Start(raspberry);

                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception in listenForRaspberries: {0}", ex.Message);
                if (ex.InnerException!=null)
                {
                    Console.WriteLine("InnerException: {0}", ex.InnerException.Message);
                }
            }
        }

        static void HandleRaspberry(object clientParam)
        {
            TcpClient client = null;
            SslStream sslStream = null;
            try 
            {
                client = (TcpClient)clientParam;
                
                Console.WriteLine("HandleRaspberry {0}", client.Client.RemoteEndPoint.ToString());
                // A client has connected. Create the SslStream using the client's network stream.
                sslStream = new SslStream(client.GetStream(), false);
            
                Console.WriteLine("Authentication started");
                sslStream.AuthenticateAsServer(serverCertificate,
                    true, SslProtocols.Tls, true);
                Console.WriteLine("Raspberry Authentication COMPLETE {0} at {1}", client.Client.RemoteEndPoint.ToString(), DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                // Set timeouts for the read and write to 5 seconds.
                //sslStream.ReadTimeout = 5000;
                //sslStream.WriteTimeout = 5000;
                // Read a message from the client.   
                Console.WriteLine("Waiting for raspberry message...");

                byte[] buffer = new byte[32768];
                int numberOfBytesRead = -1;

                while (true)
                {
                    bool breakIt = false;
                    try
                    {
                        // Read the client's test message.
                        numberOfBytesRead = sslStream.Read(buffer, 0, buffer.Length);
                    }
                    catch (ArgumentOutOfRangeException aoe)
                    {
                        Console.WriteLine("The size of the message has exceeded the maximum size allowed.");
                        Console.WriteLine(aoe.ToString());
                        continue;
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Exception while reading from socket SslTcpServer.HandleRaspberry");// {0}");//, sslStream..Endpoint);
                        break;
                    }

                    if (numberOfBytesRead <= 0)
                    {
                        //Console.WriteLine("NumberOfBytesRead: {0} from {1}", numberOfBytesRead, tcpClient.RemoteEndPoint.ToString());
                        Console.WriteLine("NumberOfBytesRead: {0}", numberOfBytesRead);//, tcpClient.RemoteEndPoint.ToString());
                        break;
                    }

                    Console.WriteLine("Message (length={1}) received from: {0} at {2}", client.Client.RemoteEndPoint.ToString(), numberOfBytesRead, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

                    byte[] readBytes = buffer.Take(numberOfBytesRead).ToArray();
                    foreach (var message in ProcessBuffer(readBytes))
                    {
                        Console.WriteLine("Message: Type={0}: {1}", (MessageType)message.Type, message.Content);
                        if (message.Type == MessageType.IdentityMsg && message.Length == 10)
                        {
                            RaspberryHandler rh = new RaspberryHandler() { Id = message.Content, SslStream = sslStream, TcpChannel = client };
                            lock (listSyncObject)
                            {
                                List<RaspberryHandler> sameRaspberries = raspberryList.Where(r => r.Id == rh.Id).ToList();
                                //int rc = raspberryList.Count(r => r.Id == rh.Id);
                                int rc = sameRaspberries.Count;
                                if (rc <= 0)
                                {
                                    raspberryList.Add(rh);
                                }
                                else
                                {
                                    if (rc == 1)
                                    {
                                        RaspberryHandler rhOld = sameRaspberries.First();
                                        rhOld.Close();

                                        rhOld.Id = rh.Id;
                                        rhOld.SslStream = sslStream;
                                        rhOld.TcpChannel = client;
                                    }
                                }
                            }
                        }
                        else
                            if (message.Type == MessageType.CreateThreadFor)
                            {
                                Console.WriteLine("CreateTunnelFor request from {0}", client.Client.RemoteEndPoint.ToString());
                                lock (listSyncObject)
                                {
                                    List<ClientHandler> cHandlers = clientList.Where(ch => ch.Identifier == message.Content && ch.Connected).ToList();

                                    if (cHandlers.Count == 1)
                                    {
                                    
                                            Tunnel t = new Tunnel(sslStream, cHandlers.First());
                                            breakIt = true;
                                            break;
                                    }
                                    //foreach (var ch in chandlers)
                                    //{
                                    //    Tunnel t = new Tunnel(sslStream, ch);
                                    //}

                                }
                            }
                    }
                    if (breakIt) break;
                }

            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");
                if (sslStream!=null)
                    sslStream.Close();
                if (client != null)
                    client.Close();
                return;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception in HandleRaspberry init: invalid TcpClient or Stream is not exist");
                Console.WriteLine("Exception: {0}", ex.Message);
                if (ex.InnerException!=null)
                {
                    Console.WriteLine("InnerException: {0}", ex.InnerException.Message);
                }
            }
            finally
            {
                // The client stream will be closed with the sslStream 
                // because we specified this behavior when creating 
                // the sslStream.
                if (sslStream != null)
                    sslStream.Close();
                if (client != null)
                    client.Close();
            }
        }

        static void HandleClient(object clientParam)
        {
            TcpClient client = null;
            SslStream sslStream = null;
            try
            {
                client = (TcpClient)clientParam;
                Console.WriteLine("HandleClient {0} started at {1}", client.Client.RemoteEndPoint.ToString(), DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                // A client has connected. Create the  
                // SslStream using the client's network stream.
                sslStream = new SslStream(client.GetStream(), false);
                // Authenticate the server but don't require the client to authenticate. 

                Console.WriteLine("Authentication STARTED at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                sslStream.AuthenticateAsServer(serverCertificate, true, SslProtocols.Ssl3, true);

                // Set timeouts for the read and write to 5 seconds.
                //sslStream.ReadTimeout = 5000;
                //sslStream.WriteTimeout = 5000;
                // Read a message from the client.   
                Console.WriteLine("Authentication COMPLETED at {0}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                int numberOfBytesRead = -1;

                byte[] buffer = new byte[32768];

                while (true)
                {
                    numberOfBytesRead = sslStream.Read(buffer, 0, buffer.Length);
                    if (numberOfBytesRead <= 0)
                    {
                        Console.WriteLine("Client closed the socket {0}", client.Client.RemoteEndPoint);
                        break;
                    }
                    Console.WriteLine("ReadBuffer content: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead));

                    List<Message> messageBuffer = ProcessBuffer(buffer.Take(numberOfBytesRead).ToArray());
                    Console.WriteLine("MessageBuffer.Count = {0}", messageBuffer.Count);

                    bool breakIt = false;

                    for (int i = 0; i < messageBuffer.Count; i++)
                    {
                        var m = messageBuffer[i];
                        Console.WriteLine("MessageType: " + m.Type + " Content: " + m.Content);
                        if (m.Type == MessageType.IdentityMsg && m.Content.Length == 10)
                        {
                            lock (listSyncObject)
                            {
                                var rh = raspberryList.Where(r => r.Id == m.Content);
                                if (rh.Count() == 1)
                                {
                                    // Kérés a raspberry-hez, hogy indítson új csatornát amin aztán ezzel a klienssel fog kommunikálni.
                                    rh.First().SendCreateTunnelFor(client.Client.RemoteEndPoint.ToString());

                                    ClientHandler ch = new ClientHandler(client, sslStream);

                                    List<Message> tempMsgs = new List<Message>();
                                    for (int j = i + 1; j < messageBuffer.Count; j++)
                                    {
                                        tempMsgs.Add(messageBuffer[j]);
                                    }

                                    ch.MessageBuffer = tempMsgs;

                                    clientList.Add(ch);

                                    breakIt = true;
                                    break;
                                }
                            }

                            // TODO: ask for creating thread, save this client
                            // TODO: vigyük a maradék üzeneteket át, kell a bejelentkezéshez!
                            // ClientHandler ch = new ClientHandler() { TcpChannel = client, SslStream = sslStream };
                        }
                    }

                    if (breakIt)
                        break;
                }
            }
            catch (AuthenticationException e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Authentication failed - closing the connection.");

            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                }
                Console.WriteLine("Exception while trying to make some effort to get the raspberry {0}", client.Client.RemoteEndPoint.ToString());
            }
            finally
            {
                //// The client stream will be closed with the sslStream 
                //// because we specified this behavior when creating 
                //// the sslStream.
                if (sslStream != null)
                    sslStream.Close();
                if (client != null)
                    client.Close();
            }
        }

        private static List<Message> ProcessBuffer(byte[] readBuffer)
        {
            var returnList = new List<Message>();

            // felfűzzük az elejére a maradékot
            byte[] completeBuffer;
            //if (trailingBuffer.Length > 0)
            //{
            //    completeBuffer = new byte[trailingBuffer.Length + readBuffer.Length];
            //    Array.Copy(trailingBuffer, completeBuffer, trailingBuffer.Length);
            //    Array.Copy(readBuffer, 0, completeBuffer, trailingBuffer.Length, readBuffer.Length);
            //}
            //else 
            completeBuffer = readBuffer;

            while (completeBuffer.Length > 0)
            {
                if (completeBuffer.Length < 4) break;

                byte[] messageTypeArray = new byte[4];
                Array.Copy(completeBuffer, messageTypeArray, 4);

                int messageType = BitConverter.ToInt32(messageTypeArray, 0);

                // Console.WriteLine(clientState.ToString());
                bool exists = false;

                // TODO: extract, do it only once!!
                foreach (var e in Enum.GetValues(typeof(MessageType)))
                {
                    if ((int)e == messageType)
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                {

                    Console.WriteLine("Gyanus! MessageType: {0}", messageType);
                    break;

                    //if (completeBuffer.Length > 4)
                    //{
                    //    var temp = completeBuffer;
                    //    temp.CopyTo(completeBuffer, 4);
                    //    completeBuffer = temp;
                    //}
                }

                if (completeBuffer.Length < 8) break;

                byte[] messageLengthArray = new byte[4];
                Array.Copy(completeBuffer, 4, messageLengthArray, 0, 4);

                int messageLength = BitConverter.ToInt32(messageLengthArray, 0);

                // TODO: az összes ilyen esetkor (pl. kétszer feljebb) el kell tárolni a trailMessage-ben!
                if (completeBuffer.Length < 8 + messageLength) break;

                byte[] messageArray = new byte[messageLength];
                Array.Copy(completeBuffer, 8, messageArray, 0, messageLength);

                string message = Encoding.UTF8.GetString(messageArray);

                returnList.Add(new Message(messageType, messageLength, message));

                //Console.WriteLine(completeBuffer.Length + " - " + (8 + messageArray.Length));
                if (completeBuffer.Length >= 8 + messageArray.Length) // CONTINUE,
                {
                    byte[] tempBuf = new byte[completeBuffer.Length - (8 + messageArray.Length)];
                    // Console.WriteLine(completeBuffer.Length + " - " + (8 + messageArray.Length) + " = tempBuf.Length: " + tempBuf.Length);
                    Array.Copy(completeBuffer, 8 + messageArray.Length, tempBuf, 0, completeBuffer.Length - (8 + messageArray.Length));
                    completeBuffer = tempBuf;
                }
            }

            return returnList;
        }

        
        #region SslDisplayDetails
        static void DisplaySecurityLevel(SslStream stream)
        {
            Console.WriteLine("Cipher: {0} strength {1}", stream.CipherAlgorithm, stream.CipherStrength);
            Console.WriteLine("Hash: {0} strength {1}", stream.HashAlgorithm, stream.HashStrength);
            Console.WriteLine("Key exchange: {0} strength {1}", stream.KeyExchangeAlgorithm, stream.KeyExchangeStrength);
            Console.WriteLine("Protocol: {0}", stream.SslProtocol);
        }
        static void DisplaySecurityServices(SslStream stream)
        {
            Console.WriteLine("Is authenticated: {0} as server? {1}", stream.IsAuthenticated, stream.IsServer);
            Console.WriteLine("IsSigned: {0}", stream.IsSigned);
            Console.WriteLine("Is Encrypted: {0}", stream.IsEncrypted);
        }
        static void DisplayStreamProperties(SslStream stream)
        {
            Console.WriteLine("Can read: {0}, write {1}", stream.CanRead, stream.CanWrite);
            Console.WriteLine("Can timeout: {0}", stream.CanTimeout);
        }
        static void DisplayCertificateInformation(SslStream stream)
        {
            Console.WriteLine("Certificate revocation list checked: {0}", stream.CheckCertRevocationStatus);

            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                Console.WriteLine("Local cert was issued to {0} and is valid from {1} until {2}.",
                    localCertificate.Subject,
                    localCertificate.GetEffectiveDateString(),
                    localCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            if (stream.RemoteCertificate != null)
            {
                Console.WriteLine("Remote cert was issued to {0} and is valid from {1} until {2}.",
                    remoteCertificate.Subject,
                    remoteCertificate.GetEffectiveDateString(),
                    remoteCertificate.GetExpirationDateString());
            }
            else
            {
                Console.WriteLine("Remote certificate is null.");
            }
        }
        private static void DisplayUsage()
        {
            Console.WriteLine("To start the server specify:");
            Console.WriteLine("iContrAll.RemoteServer certificateFile.pfx certPassphrase");
            Environment.Exit(1);
        }

        #endregion

        public static int Main(string[] args)
        {
            string certificate = null;
            string password = null;
            if (args == null || args.Length < 2)
            {
                DisplayUsage();
            }
            certificate = args[0];
            password = args[1];
            SslTcpServer.RunServer(certificate, password);
            return 0;
        }

        private static X509Certificate getServerCert(string certificateName) // certificateName = "alpha.icontrall.hu"
        {
            X509Store store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
            store.Open(OpenFlags.ReadOnly);
            X509CertificateCollection cert = store.Certificates.Find(X509FindType.FindBySubjectName, certificateName, false);
            return cert[0];
        }
    }
}