using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    static class DummyDb
    {
        public static IEnumerable<Device> GetDummyDeviceList(bool isEmpty)
        {
            List<Device> deviceList = new List<Device>();
            if (isEmpty) return deviceList;
            
            deviceList.Add(new Device() { Id = "WC10010001", Name = "TesztDevice1", Channel = 2 });
            deviceList.Add(new Device() { Id = "LC10010101", Name = "TesztDevice2", Channel = 3 });

            return deviceList;
        }
    }
}
