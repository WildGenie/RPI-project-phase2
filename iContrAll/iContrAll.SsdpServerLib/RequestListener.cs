//
// RequestListener.cs
//
// Author:
//   Scott Peterson <lunchtimemama@gmail.com> & Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 S&S Black Ltd.
// Copyright (C) 2008 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Configuration;

namespace iContrAll.SsdpServerLib
{
    class RequestListener : IDisposable
    {
        readonly object mutex = new object();
        Socket socket;

        public const string Address = "239.255.255.250";
        public const ushort Port = 1900;

        #region ResponseIngredients

        string identifier;
        string getLocalIPAddress()
        {
            IPHostEntry host;
            string localIP = "";
            host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIP = ip.ToString();
                    break;
                }
            }
            return localIP;
        }

        private string os = String.Format("{0}/{1}", Environment.OSVersion.Platform, Environment.OSVersion.Version);
        private const string user_agent = "UPnP/1.0";

        private string alive_response =
            "HTTP/1.1 200 OK\r\n" +
            "CACHE-CONTROL: max-age = {0}\r\n" +
            "DATE: {1}\r\n" +
            "EXT:\r\n" +
            "LOCATION: http://{2}:1122\r\n" +
            "SERVER: {3} UPnP/1.1 {4}\r\n" +
            "ST: {5}\r\n" +
            "USN: {6}\r\n" +
            "\r\n";

        #endregion

        public RequestListener(string identifier)
        {
            this.identifier = identifier;
        }

        private Socket createMulticastSocket()
        {
            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.MulticastTimeToLive, 4);
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.AddMembership, new MulticastOption(IPAddress.Parse(Address), 0));

            return socket;
        }

        public void Start()
        {
            lock (mutex)
            {
                Stop();
                socket = createMulticastSocket();

                socket.Bind(new IPEndPoint(IPAddress.Any, Port));
                Console.WriteLine("SSDP server is listening...");
                ReadResult(new ReceiveBuffer(socket));
            }
        }

        public void Stop()
        {
            lock (mutex)
            {
                if (socket != null)
                {
                    socket.Close();
                    socket = null;
                }
            }
        }

        void ReadResult(ReceiveBuffer buffer)
        {
            try
            {
                while (true)
                {
                    buffer.Socket.ReceiveFrom(buffer.Buffer, ref buffer.SenderEndPoint);

                    var dgram = buffer.Buffer;
                    //Console.WriteLine("Received from: {0}: {1}", buffer.SenderIPEndPoint.ToString(), Encoding.ASCII.GetString(dgram));
                    if (dgram != null && dgram.Length > 0 && IsDgramMSearch(dgram))
                    {
                        // Console.WriteLine("Datagram:\n{0}", Encoding.UTF8.GetString(dgram));
                        //Console.WriteLine(Encoding.UTF8.GetString(dgram));
                        //LogHelper.Log.WriteLine("m - search received");
                        if (IsDgramRaspberryId(dgram))
                        {
                            // csak azt írjuk ki, ha jó MSearch jött felénk.
                            LogHelper.Log.WriteLine("M-SEARCH Received from: {0}", buffer.SenderIPEndPoint.ToString());
                            //Console.WriteLine("VÁLASZOLUNK!");
                            Socket responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                            byte[] sendbuf = CreateAliveResponse(getLocalIPAddress(), identifier, identifier, 900);
                            //byte[] sendbuf = Encoding.UTF8.GetBytes(identifier);
                            //Console.WriteLine(Encoding.UTF8.GetString(sendbuf));
                            responseSocket.SendTo(sendbuf, buffer.SenderIPEndPoint);
                            //Console.WriteLine("Response sent to: {0}\n{1}", buffer.SenderIPEndPoint, Encoding.UTF8.GetString(sendbuf));
                        }
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                // Socket disposed while we were receiving from it... just ignore this
            }
        }

        public byte[] CreateAliveResponse(string location, string searchType, string usn, ushort maxAge)
        {
            return Encoding.ASCII.GetBytes(String.Format(
                alive_response, maxAge, DateTime.Now.ToString("r"), location, os, user_agent, searchType, usn));
        }

        //void OnAsyncResultReceived(IAsyncResult asyncResult)
        //{
        //    var buffer = (ReceiveBuffer)asyncResult.AsyncState;

        //    try
        //    {
        //        buffer.BytesReceived = buffer.Socket.EndReceiveFrom(asyncResult, ref buffer.SenderEndPoint);

        //        var dgram = buffer.Buffer;
        //        if (dgram != null && dgram.Length > 0 && IsDgramMSearch(dgram))
        //        {
        //            Socket responseSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        //            byte[] sendbuf = CreateAliveResponse(getLocalIPAddress(), identifier, identifier, 900);

        //            responseSocket.BeginSendTo(sendbuf, 0, sendbuf.Length, SocketFlags.None, buffer.SenderEndPoint, null, null);
        //        }
        //    }
        //    catch (ObjectDisposedException)
        //    {
        //        // Socket already disposed... just ignore this and exit
        //        // TODO: nem történhet meg!
        //        Console.WriteLine();
        //        return;
        //    }

        //    ReadResult(buffer);
        //}
        // TODO: ellenőrizni, hogy ASCII-e
        static readonly byte[] fingerprint_msearch = Encoding.ASCII.GetBytes("M-SEARCH * HTTP/1.1\r\n");

        //static readonly string raspberry_id = "DEVICE:" + ConfigFileManager.ConfigurationManager.LoginId;
        //static readonly byte[] fingerprint_raspberryId = Encoding.ASCII.GetBytes(raspberry_id);

        // TODO: ez szar
        private bool IsDgramMSearch(byte[] check)
        {
            int offSet = 0;
            for (int i = 0; i < check.Length; i++)
            {
                if (ToUpper(check[i]) == fingerprint_msearch[0])
                {
                    offSet = i;
                    break;
                }
            }

            for (int i = 0; i < fingerprint_msearch.Length + offSet; i++)
            {
                if (ToUpper(check[i]) != fingerprint_msearch[i])
                {
                    return false;
                }
            }

            return true;
        }

        private bool IsDgramRaspberryId(byte[] check)
        {
            //int offSet = 0;
            //for (int i = 0; i < check.Length; i++)
            //{
            //    if (ToUpper(check[i]) == fingerprint_raspberryId[0])
            //    {
            //        offSet = i;
            //        break;
            //    }
            //}

            //for (int i = 0; i < fingerprint_raspberryId.Length + offSet; i++)
            //{
            //    if (ToUpper(check[i]) != fingerprint_raspberryId[i])
            //    {
            //        return false;
            //    }
            //}

            string raspberry_id = "DEVICE:" + ConfigFileManager.ConfigurationManager.LoginId;
            //LogHelper.Log.WriteLine("SSDP raspberry_id: {0}", raspberry_id);
            byte[] fingerprint_raspberryId = Encoding.ASCII.GetBytes(raspberry_id);

            int found = 0;
            for (int i = 0; i < check.Length; i++)
            {
                if (check[i] == fingerprint_raspberryId[found])
                {
                    if (++found == fingerprint_raspberryId.Length)
                    {
                        return true;
                    }
                }
                else
                {
                    found = 0;
                }
            }
            return false;
        }

        static byte ToUpper(byte b)
        {
            return (b >= 'a' && b <= 'z') ? (byte)(b - 0x20) : b;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
