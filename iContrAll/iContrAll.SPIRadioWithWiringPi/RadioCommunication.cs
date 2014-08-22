using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.SPIRadio
{
    public class RadioCommunication
    {
        enum State { None, Send, Receive };

        State state;
        byte[] data = new byte[RadioConstants.FIX_PACKET_LENGTH];

        public RadioCommunication()
        {
            state = State.None;
        }

        public delegate void RadioMessageReceivedDelegate(RadioMessageEventArgs e);
        public event RadioMessageReceivedDelegate RadioMessageReveived;

        public bool InitRadio()
        {
            try
            {
                state = State.None;

                if (Init.WiringPiSetup() < 0)
                {
                    Console.WriteLine("unable to set wiring pi\n");
                    return false;
                }

                Console.WriteLine("InitWiringPiSetup OK");

                GPIO.pinMode(1, (int)GPIO.GPIOpinmode.Output);
                SPI.wiringPiSPISetup(0, 5000000);
                GPIO.pinMode(RadioConstants.PWDN, (int)GPIO.GPIOpinmode.Output);

                for (int i = 0; i < 4; i++)
                {
                    GPIO.digitalWrite(RadioConstants.PWDN, 1);
                    Thread.Sleep(RadioConstants.DD);
                    GPIO.digitalWrite(RadioConstants.PWDN, 0);
                    Thread.Sleep(RadioConstants.DD);
                }

                Console.WriteLine("GPIO setup OK");

                if (PiThreadInterrupts.wiringPiISR(RadioConstants.INT, (int)PiThreadInterrupts.InterruptLevels.INT_EDGE_FALLING, Interrupt0) < 0)
                {
                    Console.WriteLine("unable to set interrupt pin\n");
                    return false;
                }

                Console.WriteLine("Interrupt pin setup ok");

                power_up();
                Console.WriteLine("power up");

                config_rf_chip(RadioConstants.P);
                Console.WriteLine("config rf chip");

                Clear_Int_Flags(RadioConstants.P);
                RX_Command(RadioConstants.P);

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("RADIO INIT EXCEPTION");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return false;
            }
        }
        
        //public bool SendMessage(string senderId, string targetId, string hexa, 
        //                    string channels, string voltage, byte[]chDim)
        public bool SendMessage(byte[] message)
        {
            try
            {
                data = new byte[RadioConstants.FIX_PACKET_LENGTH];

                //byte[] sendMessageBytes = Encoding.UTF8.GetBytes(senderId+targetId+hexa+"x"+channels+voltage);

                //Array.Copy(sendMessageBytes, 0, data, 0, sendMessageBytes.Length);


                //// TODO: ez rendes karakteresen jöjjön
                //// Array.Copy(channels, 0, data, 11, 4);

                ////Array.Copy(chVoltage, 0, data, 15, 4);

                //Array.Copy(chDim, 0, data, 19, 4);

                //byte[] sendMessageBytes = Encoding.UTF8.GetBytes(msg);
                Array.Copy(message, 0, data, 0, message.Length);
                if (message.Length > 62) return false;

                // maradék 0
                for (int i = message.Length; i < 62; i++)
                {
                    data[i] = 0x08;
                }
                // kocsi vissza
                data[62] = 10;
                data[63] = 13;

                Console.WriteLine("this.ToString() a data összerakás után" + this.ToString());
                // írás
                Write_Tx_Fifo(RadioConstants.P, data);
                Console.WriteLine("this.ToString() a write tx fifo után" + this.ToString());
                Clear_Int_Flags(RadioConstants.P);
                Console.WriteLine("this.ToString() a clear int flags után" + this.ToString());
                TX_Command(RadioConstants.P);
                Console.WriteLine("this.ToString() a txcommand után" + this.ToString());
                Console.WriteLine("Elvileg a kiküldés végére ér");

                Thread.Sleep(RadioConstants.DD);

                return true;
            }
            catch(Exception e)
            {
                Console.WriteLine("EXCEPTION A 'SendMessage'-ben!!!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                
                return false;
            }
        }

        unsafe void CTS()
        {
            byte a = 0;
            while (a != 255)
            {
                byte[] x = new byte[] { RadioConstants.CMD_CTS_READ, 0 };
                fixed (byte* pX = x)
                {
                    SPI.wiringPiSPIDataRW(0, pX, x.Length);
                }
                a = x[1];
            }
        }

        unsafe void Interrupt0()
        {
            try
            {
                Console.WriteLine("interrup ugras eleje ok");
                Console.WriteLine("this.ToString()" + this.ToString());
                Console.WriteLine("data: "+ Encoding.UTF8.GetString(this.data));
                Console.WriteLine("state: " + state);

                if (state == State.Receive)
                {
                    Console.WriteLine("packet received");
                    Read_Rx_Fifo(RadioConstants.P, data);
                    Clear_Int_Flags(RadioConstants.P);
                    RX_Command(RadioConstants.P);

                    string s = Encoding.UTF8.GetString(data);
                    Console.WriteLine("Interrupt, message received: " + s);
                    if (RadioMessageReveived != null)
                    {
                        RadioMessageReveived(new RadioMessageEventArgs(s, 0));
                    }

                    //Console.WriteLine(s);
                }

                if (state == State.Send)
                {
                    Console.WriteLine("packet sent");
                    GPIO.digitalWrite(RadioConstants.TXRX, 0);
                    Clear_Int_Flags(RadioConstants.P);
                    RX_Command(RadioConstants.P);
                }
            }
            catch(Exception e )
            {
                Console.WriteLine("EXCEPTION AZ 'Interrupt0'-ban!!!!");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                
                if (RadioMessageReveived != null)
                {
                    RadioMessageReveived(new RadioMessageEventArgs(string.Empty, -1));
                }
            }
        }

        unsafe void Read_Rx_Fifo(int p, byte[] x)
        {
            byte[] tempData = new byte[RadioConstants.FIX_PACKET_LENGTH + 1];
            tempData[0] = RadioConstants.CMD_RX_FIFO_READ;

            for (int i = 0; i < RadioConstants.FIX_PACKET_LENGTH; i++)
            {
                tempData[i + 1] = 0;
            }


            fixed (byte* pData = tempData)
            {
                SPI.wiringPiSPIDataRW(p, pData, RadioConstants.FIX_PACKET_LENGTH + 1);
                Console.WriteLine("Read_Rx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(tempData));

            }


            for (int i = 0; i < RadioConstants.FIX_PACKET_LENGTH; i++)
            {
                x[i] = tempData[i + 1];
            }

            CTS();
            Console.WriteLine("Read_Rx_Fifo: CTS()");
            byte[] t = new byte[] { 0x15, 0x03 };


            fixed (byte* pT = t)
            {
                SPI.wiringPiSPIDataRW(p, pT, 2);
                Console.WriteLine("Read_Rx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(t));
            }

            CTS();
            Console.WriteLine("Read_Rx_Fifo: CTS()");
        }

        unsafe void Clear_Int_Flags(int p)
        {
            byte[] a = new byte[] { RadioConstants.CMD_GET_INT_STATUS, 0, 0, 0 };
            fixed (byte* pA = a)
            {
                SPI.wiringPiSPIDataRW(p, pA, a.Length);
                Console.WriteLine("Clear_Int_Flags: wiringPiSPIDataRW " + Encoding.UTF8.GetString(a));
            }
            CTS();
            Console.WriteLine("Clear_Int_Flags: CTS()");
        }

        unsafe void RX_Command(int p)
        {
            state = State.Receive;
            byte[] d = new byte[] { RadioConstants.CMD_START_RX, 0, 0, 0, RadioConstants.FIX_PACKET_LENGTH, 0, 0, 0 };
            fixed (byte* pD = d)
            {
                SPI.wiringPiSPIDataRW(p, pD, d.Length);
                Console.WriteLine("RX_Command: wiringPiSPIDataRW " + Encoding.UTF8.GetString(d));
            }
            CTS();
            Console.WriteLine("RX_Command: CTS()");
        }

        unsafe void power_up()
        {
            GPIO.digitalWrite(RadioConstants.PWDN, 1);
            Thread.Sleep(RadioConstants.D);
            GPIO.digitalWrite(RadioConstants.PWDN, 0);
            Thread.Sleep(RadioConstants.D);
            byte[] pwr = new byte[3] { RadioConstants.CMD_POWER_UP, 1, 0 };
            fixed (byte* pPwr = pwr)
            {
                SPI.wiringPiSPIDataRW(RadioConstants.P, pPwr, 3);
            }
            CTS();
            pwr[0] = RadioConstants.CMD_GET_INT_STATUS;
            fixed (byte* pPwr = pwr)
            {
                SPI.wiringPiSPIDataRW(RadioConstants.P, pPwr, 3);
            }
            CTS();
        }

        unsafe void config_rf_chip(int p)
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
                        SPI.wiringPiSPIDataRW(p, pData, len);
                    }
                }
                k += len;
            }
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
                SPI.wiringPiSPIDataRW(p, pData, RadioConstants.FIX_PACKET_LENGTH + 1);
                Console.WriteLine("Write_Tx_Fifo: wiringPiSPIDataRW " + Encoding.UTF8.GetString(tempData));
            }
            CTS();
            Console.WriteLine("Write_Tx_Fifo: CTS()");
        }

        unsafe void TX_Command(int p)
        {
            state = State.Send; Console.WriteLine("TX_Command: state changed to: " + state);
            GPIO.digitalWrite(RadioConstants.TXRX, 1); Console.WriteLine("TX_Command: txrx = > 1");
            byte[] d = new byte[] { RadioConstants.CMD_START_TX, 0, 0, 0, RadioConstants.FIX_PACKET_LENGTH, 0 };
            fixed (byte* pD = d)
            {
                SPI.wiringPiSPIDataRW(p, pD, d.Length);
                Console.WriteLine("TX_Command: wiringPiSPIDataRW " + Encoding.UTF8.GetString(d));
            }
            CTS();
            Console.WriteLine("TX_Command: CTS()");
        }
    }
}
