using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.RemoteServer
{
    class RaspberryData
    {
        public int Id { get; set; }
        public TcpClient TcpChannel { get; set; }
    }
}
