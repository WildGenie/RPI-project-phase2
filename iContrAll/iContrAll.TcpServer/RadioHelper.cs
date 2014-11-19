using iContrAll.SPIRadio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    public static class RadioHelper
    {
        // Phone -> Radio
        public static void SendCommandOnRadio(string message)
        {
            try
            {
                // raspberry azonosító
                string raspberryId = System.Configuration.ConfigurationManager.AppSettings["loginid"];
                string senderIdInMsg = message.Substring(0, 8);
                // ha nem a mi eszközünk, eldobjuk
                if (raspberryId.Substring(2) != senderIdInMsg)
                {
                    Console.WriteLine("Sender id is not equal: {0} != {1}", raspberryId.Substring(2), senderIdInMsg);
                    return;
                }
                // cél eszköz
                string targetIdInMsg = message.Substring(8, 8);

                // az üzenet 19.bájtjától kezdődik a lényeg
                string command = message.Substring(18);

                #region Lámpa üzenet megformálása
                // lámpa
                if (targetIdInMsg.StartsWith("LC1"))
                {
                    string channelControl = "";

                    // Állapotlekérés
                    if (command == string.Empty)
                    {
                        if (!Radio.Instance.SendMessage(Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + "xxxx" + "xxxx"+"xxxx")))
                            Console.WriteLine("Failed to send message on radio");
                        return;
                    }

                    int channelId;
                    int eqPos = command.IndexOf('=');
                    Console.WriteLine(command);
                    
                    // pl. ch1=1
                    if (eqPos == 3)
                    {
                        int.TryParse(command[2].ToString(), out channelId);
                        char value = command[4];

                        for (int i = 1; i <= 4; i++)
                        {
                            if (i == channelId)
                            {
                                channelControl += value;
                            }
                            else channelControl += 'x';
                        }

                        byte[] dimValues = new byte[4] { 100, 100, 100, 100 };

                        using (var dal = new DataAccesLayer())
                        {
                            foreach (var s in dal.GetDeviceStatus(targetIdInMsg))
                            {
                                dimValues[s.DeviceChannel] = (byte)s.Value;
                            }
                        }

                        // byte[] retBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl + "xxxx" + "xxxx");
                        byte[] basicBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl + "xxxx");
                        byte[] retBytes = new byte[basicBytes.Length + 4];

                        Array.Copy(basicBytes, retBytes, basicBytes.Length);
                        Array.Copy(dimValues, 0, retBytes, basicBytes.Length, 4);

                        //byte[] retBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl + "xxxx");
                        if (!Radio.Instance.SendMessage(retBytes))
                            Console.WriteLine("Failed to send message on radio");
                    }
                    else
                        //if (targetIdInMsg.StartsWith("LC1"))
                        //{
                        if (eqPos == 4)
                        {
                            // dimmm érték állítása
                            if (command[2] == 'd')
                            {
                                int.TryParse(command[3].ToString(), out channelId);
                                #region normal mukodes
                                string dim = command.Substring(eqPos + 1);

                                int iOfDot = dim.IndexOf('.');

                                int dimValue;

                                int.TryParse(dim.Substring(0, iOfDot), out dimValue);

                                // Console.WriteLine("DIM üzenet: " + dim + "("+dim.Substring(0,iOfPoint)+")"+ "=> " + dimValue + " on channel " + channelId);

                                byte[] dimValues = new byte[] { 100, 100, 100, 100 };
                                using (var dal = new DataAccesLayer())
                                {
                                    foreach(var s in dal.GetDeviceStatus(targetIdInMsg))
                                    {
                                        dimValues[s.DeviceChannel] = (byte)s.Value;
                                    }
                                    
                                    for (int i = 1; i <= 4; i++)
                                    {
                                        if (i == channelId)
                                        {
                                            dimValues[i - 1] = (byte)dimValue;
                                            // megtárgyalni a dimValue == 0 esetét. Kikapcsolás, dimÉrték hova legyen állítva?
                                            channelControl += 1;
                                        }
                                        else
                                        {
                                            channelControl += 'x';
                                        }
                                    }
                                }

                                //string dimString = Encoding.UTF8.GetString(dimValues);
                                string basicString = senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl;
                                
                                // Egyszer majd...
                                basicString += "xxxx";

                                byte[] basicBytes = Encoding.UTF8.GetBytes(basicString);

                                byte[] retBytes = new byte[basicBytes.Length + 4];

                                Array.Copy(basicBytes, retBytes, basicBytes.Length);
                                Array.Copy(dimValues, 0, retBytes, basicBytes.Length, 4);

                                //for (int i = 0; i < retBytes.Length; i++)
                                //{
                                //    Console.Write(retBytes[i]);
                                //}
                                //Console.WriteLine();

                                if (!Radio.Instance.SendMessage(retBytes))
                                    Console.WriteLine("Failed to send message on radio");

                                #endregion

                                #region fényorgona
                                //byte[] basicBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + "xxxx" + "xxxx");
                                //byte[] retBytes = new byte[basicBytes.Length + 4];
                                //Array.Copy(basicBytes, retBytes, basicBytes.Length);

                                //byte[] dimValues = new byte[4] { 255, 255, 255, 255 };


                                //dimValues[channelId] = (byte)100;
                                //Array.Copy(dimValues, basicBytes.Length, retBytes, 0, 4);
                                //Radio.Instance.SendMessage(retBytes);
                                //for (int i = 19; i >= 0 ; i--)
                                //{
                                //    Thread.Sleep(250);
                                //    dimValues[channelId] = (byte)(i*5);
                                //    Array.Copy(dimValues, basicBytes.Length, retBytes, 0, 4);
                                //    Radio.Instance.SendMessage(retBytes);

                                //}
                                //for (int i = 0; i <= 20; i++)
                                //{
                                //    Thread.Sleep(250);
                                //    dimValues[channelId] = (byte)(i * 5);
                                //    Array.Copy(dimValues, basicBytes.Length, retBytes, 0, 4);
                                //    Radio.Instance.SendMessage(retBytes);

                                //}

                                //return;
                                #endregion
                            }
                            if (command[2] == 't')
                            {
                                int.TryParse(command[3].ToString(), out channelId);
                                string value = command.Substring(eqPos + 1);

                                if (value.Length == 8)
                                {
                                    int n;
                                    bool isNumeric = int.TryParse(value, out n);
                                    if (isNumeric)
                                    {
                                        using (var dal = new DataAccesLayer())
                                        {
                                            dal.AddTimer(new TimerData
                                            {
                                                DeviceId = targetIdInMsg,
                                                DeviceChannel = channelId,
                                                StartTime = value.Substring(0, 4),
                                                EndTime = value.Substring(4, 4)
                                            });
                                        }
                                    }
                                }
                                if (value.Length == 5)
                                {
                                    int n;
                                    bool isNumeric = int.TryParse(value.Substring(1), out n);
                                    if (isNumeric)
                                    {
                                        int hour = int.Parse(value.Substring(1, 2));
                                        int minute = int.Parse(value.Substring(3, 2));
                                        
                                        DateTime time = new RealTimeClock.RealTimeClock().GetDateTime().AddHours(hour).AddMinutes(minute);
                                        string endTime = time.ToString("HHmm");

                                        Console.WriteLine("EndTime: {0}", endTime);

                                        using (var dal = new DataAccesLayer())
                                        {
                                            dal.AddTimer(new TimerData
                                            {
                                                DeviceId = targetIdInMsg,
                                                DeviceChannel = channelId,
                                                StartTime = value[0].ToString(),
                                                EndTime = endTime
                                            });
                                        }
                                    }

                                    for (int i = 1; i <= 4; i++)
                                    {
                                        if (i == channelId)
                                        {
                                            channelControl += '1';
                                        }
                                        else channelControl += 'x';
                                    }

                                    byte[] dimValues = new byte[4] { 100, 100, 100, 100 };

                                    using (var dal = new DataAccesLayer())
                                    {
                                        foreach (var s in dal.GetDeviceStatus(targetIdInMsg))
                                        {
                                            dimValues[s.DeviceChannel] = (byte)s.Value;
                                        }
                                    }

                                    // byte[] retBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl + "xxxx" + "xxxx");
                                    byte[] basicBytes = Encoding.UTF8.GetBytes(senderIdInMsg + targetIdInMsg + "01" + "x" + channelControl + "xxxx");
                                    byte[] retBytes = new byte[basicBytes.Length + 4];

                                    Array.Copy(basicBytes, retBytes, basicBytes.Length);
                                    Array.Copy(dimValues, 0, retBytes, basicBytes.Length, 4);
                                    
                                    if (!Radio.Instance.SendMessage(retBytes))
                                        Console.WriteLine("Failed to send message on radio");
                                }
                            }
                        }
                }
                #endregion
                else
                    //redőny
                    if (targetIdInMsg.StartsWith("OC1"))
                    {
                        #region Redőny üzenet megformálása
                        // most a redőny kétcsatornás, de ezt a változót átírva automatikusan jól fog működni.
                        // érdemes lenne kivezetni konfigfájlba, vagy valahonnét lekérdezni, 
                        // hiszen lehetséges, hogy többféle eszköz is gyártásra kerül
                        int chCount = 2;

                        // Állapotlekérés
                        if (command == string.Empty)
                        {
                            StringBuilder toSend = new StringBuilder();
                            toSend.Append(senderIdInMsg + targetIdInMsg + "01" + "x");
                            for (int i = 0; i < 2; i++)
                            {
                                for (int j = 0; j < chCount; j++)
                                {
                                    toSend.Append("x");
                                }
                            }
                            if (!Radio.Instance.SendMessage(Encoding.UTF8.GetBytes(toSend.ToString())))
                                Console.WriteLine("Failed to send message on radio");
                            return;
                        }

                        int channelId = -1;
                        try
                        {
                            channelId = int.Parse(command[3].ToString());
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Exception: Cannot parse channelId in SendCommandOnRadio({0})", message);
                            Console.WriteLine(e.Message);
                            if (e.InnerException != null)
                                Console.WriteLine(e.InnerException.Message);
                            return;
                        }

                        int eqPos = command.IndexOf('=');
                        string value = command.Substring(eqPos + 1);
                        byte b = 255; // sokszor használjuk 'don't care' jelzésre

                        // A küldendő üzenet ezen része már itt ismert, csak a tartalom fog változni.
                        // |senderId| = 8byte + |targetId| = 8byte + |TAG| = 2byte
                        string retMessage = senderIdInMsg + targetIdInMsg + "01";
                        byte[] retList = Encoding.UTF8.GetBytes(retMessage);
                        byte[] retArray = new byte[retList.Length + 2 * chCount + 1];
                        Array.Copy(retList, retArray, retList.Length);
                        // RSSI bit, kifelé mindegy az érték, 255-öt (don't care) küldünk.
                        retArray[retArray.Length - (2 * chCount + 1)] = b;

                        // command[2] = ch->[SDT]<-[12]
                        switch (command[2])
                        {
                            // state: ch->S<- = 'u/d/s'
                            case 's':
                                for (int i = 0; i < chCount; i++)
                                {
                                    //channelControl += (i == channelId) ? value[0] : 'x';
                                    retArray[retArray.Length - (2 * chCount) + i] = Convert.ToByte((i + 1 == channelId) ? value[0] : 'x');
                                }

                                for (int i = 0; i < chCount; i++)
                                {
                                    retArray[retArray.Length - chCount + i] = b;
                                }

                                break;
                            // százalékos állítás, dim: ch->D<- = 0..100/255
                            case 'd':
                                int indexOfDot = value.IndexOf('.');
                                if (indexOfDot == -1) indexOfDot = value.Length;

                                int shutterState = -1;
                                try
                                {
                                    shutterState = int.Parse(value.Substring(0, indexOfDot));
                                    Console.WriteLine("ShutterState: {0}", shutterState);
                                }
                                catch (Exception e)
                                {
                                    Console.WriteLine("Exception: Cannot parse shutterState in SendCommandOnRadio({0}), Value: {2} IndexOfDot: {1}", message, indexOfDot, value);
                                    Console.WriteLine(e.Message);
                                    if (e.InnerException != null)
                                        Console.WriteLine(e.InnerException.Message);
                                    return;
                                }

                                if (shutterState < 0 || shutterState > 100)
                                {
                                    shutterState = 255;
                                }

                                byte shutterStateAsByte = (byte)shutterState;

                                // Csomagösszeállítás
                                // nincs irányítás 'u/d/s', hanem százalékos értékben állítunk
                                for (int i = 0; i < chCount; i++)
                                {
                                    retArray[retArray.Length - 2 * chCount + i] = Convert.ToByte('x');
                                }
                                // a felhasználótól kapott shutterState-et írjuk be, vagy 255-t.
                                for (int i = 0; i < chCount; i++)
                                {
                                    retArray[retArray.Length - chCount + i] = (i + 1 == channelId) ? shutterStateAsByte : b;
                                }

                                break;
                            case 't': // timer: ch->T<- = // TODO: egyezményre jutni
                                // TODO: megvalósítani, de előtte átgondolni, mi kell.
                                if (value.Length == 8)
                                {
                                    int n;
                                    bool isNumeric = int.TryParse(value, out n);

                                    if (isNumeric)
                                    {
                                        using (var dal = new DataAccesLayer())
                                        {
                                            dal.AddTimer(new TimerData
                                            {
                                                DeviceId = targetIdInMsg,
                                                DeviceChannel = channelId,
                                                StartTime = value.Substring(0, 4),
                                                EndTime = value.Substring(4, 4)
                                            });
                                        }
                                    }
                                }
                                if (value.Length == 5)
                                {
                                    int n;
                                    bool isNumeric = int.TryParse(value.Substring(1), out n);

                                    int hour = int.Parse(value.Substring(1, 2));
                                    int minute = int.Parse(value.Substring(3, 2));

                                    DateTime time = new RealTimeClock.RealTimeClock().GetDateTime().AddHours(hour).AddMinutes(minute);
                                    string endTime = time.ToString("HHmm");

                                    Console.WriteLine("EndTime: {0}", endTime);

                                    if (isNumeric)
                                    {
                                        using (var dal = new DataAccesLayer())
                                        {
                                            dal.AddTimer(new TimerData
                                            {
                                                DeviceId = targetIdInMsg,
                                                DeviceChannel = channelId,
                                                StartTime = value[0].ToString(),
                                                EndTime = value.Substring(1)
                                            });
                                        }
                                    }

                                    for (int i = 0; i < chCount; i++)
                                    {
                                        //channelControl += (i == channelId) ? value[0] : 'x';
                                        retArray[retArray.Length - (2 * chCount) + i] = Convert.ToByte((i + 1 == channelId) ? 'u' : 'x');
                                    }

                                    for (int i = 0; i < chCount; i++)
                                    {
                                        retArray[retArray.Length - chCount + i] = b;
                                    }
                                }
                                break;
                            // tanítás
                            case 'c':
                                for (int i = 0; i < chCount; i++)
                                {
                                    //channelControl += (i == channelId) ? value[0] : 'x';
                                    retArray[retArray.Length - (2 * chCount) + i] = Convert.ToByte((i + 1 == channelId) ? 'c' : 'x');
                                }

                                for (int i = 0; i < chCount; i++)
                                {
                                    retArray[retArray.Length - chCount + i] = b;
                                }

                                break;
                            default:
                                break;
                        }
                        #endregion

                        // küldés
                        if (!Radio.Instance.SendMessage(retArray))
                            Console.WriteLine("Failed to send message on radio");

                        #region Redőny kód 2014.10.16 előtt
                        //if (eqPos == 4)
                        //{
                        //    string value = command.Substring(eqPos + 1);

                        //    int indexOfPoint = command.IndexOf('.');

                        //    int shutterState;

                        //    int.TryParse(value.Substring(0, indexOfPoint-eqPos), out shutterState);

                        //    if (shutterState != 0 && shutterState != 25 && shutterState != 50 && shutterState != 75 && shutterState != 100)
                        //    {
                        //        shutterState = 255;
                        //    }

                        //    byte b = (byte)shutterState;

                        //    string retMessage = senderIdInMsg + targetIdInMsg + "01" + "x" + "x";
                        //    byte[] retList = Encoding.UTF8.GetBytes(retMessage);
                        //    byte[] retArray = new byte[retList.Length + 2];

                        //    Array.Copy(retList, retArray, retList.Length);

                        //    retArray[retArray.Length - 2] = b;
                        //    retArray[retArray.Length - 1] = Convert.ToByte('x');

                        //    Radio.Instance.SendMessage(retArray);

                        //}
                        //else
                        //if (eqPos == 6)
                        //{
                        //    string value = command[7].ToString();

                        //    int state;

                        //    int.TryParse(value, out state);
                        //    if (state != 0 && state != 1) return;

                        //    char directionChar = state == 1 ? 'u' : ((state == 0)? 'd': 'x');
                        //    byte b = 255;

                        //    string retMessage = senderIdInMsg + targetIdInMsg + "01" + "x" + directionChar;
                        //    byte[] retList = Encoding.UTF8.GetBytes(retMessage);
                        //    byte[] retArray = new byte[retList.Length + 2];

                        //    Array.Copy(retList, retArray, retList.Length);

                        //    retArray[retArray.Length - 2] = b;
                        //    retArray[retArray.Length - 1] = Convert.ToByte('x');

                        //    Radio.Instance.SendMessage(retArray);

                        //}
                        //else
                        //if (eqPos == 8)
                        //{
                        //    string s = command.Substring(9);

                        //    byte b = 255;

                        //    if (s == "stop")
                        //    {
                        //        string retMessage = senderIdInMsg + targetIdInMsg + "01" + "x" + "x";
                        //        byte[] retList = Encoding.UTF8.GetBytes(retMessage);
                        //        byte[] retArray = new byte[retList.Length + 2];

                        //        Array.Copy(retList, retArray, retList.Length);

                        //        retArray[retArray.Length - 2] = b;
                        //        retArray[retArray.Length - 1] = Convert.ToByte('s');

                        //        Radio.Instance.SendMessage(retArray);
                        //    }

                        //}
                        #endregion
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: {0}", e.Message);
                if (e.InnerException != null)
                {
                    Console.WriteLine("InnerException: {0}", e.InnerException.Message);
                }
            }
            //Console.WriteLine("Sent on radio: {0}", message);
            //Console.WriteLine("Radio osztaly letezik SendMessage után??? {0}", (Radio.Instance == null) ? "NEM" : "IGEN");
            //Console.WriteLine("Radio allapotja: " + Radio.Instance.state);
        }
    }
}
