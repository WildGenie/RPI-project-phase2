using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace iContrAll.TcpServer
{
    class RemoteServerManager
    {
        string remoteServerAddress;
        int remoteServerPort;
        string serverCertificateName;
        string certificatePath;
        string certificatePassphrase;

        public RemoteServerManager(string remoteServerAddress, int remoteServerPort,
            string serverCertificateName, string certificatePath, string certificatePassphrase)
        {
            this.remoteServerAddress = remoteServerAddress;
            this.remoteServerPort = remoteServerPort;
            this.serverCertificateName = serverCertificateName;
            this.certificatePassphrase = certificatePassphrase;
            this.certificatePath = certificatePath;
        }

        private TcpClient remoteServer;
        private SslStream sslStream;

        public void Connect()
        {
            try
            {
                this.remoteServer = new TcpClient(remoteServerAddress, remoteServerPort);
                Console.WriteLine("Connected to remoteserver");
                this.sslStream = new SslStream(
                    remoteServer.GetStream(),
                    false,
                    new RemoteCertificateValidationCallback(ValidateServerCertificate),
                    new LocalCertificateSelectionCallback(CertificateSelectionCallback)
                );

                X509Certificate cert = new X509Certificate2(certificatePath, certificatePassphrase); //"/home/pi/SslClientTest2/bin/Debug/server.p12", "allcontri");
                X509CertificateCollection certs = new X509CertificateCollection();
                certs.Add(cert);

                // The server name must match the name on the server certificate. 
                try
                {
                    sslStream.AuthenticateAsClient(serverCertificateName,
                        certs,
                        SslProtocols.Tls,
                        false); // check cert revokation);
                }
                catch (AuthenticationException e)
                {
                    Console.WriteLine("Exception: {0}", e.Message);
                    if (e.InnerException != null)
                    {
                        Console.WriteLine("Inner exception: {0}", e.InnerException.Message);
                    }
                    Console.WriteLine("Authentication failed - closing the connection.");
                    remoteServer.Close();
                    return;
                }

                // Sending the device id to the server inside the login message
                byte[] message = Encoding.UTF8.GetBytes(System.Configuration.ConfigurationManager.AppSettings["loginid"]);
                byte[] data = BuildMessage(-1, message);
                //NetworkStream stream = remoteServer.GetStream();
                sslStream.Write(data, 0, data.Length);
                sslStream.Flush();
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("ArgumentNullException: {0}", e);
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
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

        // The following method is invoked by the RemoteCertificateValidationDelegate. 
        public static bool ValidateServerCertificate(
              object sender,
              X509Certificate certificate,
              X509Chain chain,
              SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers. 
            // return false;
            return true;
        }

        static X509Certificate CertificateSelectionCallback(object sender,
            string targetHost,
            X509CertificateCollection localCertificates,
            X509Certificate remoteCertificate,
            string[] acceptableIssuers)
        {
            Console.WriteLine("CertificateSelectionCallback");
            return localCertificates[0];
        }
    }


}
