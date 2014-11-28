using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AutoUpdater
{
    class Program
    {
        class UpdateAccess {
            public string address = string.Empty;
            public string user = string.Empty;
            public string password = string.Empty;
            public AccessType type;
        }

        static List<UpdateAccess> accessList = new List<UpdateAccess>();
        enum AccessType { StorageProvider, SFTP };

        static Timer timer;

        static void Main(string[] args)
        {
            timer = new Timer(TimerElapsed, null, 5000, 60000);

            Console.ReadKey();
        }

        static void TimerElapsed(object state)
        {
            UpdateAutoUpdaterConfig();
            ReadConfig();

            UpdateAccess selectedSource = UpdateNeeded();

            if (selectedSource != null)
            {
                Console.WriteLine("Update needed");
                Update(selectedSource);
            }
            else
            {
                Console.WriteLine("Don't need for update");
            }
            UpdateAutoUpdaterConfig();
        }

        static string tempVersionFilePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "tempVersion.txt");
        static string tempPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        
        private static void Update(UpdateAccess selectedSource)
        {
            try
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = (p1, p2, p3, p4) => true;
                client.DownloadFile(selectedSource.address, tempVersionFilePath);
                string versionNumber = string.Empty;
                string zipAddress = string.Empty;
                using (var f = File.OpenText(tempVersionFilePath))
                {
                    string line = f.ReadLine();
                    versionNumber = line.Split(';')[0];
                    zipAddress = line.Split(';')[1];
                }
                ServicePointManager.ServerCertificateValidationCallback = (p1, p2, p3, p4) => true;
                string zipPath = Path.Combine(tempPath, versionNumber+".zip");
                Console.WriteLine("Downloading zip {0} from {1}", zipPath, zipAddress);
                
                client.DownloadFile(zipAddress, zipPath);
                Console.WriteLine("Zip downloaded");
                string directoryPath = "/home/pi/iContrAll/bin/" + versionNumber;
                Directory.CreateDirectory(directoryPath);

                using (ZipFile zip = ZipFile.Read(zipPath))
                {
                    foreach (ZipEntry e in zip)
                    {
                        e.Extract(directoryPath, ExtractExistingFileAction.OverwriteSilently);
                    }
                }

                using (var f = File.CreateText("/home/pi/iContrAll/bin/latestversion.txt"))
                {
                    f.Write(versionNumber);
                }

                Console.WriteLine("Zip extracted");

                Restart();
            }
            catch (Exception)
            {
                Console.WriteLine("Hiba a frissítés során, leszarjuk, majd legközelebb");
            }
        }

        private static void Restart()
        {
            try
            {
                Console.WriteLine("Restarting Raspberry...");
                Process process = new Process();
                process.StartInfo.FileName = "sudo";

                process.StartInfo.Arguments = "reboot";
                process.Start();
            }
            catch (Exception)
            {
                Console.WriteLine("Cannot reboot the system");
            }
        }

        private static UpdateAccess UpdateNeeded()
        {
            string oldVersion;
            if (!File.Exists("/home/pi/iContrAll/bin/latestversion.txt")) { oldVersion = string.Empty; }
            else
            {
                using (var f = File.OpenText("/home/pi/iContrAll/bin/latestversion.txt"))
                {
                    oldVersion = f.ReadLine();
                }
            }

            string newVersion;

            foreach (var access in accessList)
            {
                if (access.type == AccessType.StorageProvider)
                {
                    try {
                        WebClient client = new WebClient();
                        ServicePointManager.ServerCertificateValidationCallback = (p1, p2, p3, p4) => true;
                        
                        client.DownloadFile(access.address, tempVersionFilePath);

                        using(var f = File.OpenText(tempVersionFilePath))
                        {
                            newVersion = f.ReadLine();
                            if (newVersion.Split(';')[0] == oldVersion)
                            {
                                return null;
                            }
                            else
                            {
                                int oldVersionNumber;
                                bool oldExists = int.TryParse(oldVersion, out oldVersionNumber);
                                int newVersionNumber;
                                int.TryParse(newVersion.Split(';')[0], out newVersionNumber);

                                if (!oldExists || newVersionNumber>oldVersionNumber)
                                {
                                    return access;
                                }
                            }
                        }
                    }
                    catch(Exception)
                    {
                        continue;
                    }
                }
            }

            return null;
        }

        static void UpdateAutoUpdaterConfig()
        {
            try
            {
                WebClient client = new WebClient();
                ServicePointManager.ServerCertificateValidationCallback = (p1, p2, p3, p4) => true;

                var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "AutoUpdaterConfig.xml");

                XDocument configXml = XDocument.Load(path);
                var config = from cfg in configXml.Descendants("AutoUpdaterConfiguration")
                             select cfg.Element("ConfigAddress");

                try
                {
                    client.DownloadFile(config.First().Value, System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "AutoUpdaterConfig.xml"));
                }
                catch(Exception)
                {
                    Console.WriteLine("Hajjaj");
                    client.DownloadFile("https://www.dropbox.com/s/d2ptd6vknafimqx/AutoUpdaterConfig.xml?dl=1", System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "AutoUpdaterConfig.xml"));
                }
            }
            catch(Exception)
            {
                Console.WriteLine("Updating the config file failed.");
            }
        }

        private static void ReadConfig()
        {
            var path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "AutoUpdaterConfig.xml");
            accessList = new List<UpdateAccess>();

            try
            {
                XDocument configXml = XDocument.Load(path);
                var config = from cfg in configXml.Descendants("AutoUpdaterConfiguration")
                             select cfg.Element("Address");

                UpdateAccess access = new UpdateAccess();
                
                foreach (var a in config)
                {
                    if (a.HasAttributes)
                    {
                        if(a.Attribute("type").Value == "SFTP")
                        {
                            access.type = AccessType.SFTP;
                            access.address = a.Value;
                            access.user = a.Attribute("user").Value;
                            access.password = a.Attribute("password").Value;
                        }
                    }
                    else
                    {
                        access.type = AccessType.StorageProvider;
                        access.address = a.Value;
                    }

                    accessList.Add(access);
                }
                
            }
            catch (Exception)
            {
                if (!accessList.Any())
                {
                    UpdateAccess access = new UpdateAccess();
                    access.type = AccessType.StorageProvider;
                    access.address = "https://www.dropbox.com/s/exvmvp38si8kwu2/latestversion.txt?dl=1";
                    accessList.Add(access);
                }
            }
        }
    }
}
