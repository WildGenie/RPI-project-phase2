using LogHelper;
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


        public bool Connected { get { return TcpChannel.Connected; } }
        public bool CanWrite { get { return SslStream.CanWrite; } }

        public delegate void RemoveRaspberryEH(RaspberryHandler rh);
        public event RemoveRaspberryEH RemoveRaspberry;

		public void Close()
		{
            try
            {
                if (this.RemoveRaspberry != null)
                {
                    this.RemoveRaspberry(this);
                }
                SslStream.Close();
                TcpChannel.Close();
                Log.WriteLine("RaspberryHandler {0} closed", Id);
            }
            catch (Exception)
            {
                Log.WriteLine("Exception while closing Client connection {0} in {1}", Id, "RaspberryHandler.Close()");
            }
		}

        public int Read(byte[] buffer)
        {
            int numberOfBytesRead = -1;
            try
            {
                if (SslStream.CanRead)
                    numberOfBytesRead = SslStream.Read(buffer, 0, buffer.Length);
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
                if (!TcpChannel.Connected)
                {
                    Log.WriteLine("Raspberry is not connected {0} in {1}", Id, "RaspberryHandler.Write()");
                    return false;
                }
                if (SslStream.CanWrite)
                {
                    SslStream.Write(message, 0, numberOfBytesRead);
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

			SslStream.Write(answer);
		}
	}
}
