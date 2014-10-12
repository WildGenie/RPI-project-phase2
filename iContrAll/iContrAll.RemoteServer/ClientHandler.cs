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
    }
}
