using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer.EF
{
    public class ServerLog
    {
        public int Id { get; set; }
        [Required]
        public string Message { get; set; }
        public string? LogLevel { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

}
