using iContrAll.SsdpServerLib;
using System;
namespace iContrAll.TcpServer
{
    class Program
    {
        private static void DisplayUsage()
        {
            Console.WriteLine("To start the server specify:");
            Console.WriteLine("iContrAll.TcpServer remoteServerAddress remoteServerPort certificateName certificatePath certificatePassphrase");
            Environment.Exit(1);
        }

        static void Main(string[] args)
        {
            new SsdpServer(new string[] { "urn:schemas-upnp-org:device:RlanDevice:1" });
            string remoteServerAddress = "79.172.214.136";
            int remoteServerPort = 1125;
            string certificateName = "alpha.icontrall.hu";
            string certificatePath = "/home/pi/server.crt";
            string certificatePassphrase = "allcontri";

            if (args==null || args.Length < 5)
            {
                DisplayUsage();
            }
            
            remoteServerAddress = args[0];
            remoteServerPort = int.Parse(args[1]);
            certificateName = args[2];
            certificatePath = args[3];
            certificatePassphrase = args[4];

            int port = int.Parse(System.Configuration.ConfigurationManager.AppSettings["ListeningPort"]);

            new Server(port, remoteServerAddress, remoteServerPort, certificateName, certificatePath, certificatePassphrase);
        }
    }
}
