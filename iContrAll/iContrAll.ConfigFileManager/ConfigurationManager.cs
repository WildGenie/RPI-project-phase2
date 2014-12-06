using LogHelper;
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
        static string configFile = @"/home/pi/iContrAll/bin/iContrAll.config";
        //static string configFile = "iContrAll.config";
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
                                    bool loginid = false;
                                    while(reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "key":
                                                string val = reader.Value;
                                                if (val == "loginid") loginid = true; else loginid = false;
                                                break;
                                            case "value":
                                                if (loginid)
                                                {
                                                    string val1 = reader.Value;
                                                    return reader.Value;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return "";
                }
                catch(Exception)
                {
                    return "";
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
                                    bool password = false;
                                    while (reader.MoveToNextAttribute())
                                    {
                                        switch (reader.Name)
                                        {
                                            case "key":
                                                string val = reader.Value;
                                                if (val == "password") password = true; else password = false;
                                                break;
                                            case "value":
                                                if (password)
                                                {
                                                    string val1 = reader.Value;
                                                    return reader.Value;
                                                }
                                                break;
                                            default:
                                                break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    return "";
                }
                catch (Exception)
                {
                    return "";
                }
            }
            set
            {
                try
                {
                    XmlDocument doc = new XmlDocument();
                    doc.Load(configFile);
                    XmlNode root = doc.DocumentElement;
                    XmlNodeList myNodes = doc.GetElementsByTagName("add");
                    foreach (XmlNode node in myNodes)
                    {
                        XmlAttribute valueAttribute = null;
                        bool isPassword = false;
                        foreach (XmlAttribute attr in node.Attributes)
                        {
                            if (attr.Name == "key" && attr.Value == "password")
                            {
                                isPassword = true;
                            }
                            if (attr.Name == "value")
                            {
                                valueAttribute = attr;
                            }
                        }

                        if (isPassword)
                        {
                            valueAttribute.Value = value;
                            break;
                        }
                    }
                    doc.Save(configFile);
                }
                catch (Exception e)
                {
                    Log.WriteLine(e);
                }
            }
        }
    }
}
