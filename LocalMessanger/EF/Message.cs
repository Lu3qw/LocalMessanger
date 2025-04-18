using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer.EF
{
    public class Message
    {
        public int Id { get; set; }
        [Required]
        public int SenderId { get; set; }
        [Required]
        public int ReceiverId { get; set; }
        [Required]
        public string Text { get; set; }
        public DateTime SentAt { get; set; } = DateTime.UtcNow;
        public virtual User Sender { get; set; }
        public virtual User Receiver { get; set; }
    }

}
