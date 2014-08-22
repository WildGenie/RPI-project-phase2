using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    class Device
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        private int channel;
        public int Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        private string name;
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string deviceType;
        public string DeviceType
        {
            get { return deviceType; }
            set { deviceType = value; }
        }

        private List<ActionType> actions;
        public List<ActionType> Actions
        {
            get { return actions; }
            set { actions = value; }
        }

        private string timer;
        public string Timer
        {
            get { return timer; }
            set { timer = value; }
        }

        private int voltage;
        public int Voltage
        {
            get { return voltage; }
            set { voltage = value; }
        }

    }
}
