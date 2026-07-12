using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace MironovLab.OvpnMonitor.Service.DbConnection
{
    [Table("intermediate_data")]
    public class IntermediateData
    {
        public int Id { get; set; }
        [Column("session_id")]
        public int SessionId { get; set; }
        public DateTime Date { get; set; }
        [Column("bytes_in")]
        public long BytesIn { get; set; }
        [Column("bytes_out")]
        public long BytesOut { get; set; }
    }
}
