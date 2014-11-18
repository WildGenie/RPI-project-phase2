using LogHelper;
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
        public static void RunServer(string certificate, string password, int raspberryPort = 1125, int clientPort = 1124)
        {
            try
            {
        		serverCertificate = new X509Certificate2(certificate, password);
                
                // Create a TCP/IP (IPv4) socket and listen for incoming connections.
                raspberryListener = new TcpListener(IPAddress.Any, raspberryPort);
                clientListener = new TcpListener(IPAddress.Any, clientPort);

                raspberryListenerThread = new Thread(new ThreadStart(listenForRaspberries));
                clientListenerThread = new Thread(new ThreadStart(listenForClients));

                Log.WriteLine("Server initialized...");

                raspberryListenerThread.Start();
                clientListenerThread.Start();
            }
            catch(System.Security.Cryptography.CryptographicException ex)
            {
                Log.WriteLine("CryptographicException {0}", ex.ToString());
            }
            catch(ArgumentOutOfRangeException ex)
            {
                Log.WriteLine("ArgumentOutOfRangeException: a port nem megfelelő tartományban mozog: {0}", ex.ToString());
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception: {0}", ex.ToString());
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
                    Log.WriteLine("Waiting for a client to connect...");
                    // Application blocks while waiting for an incoming connection. 
                    // Type CNTL-C to terminate the server.
                    TcpClient client = clientListener.AcceptTcpClient();

                    Log.WriteLine("Client connected {0}", client.Client.RemoteEndPoint.ToString());

                    var handleClientThread = new Thread(HandleClient);
                    handleClientThread.Start(client);
                }
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception in SslTcpServer.listenForClients: {0}", ex.ToString());
            }
        }

        private static void listenForRaspberries()
        {
            try
            {
                raspberryListener.Start();
                while (true)
                {
                    Log.WriteLine("Waiting for a raspberry to connect...");
                    // Application blocks while waiting for an incoming connection. 
                    // Type CNTL-C to terminate the server.
                    TcpClient raspberry = raspberryListener.AcceptTcpClient();

                    Log.WriteLine("Raspberry? connected {0}", raspberry.Client.RemoteEndPoint.ToString());

                    var handleRaspberryThread = new Thread(HandleRaspberry);
                    handleRaspberryThread.Start(raspberry);
                }
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception in listenForRaspberries: {0}", ex.ToString());
            }
        }

        static void HandleRaspberry(object raspberryParam)
        {
            TcpClient raspberryClient = null;
            SslStream sslStream = null;
            RaspberryHandler raspberry = null;
            bool closeSslStream = true;
            try 
            {
                raspberryClient = (TcpClient)raspberryParam;
                
                Log.WriteLine("HandleRaspberry {0}", raspberryClient.Client.RemoteEndPoint.ToString());
                
                // A client has connected. Create the SslStream using the client's network stream.
                sslStream = new SslStream(raspberryClient.GetStream(), false);

                Log.WriteLine("STARTED Raspberry Authentication {0}", raspberryClient.Client.RemoteEndPoint.ToString());
                
                sslStream.AuthenticateAsServer(serverCertificate, true, SslProtocols.Tls, true);
                Log.WriteLine("COMPLETE Raspberry Authentication {0}", raspberryClient.Client.RemoteEndPoint.ToString()); //, DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));
                
                

                byte[] buffer = new byte[32768];
                int numberOfBytesRead = -1;

                raspberry = new RaspberryHandler(raspberryClient, sslStream, rh_RemoveRaspberry);
                
                while (true)
                {
                    bool breakIt = false;
                    try
                    {
                        Log.WriteLine("Waiting for raspberry message...{0}", raspberry.EndPoint);
                        // Read the raspberry's message.
                        if ((numberOfBytesRead = raspberry.Read(buffer)) <= 0)
                        {
                            Log.WriteLine("{1}: Zerobyte read: {0}", numberOfBytesRead, raspberry.EndPoint);
                            raspberry.Close();
                            raspberry = null;
                            break;
                        }
                    }
                    catch (ArgumentOutOfRangeException aoe)
                    {
                        Log.WriteLine("{0}: The size of the message has exceeded the maximum size allowed.", raspberry.EndPoint);
                        Log.WriteLine(aoe.ToString());
                        continue;
                    }
                    catch (Exception)
                    {
                        Log.WriteLine("{0} Exception while reading from socket SslTcpServer.HandleRaspberry", raspberry.EndPoint);
                        break;
                    }

                    // Ha idáig eljutunk, tényleg jött valami üzenet.
                    Log.WriteLine("Message (length={1}) received from: {0} at {2}", raspberry.EndPoint, numberOfBytesRead, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture));

                    byte[] readBytes = buffer.Take(numberOfBytesRead).ToArray();
                    
                    foreach (var message in ProcessBuffer(readBytes))
                    {
                        Log.WriteLine("{2} Message: Type={0} Content={1}", (MessageType)message.Type, message.Content, raspberry.EndPoint.ToString());
                        if (message.Type == MessageType.IdentityMsg && message.Length == 10)
                        {
                            raspberry.ResetTimeOutTimer();
                            // eddig protokollnak megfelelő
                            lock (listSyncObject)
                            {
                                // Ha azonos Id-val már létezett, akkor nem adjuk hozzá az újat.
                                // TODO: átgondolni ezt biztonsági szempontból.
                                // Nem vicces.
                                int alreadyConnected = raspberryList.Count(r => r.Id == message.Content);

                                if (alreadyConnected == 0)
                                {
                                    raspberry.Id = message.Content;
                                    raspberryList.Add(raspberry);
                                    Log.WriteLine("Raspberry {0} added to list", raspberry.Id);
                                }
                                else
                                {
                                    Log.WriteLine("!!Raspberry with Id={0} already connected!! // {1} times", message.Content, alreadyConnected);
                                    raspberry.Close();
                                    raspberry = null;
                                }

                                Log.WriteLine("Connected Raspberries: ");
                                StringBuilder sb = new StringBuilder();
                                foreach (var r in raspberryList)
                                {
                                    sb.AppendFormat("\tId: {0} -> EndPoint: {1};\n", r.Id, r.EndPoint.ToString());
                                }
                                Log.WriteLine("\t" + sb.ToString());
                                sb.Clear();
                            }
                        }
                        else
                        // válasz érkezett a CreateThreadFor Client-re, egy új socket
                        if (message.Type == MessageType.CreateThreadFor)
                        {
                            raspberry.ResetTimeOutTimer();
                            Log.WriteLine("CreateTunnelFor request from {0}", raspberry.EndPoint.ToString());
                            lock (listSyncObject)
                            {
                                List<ClientHandler> cHandlers = clientList.Where(ch => ch.Id == message.Content && ch.Connected).ToList();

                                if (cHandlers.Count == 1)
                                {
                                    raspberry.Id = cHandlers.First().DemandedRaspberryId;
                                    // itt párosítjuk a jelenlegi raspberry csatlakozás sslStream-jét az erre a válaszra váró félretett klienssel.
                                    Tunnel t = new Tunnel(raspberry, cHandlers.First());
                                    // az egész while ciklust megszakítjuk, a csatorna kommunikáció maradt csak, nem itt kell lesni a raspberry üzeneteit
                                    breakIt = true;
                                    closeSslStream = false;
                                    // a maradék üzenetet eldobjuk, épp eleget tudunk
                                    break;
                                }
                            }
                        }
                        else
                        if (message.Type == MessageType.Ping)
                        {
                            Log.WriteLine("PingMessage from {0}", raspberry.Id);
                            raspberry.ResetTimeOutTimer();
                        }
                    }
                    if (breakIt) break;
                }
            }
            catch (AuthenticationException e)
            {
                Log.WriteLine("Authentication failed - closing connection.");
                Log.WriteLine("Exception: {0}", e.ToString());
                
                if (sslStream!=null)
                    sslStream.Close();
                if (raspberryClient != null)
                    raspberryClient.Close();
                return;
            }
            catch(Exception ex)
            {
                Log.WriteLine("Exception in HandleRaspberry init: invalid TcpClient or Stream is not exist");
                Log.WriteLine("Exception: {0}", ex.ToString());
            }
            finally
            {
                // akkor zárjuk, ha a fő kommunikációs socket szűnik meg. 
                // A Tunnel socket esetében nem zárunk finally-ben.
                if (closeSslStream)
                {
                    if (raspberry != null)
                    {
                        raspberry.Close();
                        raspberry = null;
                    }
                    // The client stream will be closed with the sslStream 
                    // because we specified this behavior when creating 
                    // the sslStream.
                    if (sslStream != null)
                        sslStream.Close();
                    if (raspberryClient != null)
                        raspberryClient.Close();
                }
            }
        }

        

        static void HandleClient(object clientParam)
        {
            bool closeSslStream = true;
            TcpClient tcpClient = null;
            SslStream sslStream = null;
            ClientHandler client = null;

            try
            {
                tcpClient = (TcpClient)clientParam;
                Log.WriteLine("HandleClient started {0}", tcpClient.Client.RemoteEndPoint.ToString());
                // A client has connected. Create the  
                // SslStream using the client's network stream.
                sslStream = new SslStream(tcpClient.GetStream(), false);

                Log.WriteLine("STARTED Client Authentication {0}", tcpClient.Client.RemoteEndPoint.ToString());
                sslStream.AuthenticateAsServer(serverCertificate, true, SslProtocols.Ssl3, true);

                // Read a message from the client.   
                Log.WriteLine("COMPLETED Client Authentication {0}", tcpClient.Client.RemoteEndPoint.ToString());
                int numberOfBytesRead = -1;

                byte[] buffer = new byte[32768];

                client = new ClientHandler(tcpClient, sslStream, ch_RemoveClient);

                while (true)
                {
                    Log.WriteLine("Waiting for client message...{0}", client.EndPoint);
                    if ((numberOfBytesRead = client.Read(buffer)) <= 0)
                    {
                        Log.WriteLine("Client {1}: Zerobyte read: {0}", numberOfBytesRead, client.EndPoint);
                        client.Close();
                        client = null;
                        break;
                    }

                    Log.WriteLine("{1} ReadBuffer content: {0}", Encoding.UTF8.GetString(buffer, 0, numberOfBytesRead), client.EndPoint.ToString());

                    List<Message> messageBuffer = ProcessBuffer(buffer.Take(numberOfBytesRead).ToArray());
                    Log.WriteLine("{1} MessageBuffer.Count = {0}", messageBuffer.Count, client.EndPoint.ToString());

                    bool breakIt = false;

                    for (int i = 0; i < messageBuffer.Count; i++)
                    {
                        var m = messageBuffer[i];
                        Log.WriteLine("Message from {0}: Type: " + m.Type + " Content: " + m.Content, client.EndPoint.ToString());
                        if (m.Type == MessageType.IdentityMsg && m.Content.Length == 10)
                        {
                            client.DemandedRaspberryId = m.Content;
                            // helyes a kérés
                            lock (listSyncObject)
                            {
                                var rh = raspberryList.Where(r => r.Id == m.Content);
                                if (rh.Count() == 1)
                                {
                                    // Kérés a raspberry-hez, hogy indítson új csatornát amin aztán ezzel a klienssel fog kommunikálni.
                                    rh.First().SendCreateTunnelFor(client.Id.ToString());

                                    List<Message> tempMsgs = new List<Message>();
                                    for (int j = i + 1; j < messageBuffer.Count; j++)
                                    {
                                        tempMsgs.Add(messageBuffer[j]);
                                    }

                                    client.MessageBuffer = tempMsgs;

                                    clientList.Add(client);

                                    Log.WriteLine("Client {0} added to list", client.Id);
                                    
                                    StringBuilder sb = new StringBuilder();
                                    Log.WriteLine("Connected Clients: ");
                                    foreach (var c in clientList)
                                    {
                                        sb.AppendFormat("\t-> EndPoint: {0} -> DemandedRaspberry: {1};\n", c.Id, c.DemandedRaspberryId);
                                    }
                                    Log.WriteLine(sb.ToString());
                                    sb.Clear();

                                    closeSslStream = false;
                                    breakIt = true;
                                    break;
                                }
                                else
                                {
                                    // Itt gáz van
                                    breakIt = true;
                                    break;
                                }
                            }
                        }
                    }

                    if (breakIt)
                        break;
                }
            }
            catch (AuthenticationException e)
            {
                Log.WriteLine("Authentication failed - closing the connection.");
                Log.WriteLine("Exception: {0}", e.ToString());
            }
            catch (Exception e)
            {
                Log.WriteLine("Exception while trying to make some effort to get the raspberry {0}", tcpClient.Client.RemoteEndPoint.ToString());
                Log.WriteLine("Exception: {0}", e.ToString());
            }
            finally
            {
                //// The client stream will be closed with the sslStream 
                //// because we specified this behavior when creating 
                //// the sslStream.
                if (closeSslStream)
                {
                    if (client!=null) client.Close();
                    if (sslStream != null)
                        sslStream.Close();
                    if (tcpClient != null)
                        tcpClient.Close();
                }
            }
        }

        static void ch_RemoveClient(ClientHandler ch)
        {
            if (ch == null) return;
            lock(listSyncObject)
            {
                string id = ch.Id;
                bool result = clientList.Remove(ch);
                if (result) Log.WriteLine("Client" + id + "succesfully removed");
                else Log.WriteLine("Client" + id + "was not in the list, or cannot be removed");

                StringBuilder sb = new StringBuilder();
                Log.WriteLine("Connected clients: ");
                foreach (var c in clientList)
                {
                    sb.AppendFormat("\tId: {0} -> DemandedRaspberryId: {1};\n", c.Id, c.DemandedRaspberryId);
                }
                Log.WriteLine(sb.ToString());
                sb.Clear();

                ch = null;
            }
        }

        static void rh_RemoveRaspberry(RaspberryHandler rh)
        {
            if (rh == null) return;
            lock (listSyncObject)
            {
                string id = rh.Id != null ? rh.Id : rh.EndPoint.ToString();
                bool result = raspberryList.Remove(rh);
                Log.WriteLine("Raspberry " + id + (result ? " succesfully removed"
                                                          : " was not in the list or cannot be removed"));

                rh = null;

                Log.WriteLine("Connected Raspberries: ");
                StringBuilder sb = new StringBuilder();
                foreach (var r in raspberryList)
                {
                    sb.AppendFormat("\tId: {0} -> EndPoint: {1};\n", r.Id, r.EndPoint.ToString());
                }
                Log.WriteLine(sb.ToString());
                sb.Clear();
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

                    Log.WriteLine("Gyanus! MessageType: {0}", messageType);
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