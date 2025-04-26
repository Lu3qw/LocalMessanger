using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace LocalMessangerServer.EF
{


    public enum UserStatus { Online, Offline, DoNotDisturb, Away }
    public class User
    {
        public int Id { get; set; }
        [Required]
        public string Username { get; set; }

        [Required]
        [MaxLength(72)]
        public string PasswordHash { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Message> SentMessages { get; set; }
        public virtual ICollection<Message> ReceivedMessages { get; set; }
        public virtual ICollection<UserLog> Logs { get; set; }
        public UserStatus Status { get; set; }
        public virtual ICollection<Block> BlocksInitiated { get; set; } = new List<Block>();
        public virtual ICollection<Block> BlocksReceived { get; set; } = new List<Block>();
    }
}

   
