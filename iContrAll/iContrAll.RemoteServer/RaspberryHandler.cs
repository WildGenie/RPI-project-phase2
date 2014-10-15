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



		internal void Close()
		{
			SslStream.Close();
			TcpChannel.Close();
		}

		internal byte[] Read()
		{
			byte[] readBuffer = new byte[32768];
			int numberOfBytesRead = SslStream.Read(readBuffer, 0, readBuffer.Length);

			return readBuffer;
		}

		internal void Write(byte[] sendBuffer)
		{
			SslStream.Write(sendBuffer);
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
