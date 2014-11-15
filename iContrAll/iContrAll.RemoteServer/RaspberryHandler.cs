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
            }
            catch (Exception)
            {
                Console.WriteLine("Exception while closing Client connection {0}", Id);
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
                Console.WriteLine("Exception while reading from Raspberry {0}", Id);
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                { Console.WriteLine(ex.InnerException.Message); }
            }
            return numberOfBytesRead;
        }

        public bool Write(byte[] message, int numberOfBytesRead)
        {
            try
            {
                if (!TcpChannel.Connected)
                {
                    Console.WriteLine("Raspberry is not connected {0}", Id);
                    return false;
                }
                if (SslStream.CanWrite)
                {
                    SslStream.Write(message, 0, numberOfBytesRead);
                    Console.WriteLine("SentToRaspberry {0}", Encoding.UTF8.GetString(message, 0, numberOfBytesRead));

                    return true;
                }
                else
                {
                    Console.WriteLine("Cannot write to sslStream at {0}", Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception while trying to write to Raspberry {0}", Id);
                Console.WriteLine(ex.Message);
                if (ex.InnerException != null)
                    Console.WriteLine(ex.InnerException);
                return false;
            }
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
