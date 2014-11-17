using LogHelper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace iContrAll.RemoteServer
{
	class RaspberryHandler
	{
		public string Id { get; set; }
        private TcpClient tcpChannel;
        private SslStream sslStream;

        public string EndPoint;

        //public bool Connected { get { return tcpChannel.Connected; } }
        //public bool CanWrite { get { return sslStream.CanWrite; } }

        private List<RemoveRaspberryEH> delegates = new List<RemoveRaspberryEH>();

        public delegate void RemoveRaspberryEH(RaspberryHandler rh);

        private event RemoveRaspberryEH removeRaspberry;

        public event RemoveRaspberryEH RemoveRaspberry
        {
            add
            {
                removeRaspberry += value;
                delegates.Add(value);
            }
            remove
            {
                removeRaspberry -= value;
                delegates.Remove(value);
            }
        }

        Timer lastPingTimer;
        bool hadPing = false;

        public RaspberryHandler(TcpClient raspberryTcpClient, System.Net.Security.SslStream sslStream, RemoveRaspberryEH removeRaspberryEH)
        {
            this.tcpChannel = raspberryTcpClient; 
            this.EndPoint = this.tcpChannel.Client.RemoteEndPoint.ToString();
            this.sslStream = sslStream;
            this.RemoveRaspberry += removeRaspberryEH;
            // 5 percenként ellenőriz, az első ellenőrzés 5 percnél, utána igazából semmit, mert eltűntetjük
            this.lastPingTimer = new Timer(lastPingTimerCallback, null, 300000, 300000);
        }

        public void ResetTimeOutTimer()
        {
            hadPing = true;
        }

        private void lastPingTimerCallback(object state)
        {
            if (this.hadPing)
            {
                this.hadPing = false;
            }
            else
            {
                this.Close();
            }
        }

		public void Close()
		{
            try
            {
                if (this.lastPingTimer != null)
                {
                    this.lastPingTimer.Dispose();
                }
                sslStream.Close();
                if (sslStream!=null) sslStream.Dispose();
                tcpChannel.Close();

                if (this.removeRaspberry!=null)
                {
                    this.removeRaspberry(this);
                }

                foreach (var d in delegates)
                {
                    removeRaspberry -= d;
                }
                delegates.Clear();

                Log.WriteLine("RaspberryHandler {0} closed", Id);
            }
            catch (Exception)
            {
                Log.WriteLine("Exception while closing Raspberry connection {0} in {1}", Id, "RaspberryHandler.Close()");
            }
		}

        public int Read(byte[] buffer)
        {
            int numberOfBytesRead = -1;
            try
            {
                if (sslStream.CanRead)
                    numberOfBytesRead = sslStream.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception while reading from Raspberry {0} in {1}", Id, "RaspberryHandler.Read()");
                Log.WriteLine(ex.ToString());
            }
            return numberOfBytesRead;
        }

        public bool Write(byte[] message, int numberOfBytesRead)
        {
            try
            {
                if (!tcpChannel.Connected)
                {
                    Log.WriteLine("Raspberry is not connected {0} in {1}", Id, "RaspberryHandler.Write()");
                    return false;
                }
                if (sslStream.CanWrite)
                {
                    sslStream.Write(message, 0, numberOfBytesRead);
                    sslStream.Flush();
                    Log.WriteLine("SentToRaspberry {1} {0} in {2}", Encoding.UTF8.GetString(message, 0, numberOfBytesRead), Id, "RaspberryHandler.Write()");

                    return true;
                }
                else
                {
                    Log.WriteLine("Cannot write to sslStream at {0} in {1}", Id, "RaspberryHandler.Write()");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception while trying to write to Raspberry {0} in {1}", Id, "RaspberryHandler.Write()");
                Log.WriteLine(ex.ToString());
            }
            return false;
        }

		internal void SendCreateTunnelFor(string msg)
		{
			byte[] message = Encoding.UTF8.GetBytes(msg);

			byte[] msgNbrArray = new byte[4];
			Array.Copy(BitConverter.GetBytes((int)MessageType.CreateThreadFor), msgNbrArray, msgNbrArray.Length);
			
			byte[] lengthArray = new byte[4];
			Array.Copy(BitConverter.GetBytes(message.Length), lengthArray, lengthArray.Length);
			
			byte[] answer = new byte[4 + 4 + message.Length];

			System.Buffer.BlockCopy(msgNbrArray, 0, answer, 0, msgNbrArray.Length);
			System.Buffer.BlockCopy(lengthArray, 0, answer, msgNbrArray.Length, lengthArray.Length);
			System.Buffer.BlockCopy(message, 0, answer, msgNbrArray.Length + lengthArray.Length, message.Length);

			sslStream.Write(answer);
            sslStream.Flush();
		}
	}
}
