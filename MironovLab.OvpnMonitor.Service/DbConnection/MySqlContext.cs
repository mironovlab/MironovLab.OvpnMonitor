using System.Data.Entity;
using MySql.Data.EntityFramework;

namespace MironovLab.OvpnMonitor.Service.DbConnection
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class MySqlContext : DbContext
    {
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Session> Sessions { get; set; }
        public virtual DbSet<IntermediateData> IntermediateDatas { get; set; }

        public MySqlContext(string connectionString) : base(connectionString)
        {
        }
    }
}
