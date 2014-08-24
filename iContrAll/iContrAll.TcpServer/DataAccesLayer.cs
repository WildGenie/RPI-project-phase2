using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iContrAll.TcpServer
{
    #region helperclasses
    class DevicesInPlaces
    {
        public Guid PlaceId { get; set; }
        public string DeviceId { get; set; }
        public int DeviceChannel { get; set; }
    }
    #endregion
    class DataAccesLayer: IDisposable
    {
        private MySqlConnection mysqlConn;

        public DataAccesLayer()
        {
            try
            {
                var connectionString = ConfigurationManager.AppSettings["mysqlConnectionString"].ToString();
                
                mysqlConn = new MySqlConnection(connectionString);
                mysqlConn.Open();
            }
            catch (Exception ex)
            {
                // TODO: normális hibakezelő modult írni!
                Console.WriteLine(ex.Message.ToString());
            }
        }

        #region Queries
        public IEnumerable<Device> GetDeviceList()
        {
            // TODO:
            //      MINDEN RESULT-ot ELLENŐRIZNI!!! (típus, try-catch)
            List<Device> deviceList = new List<Device>();

            try
            {
                using (var cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM Devices";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // mandatory properties
                            Device d = new Device
                            {
                                Id = reader["Id"].ToString(),
                                Channel = int.Parse(reader["Channel"].ToString()),
                                Name = reader["Name"].ToString(),
                                Actions = new List<ActionType>(),
                                Voltage = 0,
                                DeviceType = ""
                            };
                            // optional properties
                            d.Timer = reader["Timer"].ToString();
                            int tempVoltage = 0;
                            if (int.TryParse(reader["Voltage"].ToString(), out tempVoltage)) d.Voltage = tempVoltage;
                            object devType = reader["DeviceType"];

                            d.DeviceType = devType != null ? d.DeviceType = devType.ToString() : "";

                            deviceList.Add(d);
                        }
                    }

                    cmd.CommandText = "SELECT * FROM ActionTypes";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            ActionType actionType = new ActionType()
                            {
                                Id = int.Parse(reader["Id"].ToString()),
                                DeviceType = reader["DeviceType"].ToString(),
                                Name = reader["Name"].ToString()
                            };

                            foreach (var device in deviceList)
                            {
                                if (device.DeviceType == actionType.DeviceType)
                                {
                                    device.Actions.Add(actionType);
                                }

                            }
                        }
                    }
                }
            }
            catch(Exception)
            {
                Console.WriteLine("Mysql exception");
            }

            return deviceList;
        }

        // TODO: készülj hibás bemenetre a 'voltage' esetén a függvény hívásakor (pl. string...)
        public void AddDevice(string id, int channel, string name, string timer, int voltage)
        {
            try
            {
                // Nem professzionális, kéne valami típus paraméter, de így is jó.
                bool usableType = false;
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT Id FROM DeviceTypes";
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Nem professzionális, kéne valami típus paraméter, de így is jó.
                            if (reader["Id"].ToString() == id.Substring(0, 3))
                            {
                                usableType = true;
                            }
                        }
                    }
                }

                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    var count = GetDeviceList().Count(d => d.Id == id && d.Channel == channel);
                    if (count > 0)
                    {
                        // UPDATE
                        cmd.CommandText = "UPDATE Devices SET Name = @Name, Timer = @Timer, Voltage=@Voltage WHERE Id = @Id AND Channel = @Channel";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Channel", channel);
                        cmd.Parameters.AddWithValue("@Name", name);
                        cmd.Parameters.AddWithValue("@Timer", timer);
                        cmd.Parameters.AddWithValue("@Voltage", voltage);
                    }
                    else
                    {
                        // INSERT
                        cmd.CommandText = "INSERT INTO Devices(Id,Channel,Name,DeviceType) VALUES(@Id, @Channel, @Name, @DeviceType)";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Channel", channel);
                        cmd.Parameters.AddWithValue("@Name", name);
                        if (usableType)
                            cmd.Parameters.AddWithValue("@DeviceType", id.Substring(0, 3));
                        else cmd.Parameters.AddWithValue("@DeviceType", null);
                        cmd.Parameters.AddWithValue("@Timer", timer);
                        cmd.Parameters.AddWithValue("@Voltage", voltage);
                    }

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void DelDevice(string id, int channel)
        {
            try
            {
                using (var cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Devices " +
                                             "WHERE Id = @Id AND Channel = @Channel";
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@Channel", channel);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public IEnumerable<Place> GetPlaceList()
        {
            List<Place> returnList = new List<Place>();
            try
            {
                using (var cmd = mysqlConn.CreateCommand())
                {
                    //cmd.CommandText = "SELECT DevicesInPlaces.DeviceId, DevicesInPlaces.DeviceChannel, DevicesInPlaces.PlaceId, Places.Name" +
                    //                 " FROM DevicesInPlaces JOIN Places ON Places.Id=DevicesInPlaces.PlaceId";

                    cmd.CommandText = "select * from DevicesInPlaces";
                    List<DevicesInPlaces> devsInPlaces = new List<DevicesInPlaces>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            devsInPlaces.Add(new DevicesInPlaces
                            {
                                DeviceId = reader["DeviceId"].ToString(),
                                DeviceChannel = int.Parse(reader["DeviceChannel"].ToString()),
                                PlaceId = new Guid(reader["PlaceId"].ToString())
                            });
                        }
                    }

                    cmd.CommandText = "select * from Places";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Place p = new Place { Id = new Guid(reader["Id"].ToString()), Name = reader["Name"].ToString() };
                            var devsOfPlace = from d in devsInPlaces
                                              where d.PlaceId == p.Id
                                              select new Device { Id = d.DeviceId, Channel = d.DeviceChannel };

                            p.DevicesInPlace = devsOfPlace.ToList();
                            returnList.Add(p);
                        }
                    }
                }
            }

            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }


            return returnList;
        }
        
        public void AddPlace(string id, string name)
        {
            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    var count = GetPlaceList().Count(d => d.Id == new Guid(id));
                    if (count > 0)
                    {
                        // UPDATE
                        cmd.CommandText = "UPDATE Places SET Name = @Name WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", name);
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO Places(Id,Name) VALUES(@Id, @Name)";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", name);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void DelPlace(string id)
        {
            try
            {
                using (var cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM Places WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void AddDeviceToPlace(string deviceId, int channel, Guid placeId)
        {
            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM DevicesInPlaces";
                    //cmd.ExecuteNonQuery(); // ????

                    List<DevicesInPlaces> devsInPlaces = new List<DevicesInPlaces>();
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            devsInPlaces.Add(new DevicesInPlaces
                            {
                                DeviceId = reader["DeviceId"].ToString(),
                                DeviceChannel = int.Parse(reader["DeviceChannel"].ToString()),
                                PlaceId = new Guid(reader["PlaceId"].ToString())
                            });
                        }
                    }

                    if (devsInPlaces.Count(dip => dip.DeviceId == deviceId &&
                                                  dip.DeviceChannel == channel &&
                                                  dip.PlaceId == placeId) <= 0)
                    {
                        // not necessary to check, 'cause all of the 3 parameters are key attributes
                        cmd.CommandText = "INSERT INTO DevicesInPlaces(DeviceId,DeviceChannel, PlaceId) VALUES(@DeviceId, @Channel, @PlaceId)";
                        cmd.Parameters.AddWithValue("@DeviceId", deviceId.ToString());
                        cmd.Parameters.AddWithValue("@Channel", channel.ToString());
                        cmd.Parameters.AddWithValue("@PlaceId", placeId.ToString());

                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void DelDeviceFromPlace(string deviceId, int channel, Guid placeId)
        {
            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    // not necessary to check, 'cause all of the 3 parameters are key attributes
                    cmd.CommandText = "DELETE FROM DevicesInPlaces WHERE DeviceId=@DeviceId AND DeviceChannel=@Channel AND PlaceId=@PlaceId";
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId.ToString());
                    cmd.Parameters.AddWithValue("@Channel", channel.ToString());
                    cmd.Parameters.AddWithValue("@PlaceId", placeId.ToString());

                    cmd.ExecuteNonQuery();
                }
            } 
            catch(MySqlException ex)
            {
                Console.WriteLine(ex.StackTrace);
            }
        }

        public List<ActionList> GetActionLists()
        {
            var returnList = new List<ActionList>();

            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "SELECT * FROM ActionLists";
                    cmd.ExecuteNonQuery();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            returnList.Add(
                                new ActionList
                                {
                                    Id = new Guid(reader["Id"].ToString()),
                                    Name = reader["Name"].ToString(),
                                    Actions = new List<Action>()
                                });
                        }
                    }

                    cmd.CommandText = "SELECT Actions.DeviceId, Actions.DeviceChannel, Actions.OrderNumber, Actions.ActionListId, ActionTypes.Name "+
                                      "FROM Actions, ActionTypes "+
                                      "WHERE ActionTypes.Id = Actions.ActionTypeId";
                    cmd.ExecuteNonQuery();

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            Action a = new Action
                            {
                                DeviceId = reader["DeviceId"].ToString(),
                                Order = int.Parse(reader["OrderNumber"].ToString()),
                                ActionTypeName = reader["Name"].ToString(),

                            };
                            Guid actionListId = new Guid(reader["ActionListId"].ToString());

                            foreach (var actionList in returnList)
                            {
                                if (actionList.Id == actionListId)
                                {
                                    actionList.Actions.Add(a);
                                }
                            }
                        }
                    }
                }

                
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
            return returnList;
        }

        public void AddActionList(string id, string name)
        {
            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    var count = GetActionLists().Count(d => d.Id == new Guid(id));
                    if (count > 0)
                    {
                        // UPDATE
                        cmd.CommandText = "UPDATE ActionLists SET Name = @Name WHERE Id = @Id";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", name);
                    }
                    else
                    {
                        cmd.CommandText = "INSERT INTO ActionLists(Id,Name) VALUES(@Id, @Name)";
                        cmd.Parameters.AddWithValue("@Id", id);
                        cmd.Parameters.AddWithValue("@Name", name);
                    }
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void DelActionList(string id)
        {
            try
            {
                using (var cmd = mysqlConn.CreateCommand())
                {
                    cmd.CommandText = "DELETE FROM ActionLists WHERE Id = @Id";
                    cmd.Parameters.AddWithValue("@Id", id);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Mysql exception");
            }
        }

        public void AddActionToActionList(Guid actionListId, int actionType, int channel, int order, string deviceId)
        {
            try
            {
                using (MySqlCommand cmd = mysqlConn.CreateCommand())
                {
                    ActionList actionList = GetActionLists().First(l => l.Id == actionListId);

                    if (actionList.Actions.Count(a => a.Order == order) > 0)
                    {
                        Console.WriteLine("Tried to add an action with an existing order number again");
                        cmd.Dispose();
                        return;
                    }

                    // not necessary to check, 'cause all of the 3 parameters are key attributes
                    cmd.CommandText = "INSERT INTO Actions(ActionTypeId, DeviceId, ActionListId, OrderNumber) VALUES(@ActionTypeId, @DeviceId, @ActionListId, @Order)";
                    cmd.Parameters.AddWithValue("@ActionTypeId", actionType);
                    cmd.Parameters.AddWithValue("@DeviceId", deviceId.ToString());
                    cmd.Parameters.AddWithValue("@ActionListId", actionListId.ToString());
                    cmd.Parameters.AddWithValue("@Order", order);



                    cmd.ExecuteNonQuery();
                }


            }
            catch (Exception)
            {
                Console.WriteLine("AddActionToList Exception");
            }
        }

        public void DelActionFromActionList(Guid actionListId, int actionType /*actionTypes.Name*/, int order, string deviceId)
        {
            try
            {
            using (MySqlCommand cmd = mysqlConn.CreateCommand())
            {
                // not necessary to check, 'cause all of the 3 parameters are key attributes
                cmd.CommandText = "DELETE FROM Actions WHERE ActionTypeId=@ActionTypeId AND DeviceId=@DeviceId AND ActionListId=@ActionListId AND OrderNumber=@Order";
                cmd.Parameters.AddWithValue("@ActionTypeId", actionType);
                cmd.Parameters.AddWithValue("@DeviceId", deviceId.ToString());
                cmd.Parameters.AddWithValue("@ActionListId", actionListId.ToString());
                cmd.Parameters.AddWithValue("@Order", order);

                cmd.ExecuteNonQuery();

            }
            }
            catch (Exception)
            {
                Console.WriteLine("AddActionToList Exception");
            }
        }

        public void UpdateDeviceStatus(string deviceId, int deviceChannel, bool state, int value, int power)
        {
            using (MySqlCommand cmd = mysqlConn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO Statuses (DeviceId, DeviceChannel, State, Value, Power) " +
                                  "VALUES (@DeviceId, @DeviceChannel, @State, @Value, @Power) " +
                                  "ON DUPLICATE KEY UPDATE State = VALUES(State), Value = VALUES(Value), Power = VALUES(Power)";
                
                cmd.Parameters.AddWithValue("@DeviceId", deviceId);
                cmd.Parameters.AddWithValue("@DeviceChannel", deviceChannel);
                cmd.Parameters.AddWithValue("@State", state?1:0);
                cmd.Parameters.AddWithValue("@Value", value);
                cmd.Parameters.AddWithValue("@Power", power);

                cmd.ExecuteNonQuery();
            }
        }

        public IEnumerable<Status> GetDeviceStatus(string deviceId)
        {
            List<Status> statusList = new List<Status>();

            using (MySqlCommand cmd = mysqlConn.CreateCommand())
            {
                cmd.CommandText = "SELECT * FROM Statuses WHERE DeviceId = @DeviceId";

                cmd.Parameters.AddWithValue("@DeviceId", deviceId);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        statusList.Add(new Status
                        {
                            DeviceId = reader["DeviceId"].ToString(),
                            DeviceChannel = int.Parse(reader["DeviceChannel"].ToString()),
                            State = int.Parse(reader["State"].ToString()) == 1 ? true : false,
                            Value = int.Parse(reader["Value"].ToString()),
                            Power = int.Parse(reader["Power"].ToString())
                        });
                        
                    }
                }
            }

            return statusList;
        }

        #endregion

        #region IDisposable members
        public void Dispose()
        {
            mysqlConn.Close();
        }

        #endregion
    }
}
