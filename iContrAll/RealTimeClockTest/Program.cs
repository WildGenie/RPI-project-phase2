using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealTimeClockTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var rtc = new iContrAll.RealTimeClock.RealTimeClock();
            
            //var syncOK = rtc.Synchronize();
            //if (!syncOK) Console.WriteLine("Az idő szinkronizálása nem sikeres.");

            //for (int i = 0; i < 100; i++)
            //{
            //    var now = rtc.Read();
            //    Console.WriteLine("The current datetime is: {0}.{1}.{2}. {3},  {4}:{5}:{6}", now.Year, now.Month, now.Day, now.DayOfWeek, now.Hour, now.Minute, now.Second);

            //    System.Threading.Thread.Sleep(i * 1000);
            //}
            
            var now = rtc.Read();
            Console.WriteLine("The current datetime is: {0}.{1}.{2}. {3},  {4}:{5}:{6}", now.Year, now.Month, now.Day, now.DayOfWeek, now.Hour, now.Minute, now.Second);
        }
    }
}
