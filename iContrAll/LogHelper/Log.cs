using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogHelper
{
	public static class Log
	{
		private static object logLockObject = new object();

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
					WriteLine(string.Format(args[0].ToString(), args.Skip(1).ToArray()));
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

		private static void Write(object s)
		{
			Console.Write(s);
			try
			{
				lock (logLockObject)
				{
					using (var writer = new StreamWriter(fileName, append: true))
					{
						writer.Write(s.ToString());
					}
				}
			}
			catch(Exception ex)
			{ Console.WriteLine("Log.Write: StreamWriter exception"); Console.WriteLine(ex); }
		}

		private static void WriteLine(string message)
		{
			Console.WriteLine(message);
			try
			{
				lock (logLockObject)
				{
					using (var writer = new StreamWriter(fileName, append: true))
					{
						writer.WriteLine(TimeStampMessage(message));
					}
				}
			}
			catch (Exception ex)
			{ Console.WriteLine("Log.Write: StreamWriter exception"); Console.WriteLine(ex); }
		}

		public static void WriteByteArray(byte[] array)
		{
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < array.Length; i++)
			{
				sb.Append(array[i].ToString());
				sb.Append("|");
            }
            WriteLine(sb.ToString());
        }

        private static string TimeStampMessage(string message)
        {
            return string.Format("[{0}] {1}", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture), message);
        }
    }
}
