using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.SPIRadio
{
    public enum RadioState { None, Send, Receive };

    public sealed class Radio
    {
        #region Singleton members

        private static bool initSuccess = false;

        private static volatile Radio instance;
        private static object syncRoot = new Object();

        private Radio()
        {
            Console.WriteLine("!!!!!!!!!!!!!!NEW INSTANCE!!!!!!!!!!!!!NEW INSTANCE!!!!!!!!!!!!!!NEW INSTANCE!!!!!!!!!!!!");
            // state = (byte)RadioState.None;
            Volatile.Write(ref state, (byte)RadioState.None);

            //data = new byte[RadioConstants.FIX_PACKET_LENGTH];
            Volatile.Write(ref data, new byte[RadioConstants.FIX_PACKET_LENGTH]);
            this.InterruptReceived += Radio_InterruptReceived;
            if (InitRadio())
            {
                Console.WriteLine("Radio init sikeres");
                initSuccess = true;
            }
            else
            {
                Console.WriteLine("Radio init NEM sikeres");
                initSuccess = false;
            }
        }

        public static Radio Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (syncRoot)
                    {
                        if (instance == null)
                        {
                            instance = new Radio();
                        }
                    }
                }
                
                return instance;
            }
        }
        #endregion
        PiThreadInterrupts.ISRCallback interruptMethod;
        private byte state;
        //public byte[] data = new byte[RadioConstants.FIX_PACKET_LENGTH];

        //public delegate void RadioMessageReceivedDelegate(RadioMessageEventArgs eventArgs);
        public delegate void RadioMessageReceivedDelegate(byte[] data);
        public event RadioMessageReceivedDelegate RadioMessageReveived;

        private unsafe bool InitRadio()
        {
            try
            {
                // state = (byte)RadioState.None;
                Volatile.Write(ref state, (byte)RadioState.None);

                if (Init.WiringPiSetup() < 0)
                {
                    Console.WriteLine("unable to set wiring pi\n");
                    initSuccess = false;
                    return false;
                }

                Console.WriteLine("InitWiringPiSetup OK");

                int spiOK = SPI.wiringPiSPISetup(RadioConstants.P, 5000000);
                Console.WriteLine(spiOK);

                // GPIO.pinMode(RadioConstants.PWDN, (int)GPIO.GPIOpinmode.Output);
                
                
                
                //GPIO.digitalWrite(RadioConstants.CS, 0);

                for (int i = 0; i < 4; i++)
                {
                    GPIO.digitalWrite(RadioConstants.PWDN, 1);
                    Thread.Sleep(RadioConstants.DD);
                    GPIO.digitalWrite(RadioConstants.PWDN, 0);
                    Thread.Sleep(RadioConstants.DD);
                }

                Console.WriteLine("GPIO setup OK");

                Volatile.Write(ref interruptMethod, Interrupt0);

                if (PiThreadInterrupts.wiringPiISR(RadioConstants.INT, (int)PiThreadInterrupts.InterruptLevels.INT_EDGE_FALLING, Volatile.Read(ref interruptMethod)) < 0)
                {
                    Console.WriteLine("unable to set interrupt pin\n");
                    initSuccess = false;
                    return false;
                }

                 Console.WriteLine("Interrupt pin setup ok");

                 if (!power_up())
                 {
                     Console.WriteLine("Radio power_up failed");
                     initSuccess = false;
                     return false;
                 }
                Console.WriteLine("power up");

                if (!config_rf_chip(RadioConstants.P))
                {
                    Console.WriteLine("config_rf_chip failed");
                    initSuccess = false;
                    return false;
                }
                Console.WriteLine("config rf chip");

                if (!Clear_Int_Flags(RadioConstants.P))
                {
                    Console.WriteLine("Clear_Int_Flags failed");
                    initSuccess = false;
                    return false;
                }
                if (!RX_Command(RadioConstants.P))
                {
                    Console.WriteLine("RX_Command failed");
                    initSuccess = false;
                    return false;
                }
                initSuccess = true;
                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("RADIO INIT EXCEPTION");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                initSuccess = false;
                return false;
            }
        }

        private static object sendLock = new object();
        public unsafe bool SendMessage(byte[] message)
        {
            lock (sendLock)
            {
                try
                {
                    if (!initSuccess)
                    {
                        if (InitRadio())
                        {
                            Console.WriteLine("Radio init sikeres");
                            initSuccess = true;
                        }
                        else
                        {
                            Console.WriteLine("Radio init NEM sikeres");
                            // TODO: felesleges sor
                            initSuccess = false;
                            return false;
                        }
                    }
                }
                catch(Exception)
                {
                    initSuccess = false;
                    return false;
                }
                try
                {
                    Console.WriteLine("SendMessage {0}", Encoding.UTF8.GetString(message));
                    byte[] bytesToSend = new byte[RadioConstants.FIX_PACKET_LENGTH];

                    Array.Copy(message, 0, bytesToSend, 0, message.Length);
                    if (message.Length > 62) return false;

                    // maradék 0
                    for (int i = message.Length; i < 62; i++)
                    {
                        bytesToSend[i] = 0x08;
                    }
                    // kocsi vissza
                    bytesToSend[62] = 10;
                    bytesToSend[63] = 13;

                    // Console.WriteLine("this.ToString() a data összerakás után" + this.ToString());
                    // írás
                    Write_Tx_Fifo(RadioConstants.P, bytesToSend);
                    // Console.WriteLine("this.ToString() a write tx fifo után" + this.ToString());
                    Clear_Int_Flags(RadioConstants.P);
                    // Console.WriteLine("this.ToString() a clear int flags után" + this.ToString());
                    TX_Command(RadioConstants.P);
                    // Console.WriteLine("this.ToString() a txcommand után" + this.ToString());


                    Thread.Sleep(RadioConstants.DD);
                    Console.WriteLine("Elvileg a kiküldés végére ér");
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("EXCEPTION A 'RadioCommunication.SendMessage'-ben!!!");
                    Console.WriteLine(e.Message);
                    Console.WriteLine(e.StackTrace);

                    return false;
                }
            }
        }

        unsafe bool CTS()
        {
            int c = 0;
            byte a = 0;
            while (a != 255 && c < 255)
            {
                byte[] x = new byte[] { RadioConstants.CMD_CTS_READ, 0 };
                fixed (byte* pX = x)
                {
                    if (SPI.wiringPiSPIDataRW(0, pX, x.Length) == -1) return false;
                    a = pX[1];
                }
                c++;
            }

            if (c >= 255)
            {
                Console.WriteLine("CTS failed");
                return false;
            }
            return true;
        }

        byte[] data;

        unsafe void Interrupt0()
        {
            Console.WriteLine("interrup ugras eleje ok");
            try
            {
                Console.WriteLine("state ok: " + Volatile.Read(ref state));
                InterruptReceivedDelegate copy = Volatile.Read(ref this.InterruptReceived);

                if (copy!=null)
                {
                    copy();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("EXCEPTION AZ 'Interrupt0'-ban!!!!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        private delegate void InterruptReceivedDelegate();
        private event InterruptReceivedDelegate InterruptReceived;

        void Radio_InterruptReceived()
        {
            try
            {
                if (Volatile.Read(ref state) == (byte)RadioState.Receive)
                {
                    Console.WriteLine("packet received");
                    if (!ReadCRCError())
                    {
                        Read_Rx_Fifo(RadioConstants.P);
                    }
                    else
                    {
                        Console.WriteLine("de CRC hiba");
                    }

                    Clear_Int_Flags(RadioConstants.P);
                    RX_Command(RadioConstants.P);
                    
                    //byte rssi = ReadRSSI(RadioConstants.P);
                    //var eArgs = new RadioMessageEventArgs(data, 0, rssi);
                    
                    RadioMessageReceivedDelegate tempEvent = Volatile.Read(ref RadioMessageReveived);
                    if (tempEvent != null)
                    {
                        //tempEvent(Volatile.Read(ref eArgs));
                        tempEvent(Volatile.Read(ref data));
                    }
                }

                if (Volatile.Read(ref state) == (byte)RadioState.Send)
                {
                    Console.WriteLine("packet sent");
                    //GPIO.digitalWrite(RadioConstants.TXRX, 0);
                    Clear_Int_Flags(RadioConstants.P);
                    RX_Command(RadioConstants.P);
                }
            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("NullReferenceException az 'Radio_InterruptReceived'-ben!!!!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        unsafe void Read_Rx_Fifo(int p)
        {
            byte[] tempData = new byte[RadioConstants.FIX_PACKET_LENGTH + 1];
            tempData[0] = RadioConstants.CMD_RX_FIFO_READ;

            for (int i = 0; i < RadioConstants.FIX_PACKET_LENGTH; i++)
            {
                tempData[i + 1] = 0;
            }


            fixed (byte* pData = tempData)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                SPI.wiringPiSPIDataRW(p, pData, RadioConstants.FIX_PACKET_LENGTH + 1);
                // GPIO.digitalWrite(RadioConstants.CS, 1);
                //Console.WriteLine("Read_Rx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(tempData));

            }

            //byte[] readMessage = new byte[RadioConstants.FIX_PACKET_LENGTH];
            for (int i = 0; i < RadioConstants.FIX_PACKET_LENGTH; i++)
            {
                //data[i] = tempData[i + 1];
                Volatile.Write(ref data[i], tempData[i + 1]);
            }

            CTS();
            //Console.WriteLine("Read_Rx_Fifo: CTS()");
            byte[] t = new byte[] { 0x15, 0x03 };


            fixed (byte* pT = t)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                SPI.wiringPiSPIDataRW(p, pT, 2);
                //GPIO.digitalWrite(RadioConstants.CS, 1);
              //  Console.WriteLine("Read_Rx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(t));
            }

            CTS();
            // Console.WriteLine("Read_Rx_Fifo: CTS()");

            //return readMessage;
        }

        unsafe bool Clear_Int_Flags(int p)
        {
            byte[] a = new byte[] { RadioConstants.CMD_GET_INT_STATUS, 0, 0, 0 };
            fixed (byte* pA = a)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                if (SPI.wiringPiSPIDataRW(p, pA, a.Length) == -1) return false;
                // GPIO.digitalWrite(RadioConstants.CS, 1);
               // Console.WriteLine("Clear_Int_Flags: wiringPiSPIDataRW " + Encoding.UTF8.GetString(a));
            }
            if (!CTS()) return false;
            return true;
            // Console.WriteLine("Clear_Int_Flags: CTS()");
        }

        unsafe bool RX_Command(int p)
        {
            //state = (byte)RadioState.Receive;
            Volatile.Write(ref state, (byte)RadioState.Receive);

            byte[] d = new byte[] { RadioConstants.CMD_START_RX, 0, 0, 0, RadioConstants.FIX_PACKET_LENGTH, 0, 0, 0 };
            fixed (byte* pD = d)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                if (SPI.wiringPiSPIDataRW(p, pD, d.Length) == -1) return false;
                // GPIO.digitalWrite(RadioConstants.CS, 1);
                // Console.WriteLine("RX_Command: wiringPiSPIDataRW " + Encoding.UTF8.GetString(d));
            }
            if (!CTS()) return false;
            // Console.WriteLine("RX_Command: CTS()");
            return true;
        }

        unsafe bool power_up()
        {
            GPIO.digitalWrite(RadioConstants.PWDN, 1);
            Thread.Sleep(RadioConstants.D);
            GPIO.digitalWrite(RadioConstants.PWDN, 0);
            Thread.Sleep(RadioConstants.D);
            byte[] pwr = new byte[3] { RadioConstants.CMD_POWER_UP, 1, 0 };

            Console.WriteLine("Na lássuk");
            GPIO.digitalWrite(0, 1);
            fixed (byte* pPwr = pwr)
            {
                //GPIO.digitalWrite(RadioConstants.CS, 0);
                if (SPI.wiringPiSPIDataRW(RadioConstants.P, pPwr, 3)==-1)
                    return false;
                //GPIO.digitalWrite(RadioConstants.CS, 1);
            }


            if (!CTS())
            { return false; }

            Console.WriteLine("Ment!");

            pwr[0] = RadioConstants.CMD_GET_INT_STATUS;
            fixed (byte* pPwr = pwr)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                if (SPI.wiringPiSPIDataRW(RadioConstants.P, pPwr, 1) == -1) return false;
                // GPIO.digitalWrite(RadioConstants.CS, 1);
            }
            if (!CTS())
            { return false; }

            Console.WriteLine("Ez is");
            return true;
        }

        unsafe bool config_rf_chip(int p)
        {
            //	unsigned char config[30][17];
            int i, j, k;
            byte len;
            byte[] tempData = new byte[16];
            byte[] ARRAY = RadioConstants.RADIO_CONFIGURATION_DATA_ARRAY;
            k = 0;
            len = 1;
            for (i = 0; len > 0; i++)
            {
                len = ARRAY[i + k];
                // ????????????????????????????
                if (len > 0)
                    for (j = 1; j < (int)len + 1; j++)
                        tempData[j - 1] = ARRAY[i + j + k];
                if (len > 0)
                {
                    fixed (byte* pData = tempData)
                    {
                        // GPIO.digitalWrite(RadioConstants.CS, 0);
                        if (SPI.wiringPiSPIDataRW(p, pData, len) == -1)
                            return false;
                        // GPIO.digitalWrite(RadioConstants.CS, 1);
                    }
                }
                k += len;
            }
            return true;
        }

        unsafe void Write_Tx_Fifo(int p, byte[] x)
        {
            byte[] tempData = new byte[RadioConstants.FIX_PACKET_LENGTH + 1];
            for (int i = 0; i < RadioConstants.FIX_PACKET_LENGTH; i++)
            {
                tempData[i + 1] = x[i];
            }
            tempData[0] = RadioConstants.CMD_TX_FIFO_WRITE;
            fixed (byte* pData = tempData)
            {
               //  GPIO.digitalWrite(RadioConstants.CS, 0);
                SPI.wiringPiSPIDataRW(p, pData, RadioConstants.FIX_PACKET_LENGTH + 1);
                // GPIO.digitalWrite(RadioConstants.CS, 1);
               // Console.WriteLine("Write_Tx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(tempData));
            }
            CTS();
            // Console.WriteLine("Write_Tx_Fifo: CTS()");
        }

        unsafe void TX_Command(int p)
        {
            // state = (byte)RadioState.Send;
            Volatile.Write(ref state, (byte)RadioState.Send);

            // GPIO.digitalWrite(RadioConstants.TXRX, 1); 
            
            byte[] d = new byte[] { RadioConstants.CMD_START_TX, 0, 0, 0, RadioConstants.FIX_PACKET_LENGTH, 0 };
            fixed (byte* pD = d)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                SPI.wiringPiSPIDataRW(p, pD, d.Length);
                // GPIO.digitalWrite(RadioConstants.CS, 1);
                // Console.WriteLine("TX_Command: wiringPiSPIDataRW " + Encoding.UTF8.GetString(d));
            }
            CTS();
            // Console.WriteLine("TX_Command: CTS()");
        }

        unsafe bool ReadCRCError()
        {
            byte t;

            byte[] toRFChip = new byte[6];
            toRFChip[0] = RadioConstants.CMD_GET_INT_STATUS;
            toRFChip[1] = 0x00;
            fixed (byte* pToRFChip = toRFChip)
            {
                SPI.wiringPiSPIDataRW(RadioConstants.P, pToRFChip, 2);
            }
            toRFChip[0] = RadioConstants.CMD_CTS_READ;
            toRFChip[1] = 0x00;
            toRFChip[2] = 0x00;
            toRFChip[3] = 0x00;
            toRFChip[4] = 0x00;
            toRFChip[5] = 0x00;
            fixed (byte* pToRFChip = toRFChip)
            {
                // GPIO.digitalWrite(RadioConstants.CS, 0);
                SPI.wiringPiSPIDataRW(RadioConstants.P, pToRFChip, 6);
                // GPIO.digitalWrite(RadioConstants.CS, 1);
                t = pToRFChip[5];
            }
            //Console.WriteLine("ReadCRCError Return előtt");
            return (t & 0x08)>0;

        }

        unsafe byte ReadRSSI(int p)
        {
            byte[] a = new byte[]{RadioConstants.CMD_GET_MODEM_STATUS,0};
            fixed (byte* pA = a)
            {
                SPI.wiringPiSPIDataRW(p, pA, a.Length);
            }
            Timing.delayMicroseconds(10);
            byte[] b = new byte[] { RadioConstants.CMD_CTS_READ, 0, 0, 0, 0 };
            fixed (byte* pB = b)
            {
                SPI.wiringPiSPIDataRW(p, pB, b.Length);
            }
            CTS();
            return b[4];
        }
    }
}
