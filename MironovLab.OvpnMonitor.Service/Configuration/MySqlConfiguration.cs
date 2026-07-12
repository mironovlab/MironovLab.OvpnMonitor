using MySql.Data.MySqlClient;

namespace MironovLab.OvpnMonitor.Service.Configuration
{
    public class MySqlConfiguration
    {
        public string Server { get; set; }
        public uint Port { get; set; }
        public string User { get; set; }
        public string Password { get; set; }
        public string DataBase { get; set; }

        public override string ToString()
        {
            var builder = new MySqlConnectionStringBuilder
            {
                Server = Server,
                Port = Port,
                UserID = User,
                Password = Password,
                Database = DataBase,
            };

            return builder.ToString();
        }
    }
}
