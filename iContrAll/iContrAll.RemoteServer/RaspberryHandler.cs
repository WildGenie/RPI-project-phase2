using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace iContrAll.RemoteServer
{
    class RaspberryHandler
    {
        public string Id { get; set; }
        public TcpClient TcpChannel { get; set; }
        public SslStream SslStream { get; set; }


    }
}
