using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace iContrAll.ConfigFileManager
{
    public static class ConfigurationManager
    {
        static string configFile = "/home/pi/iContrAll/bin/iContrAll.config";
        public static string LoginId 
        { 
            get
            {
                try
                {
                    using (var reader = XmlReader.Create(configFile))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                if (reader.Name == "add")
                                {
                                    while(reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "key": if (reader.Value != "loginid") continue;
                                                break;
                                            case "value": return reader.Value;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch(Exception)
                {
                    return string.Empty;
                }
            }
        }

        public static string Password
        {
            get
            {
                try
                {
                    using (var reader = XmlReader.Create(configFile))
                    {
                        while (reader.Read())
                        {
                            if (reader.IsStartElement())
                            {
                                if (reader.Name == "add")
                                {
                                    while (reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "key": if (reader.Value != "password") continue;
                                                break;
                                            case "value": return reader.Value;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
        }
    }
}
