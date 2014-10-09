using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoUpdater
{
    public class ConnectionManager : IDisposable
    {
        private readonly SshClient sshClient;
        public readonly MySqlConnection Connection;
        private readonly ForwardedPortLocal port;
        private readonly string password;

        public ConnectionManager(string remoteHost, string sshUser, string sshPwd)
        {
            this.password = sshPwd;

            sshClient = new SshClient(remoteHost, sshUser, sshPwd);

            sshClient.Connect();

            // port forwardig
            port = new ForwardedPortLocal("localhost", 3306, "localhost", 3306);
            sshClient.AddForwardedPort(port);
            port.Start();

            // MySql Connection
            Connection = new MySqlConnection("Server=localhost;Port=3306;Database=redmine;Uid=redmine;Pwd=redmine123;");

            Connection.Open();
        }

        public void Dispose()
        {
            port.Stop();
            Connection.Close();
            Connection.Dispose();
            sshClient.Dispose();
        }

        public ObservableCollection<StatusColorData> DownloadFileFromServer(string remoteFolder, string remoteFileName)
        {
            var localFileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "application.css");

            using (var sftp = new SftpClient(sshClient.ConnectionInfo.Host, sshClient.ConnectionInfo.Username, password))
            {
                sftp.Connect();
                sftp.ChangeDirectory(remoteFolder);

                using (var file = File.OpenWrite(localFileName))
                {
                    sftp.DownloadFile(remoteFileName, file);
                }

                sftp.Disconnect();
            }

            return FileHandling.ReadFromFile(localFileName);
        }

        public ObservableCollection<StatusColorData> GetStatusesFromDatabase()
        {
            ObservableCollection<StatusColorData> StatusList = new ObservableCollection<StatusColorData>();

            MySqlCommand command = Connection.CreateCommand();
            command.CommandText = "SELECT id, name FROM redmine.issue_statuses";
            int id;
            string text;
            var reader = command.ExecuteReader();
            while (reader.Read())
            {
                id = reader.GetInt32("id");
                text = reader.GetString("name");
                bool found = false;
                for (int i = 0; i < StatusList.Count; i++)
                {
                    if (StatusList[i].StatusId == id)
                    {
                        StatusList[i].StatusName = text;
                        found = true;
                    }
                }
                if (!found) StatusList.Add(new StatusColorData(id, text));
            }
            reader.Close();

            return StatusList;
        }

        public void UploadColorsToDatabase(ObservableCollection<StatusColorData> statusList, string remoteFolder, string remoteFileName)
        {
            FileHandling.SaveToFile(false, statusList);
            var localFileName = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), "application.css");

            using (var sftp = new SftpClient(sshClient.ConnectionInfo.Host, sshClient.ConnectionInfo.Username, password))
            {
                sftp.Connect();
                sftp.ChangeDirectory(remoteFolder);

                using (var file = File.OpenRead(localFileName))
                {
                    sftp.UploadFile(file, remoteFileName, true);
                }

                sftp.Disconnect();
            }
        }
}
