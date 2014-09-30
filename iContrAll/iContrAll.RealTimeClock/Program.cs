using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.RealTimeClock
{
    class Program
    {
        static int fd;
        static void Main(string[] args)
        {
            if (Init.WiringPiSetup() < 0)
            {
                Console.WriteLine("unable to set wiring pi\n");
                return;
            }

            fd = I2C.wiringPiI2CSetup(0x68);
            if (fd < 0) Console.WriteLine("Error opening I2C channel.");
            else Console.WriteLine(fd);

            DateTime now = DateTime.Now;

            string secondStr = Convert.ToString(now.Second, 2);
            Console.WriteLine(secondStr);
            string minuteStr = Convert.ToString(now.Minute, 2);
            Console.WriteLine(minuteStr);
            string hourStr = Convert.ToString(now.Hour, 2);
            Console.WriteLine(hourStr);
            string dayOfWeekStr = Convert.ToString((byte)now.DayOfWeek, 2);
            Console.WriteLine(dayOfWeekStr);
            string dayStr = Convert.ToString(now.Day, 2);
            Console.WriteLine(dayStr);
            string monthStr = Convert.ToString(now.Month, 2);
            Console.WriteLine(monthStr);
            string yearStr = Convert.ToString(now.Year, 2);
            Console.WriteLine(yearStr);

            //I2C.wiringPiI2CWriteReg8(fd, 0, now.Second); // sec
            //I2C.wiringPiI2CWriteReg8(fd, 1, now.Minute); // min
            //I2C.wiringPiI2CWriteReg8(fd, 2, now.Hour); // hour
            //I2C.wiringPiI2CWriteReg8(fd, 3, (byte)now.DayOfWeek); // day
            //I2C.wiringPiI2CWriteReg8(fd, 4, now.Day); // date
            //I2C.wiringPiI2CWriteReg8(fd, 5, now.Month); // month
            //I2C.wiringPiI2CWriteReg8(fd, 6, now.Year % 1000); // year

            for (int i = 0; i < 100; i++)
            {
                Thread.Sleep(1000);
                Read();
            }

        }

        static void Read()
        {
            int s = I2C.wiringPiI2CReadReg8(fd, 0);
            int m = I2C.wiringPiI2CReadReg8(fd, 1);
            int h = I2C.wiringPiI2CReadReg8(fd, 2);
            int dname = I2C.wiringPiI2CReadReg8(fd, 3);
            int d = I2C.wiringPiI2CReadReg8(fd, 4);
            int mo = I2C.wiringPiI2CReadReg8(fd, 5);
            int y = I2C.wiringPiI2CReadReg8(fd, 6);

            string secondStr = Convert.ToString(s, 2);
            Console.WriteLine(secondStr);
            string minuteStr = Convert.ToString(m, 2);
            Console.WriteLine(minuteStr);
            string hourStr = Convert.ToString(h, 2);
            Console.WriteLine(hourStr);
            string dayOfWeekStr = Convert.ToString(dname, 2);
            Console.WriteLine(dayOfWeekStr);
            string dayStr = Convert.ToString(d, 2);
            Console.WriteLine(dayStr);
            string monthStr = Convert.ToString(m, 2);
            Console.WriteLine(monthStr);
            string yearStr = Convert.ToString(y, 2);
            Console.WriteLine(yearStr);


            int c = I2C.wiringPiI2CReadReg8(fd, 7);

            Console.WriteLine("The current datetime is: {6}.{5}.{4}. {3},  {2}:{1}:{0}", y, mo, d, dname, h, m, s);
        }
    }
}
