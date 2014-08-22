using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iContrAll.TcpServer
{
    class Place
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public List<Device> DevicesInPlace { get; set; }
    }
}
