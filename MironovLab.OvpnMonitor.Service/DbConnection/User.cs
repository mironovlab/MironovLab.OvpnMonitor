using System.ComponentModel.DataAnnotations.Schema;

namespace MironovLab.OvpnMonitor.Service.DbConnection
{
    [Table("users")]
    public class User
    {
        public int Id { get; set; }
        [Column("common_name")]
        public string CommonName { get; set; }
        [Column("ip_address")]
        public string IPAddress { get; set; }
    }
}
