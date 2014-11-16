using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogHelper
{
    public static class Log
    {
        private static string fileName
        {
            get
            {
                var now = DateTime.Now;
                return string.Format("{0}-{1}-{2}.txt", now.Year, now.Month, now.Day);
            }
        }

        public static void WriteLine(params object[] args)
        {
            try
            {
                if (args == null || args.Length == 0) return;
                if (args.Length > 1)
                {
                    WriteLine(string.Format(args[0].ToString(), args.Skip(1)));
                }
                else WriteLine(args[0].ToString());
            }
            catch (FormatException)
            {
                Write(TimeStampMessage(args[0].ToString() + ","));
                for (int i = 1; i < args.Length - 1; i++)
                {
                    Write(args[i].ToString() + ",");
                }
                WriteLine(args[args.Length - 1]);
            }
        }

        private static async void Write(object s)
        {
            Console.Write(s);
            try
            {
                using (var writer = new StreamWriter(fileName, append: true))
                {
                    await writer.WriteAsync(s.ToString());
                }
            }
            catch(Exception)
            { Console.WriteLine("Log.Write: StreamWriter exception"); }
            
        }

        private static async void WriteLine(string message)
        {
            Console.WriteLine(message);
            try
            {
                using (var writer = new StreamWriter(fileName, append: true))
                {
                    await writer.WriteLineAsync(TimeStampMessage(message));
                }
            }
            catch (Exception)
            { Console.WriteLine("Log.Write: StreamWriter exception"); }
        }

        private static string TimeStampMessage(string message)
        {
            return string.Format("[{0}] {1}", DateTime.Now.ToString("o"), message);
        }
    }
}
