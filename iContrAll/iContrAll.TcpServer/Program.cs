using iContrAll.SsdpServerLib;
using System;
namespace iContrAll.TcpServer
{
    class Program
    {
        static void Main(string[] args)
        {
            new SsdpServer(new string[] { "urn:schemas-upnp-org:device:RlanDevice:1" });
            new Server(1122);
        }
    }
}
