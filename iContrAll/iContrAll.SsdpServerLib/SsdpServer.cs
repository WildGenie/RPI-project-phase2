using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.SsdpServerLib
{
    public class SsdpServer
    {
        private string identifier;

        public string Identifier
        {
            get { return identifier; }
            set { identifier = value; }
        }

        private Thread ssdpListenerThread;

        public SsdpServer(string[] args)
        {
            this.identifier = args.Length > 0 ? args[0] : "urn:schemas-upnp-org:device:RlanDevice:1";

            this.ssdpListenerThread = new Thread(Start);

            this.ssdpListenerThread.Start();            
        }

        void Start()
        {
            using (var listener = new RequestListener(identifier))
            {
                listener.Start();
            }
        }
    }
}
