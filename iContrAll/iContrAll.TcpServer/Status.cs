using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iContrAll.TcpServer
{
    public class Status
    {
        public string DeviceId { get; set; }
        public int DeviceChannel { get; set; }
        public bool State { get; set; }
        public int Value { get; set; }
        public int Power { get; set; }
    }
}
