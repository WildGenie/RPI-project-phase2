using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.RealTimeClock
{
    // TODO: IDisposable
    public class RealTimeClock
    {
        int fd;
        bool initOK = false;

        public bool Synchronize()
        {
            if (!initOK) fd = InitI2C();

            try
            {
                DateTime now = new NetworkTime().GetDateTime(true);

                Console.WriteLine("The current date and time: {0:yyyy/MM/dd H:mm:ss}", now); 

                int secondToRTC = Nbr2BcdInt(now.Second);
                int minuteToRTC = Nbr2BcdInt(now.Minute);
                int hourToRTC = Nbr2BcdInt(now.Hour);
                int dayOfWeekToRTC = Nbr2BcdInt((byte)now.DayOfWeek);
                int dayToRTC = Nbr2BcdInt(now.Day);
                int monthToRTC = Nbr2BcdInt(now.Month);
                int yearToRTC = Nbr2BcdInt(now.Year % 100);

                I2C.wiringPiI2CWriteReg8(fd, 0, secondToRTC); // sec
                I2C.wiringPiI2CWriteReg8(fd, 1, minuteToRTC); // min
                I2C.wiringPiI2CWriteReg8(fd, 2, hourToRTC); // hour
                I2C.wiringPiI2CWriteReg8(fd, 3, (byte)dayOfWeekToRTC); // day
                I2C.wiringPiI2CWriteReg8(fd, 4, dayToRTC); // date
                I2C.wiringPiI2CWriteReg8(fd, 5, monthToRTC); // month
                I2C.wiringPiI2CWriteReg8(fd, 6, yearToRTC % 100); // year

                Console.WriteLine("The RTC has been set to: (in BCD): {0}.{1}.{2}. {3},  {4}:{5}:{6}",
                    yearToRTC, monthToRTC, dayToRTC, dayOfWeekToRTC, hourToRTC, minuteToRTC, secondToRTC);

                return true;
            }
            catch (NoServerFoundException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
            //catch (Exception ex)
            //{
            //    Console.WriteLine(ex.Message);
            //    return false;
            //}
        }

        private int InitI2C()
        {
            if (Init.WiringPiSetup() < 0)
            {
                Console.WriteLine("unable to set wiring pi\n");
                return -1;
            }

            int fd = I2C.wiringPiI2CSetup(0x68);
            if (fd < 0)
            {
                Console.WriteLine("Error opening I2C channel.");
            }

            initOK = true;

            return fd;
        }

        /// <summary>
        /// Visszatér a tizedesjegyekként feldarabolt számból byte-onként képzett binárisokból álló tömbbel (BCD)  
        /// </summary>
        /// <param name="nbr">A paraméterként várt kétjegyű szám.</param>
        /// <returns>E.g. 24 => |0010|0100|</returns>
        int Nbr2BcdInt(int nbr)
        {
            if (nbr < 0 || nbr > 99) throw new ArgumentException();

            string[] bcd = new string[2];

            string b1 = Convert.ToString(nbr % 10, 2);
            int l = b1.Length;
            for (int i = 0; i < 4 - l; i++)
            {
                b1 = '0' + b1;
            }
            if (nbr < 10)
            {
                bcd = new string[] { "0000", b1 };
            }
            else
            {
                string b2 = Convert.ToString(nbr / 10, 2);
                l = b2.Length;
                for (int i = 0; i < 4 - l; i++)
                {
                    b2 = '0' + b2;
                }
                bcd = new string[] { b2, b1 };
            }
            int bcdInt = Convert.ToInt32(bcd[0] + bcd[1], 2);

            // Console.WriteLine("Nbr2Bcd: {0} => {1}|{2} => {3}", nbr, bcd[0], bcd[1], bcdInt);

            return bcdInt;

            //int b1 = Convert.ToInt32(Convert.ToString(nbr % 10, 2), 2);
            //int b2 = Convert.ToInt32(Convert.ToString(nbr / 10, 2), 2);
        }

        /// <summary>
        /// Converts a 2 byte BCD format number to an integer 
        /// </summary>
        /// <param name="bcd"></param>
        int Bcd2Int(int bcd)
        {
            string bin = Convert.ToString(bcd, 2);
            int l = bin.Length;

            // kiegészítjük 8 bit-re
            for (int i = 0; i < 8 - l; i++)
            {
                bin = '0' + bin;
            }
            
            int nbr = Convert.ToInt32(bin.Substring(0, 4), 2) * 10 + Convert.ToInt32(bin.Substring(4, 4), 2);

            //Console.WriteLine("Bcd2Int: {0} => {1} => {2}", bcd, bin, nbr);

            return nbr;
        }

        public DateTime Read()
        {
            if (!initOK) fd = InitI2C();

            int s = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 0));
            int m = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 1));
            int h = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 2));
            // int dname = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 3));
            int d = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 4));
            int mo = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 5));
            int y = Bcd2Int(I2C.wiringPiI2CReadReg8(fd, 6));
            
            // TODO: legkésőbb 2099. december 31-én megoldást találni. A jelenlegi eszközön az RTC csak kétszámjegyű éveket tárol. Kelt: 2014. október 3.
            y+=2000;

            //int c = I2C.wiringPiI2CReadReg8(fd, 7);
            // Console.WriteLine("The current datetime is: {0}.{1}.{2}. {3},  {4}:{5}:{6}", y, mo, d, dname, h, m, s);
            DateTime now = new DateTime(y, mo, d, h, m, s);
            

            return now;
        }
    }
}
