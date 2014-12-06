using LogHelper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace iContrAll.RemoteServer.Launcher
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
            Process process = new Process();
            process.StartInfo.FileName = "mono";
            if (debug)
                process.StartInfo.Arguments = "/home/pi/iContrAll/bin/Debug/iContrAll.RemoteServer.exe /home/pi/iContrAll/ca/server.p12 allcontri";
            else
                process.StartInfo.Arguments = "/home/pi/iContrAll/bin/iContrAll.RemoteServer.exe /home/pi/iContrAll/ca/server.p12 allcontri";
            process.EnableRaisingEvents = true;
            process.Exited += LaunchIfCrashed;

            process.Start();
        }

        private static void LaunchIfCrashed(object o, EventArgs e)
        {
            Process process = (Process)o;
            Log.WriteLine("!!!!Process crashed: {0}!!!", process.StartInfo.FileName);
            Log.WriteLine(process.ExitCode);
            if (process.ExitCode != 0)
            {
                Launch();
            }
            else
            {
                Environment.Exit(0);
            }
        }
    }
}
