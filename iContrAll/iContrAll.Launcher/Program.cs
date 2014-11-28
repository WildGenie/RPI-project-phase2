using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.Launcher
{
    class Program
    {
        static bool debug = false;
        static void Main(string[] args)
        {
            if (args != null && args.Length > 0 && args[0] == "d")
            {
                debug = true;
            }
            else
                debug = false;
	        Launch();
            Thread.Sleep(Timeout.Infinite);
        }

        private static void Launch()
        {
            InitGPIOPins();

            string versionNumber = string.Empty;
            using (var file = File.OpenText("/home/pi/iContrAll/bin/latestversion.txt"))
            {
                versionNumber = file.ReadLine();
            }

            string exeFile = "iContrAll.TcpServer.exe";

            string filePath = Path.Combine("/home/pi/iContrAll/bin", versionNumber, exeFile);

            Process process = new Process();
            process.StartInfo.FileName = "mono";
            if (debug)
                process.StartInfo.Arguments = "/home/pi/iContrAll/bin/Debug/iContrAll.TcpServer.exe alpha.icontrall.hu 1125 alpha.icontrall.hu /home/pi/iContrAll/ca/server.p12 allcontri";
                //process.StartInfo.Arguments = "/home/pi/iContrAll/bin/Debug/iContrAll.TcpServer.exe 89.133.26.35 1125 alpha.icontrall.hu /home/pi/iContrAll/ca/server.p12 allcontri";
            else
                process.StartInfo.Arguments = filePath + " alpha.icontrall.hu 1125 alpha.icontrall.hu /home/pi/iContrAll/ca/server.p12 allcontri";
                //process.StartInfo.Arguments = "/home/pi/iContrAll/bin/iContrAll.TcpServer.exe 89.133.26.35 1125 alpha.icontrall.hu /home/pi/iContrAll/ca/server.p12 allcontri";
            process.EnableRaisingEvents = true;
            process.Exited += LaunchIfCrashed;

            process.Start();
        }

        private static void LaunchIfCrashed(object o, EventArgs e)
        {
            InitGPIOPins();

            Process process = (Process)o;
            Console.WriteLine("Process crashed: {0}", process.StartInfo.FileName);
            Console.WriteLine(process.ExitCode);
            if (process.ExitCode != 0)
            {
                Launch();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        private static void InitGPIOPins()
        {
            Process process = new Process();
            process.StartInfo.FileName = "sudo";
            
            process.StartInfo.Arguments = "gpio mode 0 out";
            process.Start();
            Thread.Sleep(100);

            process.StartInfo.Arguments = "gpio mode 7 down";
            process.Start();
            Thread.Sleep(100);

            process.StartInfo.Arguments = "gpio mode 7 out";
            process.Start();
            Thread.Sleep(100);
            
	        process.StartInfo.Arguments = "gpio mode 2 in";
            process.Start();

            process.StartInfo.Arguments = "gpio mode 2 up";
            process.Start();
        }
    }
}
