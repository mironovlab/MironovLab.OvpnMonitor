using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MironovLab.OvpnMonitor.Service.DbConnection
{
    [Table("sessions")]
    public class Session
    {
        public int Id { get; set; }
        [Column("user_id")]
        public int UserId { get; set; }
        [Column("session_id")]
        public int SessionId { get; set; }
        public DateTime Connected { get; set; }
        [Column("ip_address")]
        public string IPAddress { get; set; }
        public string Platform { get; set; }
        [Column("bytes_in")]
        public long BytesIn { get; set; }
        [Column("bytes_out")]
        public long BytesOut { get; set; }
        [Column("last_updated")]
        public DateTime LastUpdated { get; set; }
    }
}
