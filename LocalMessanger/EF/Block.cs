using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer.EF
{
    public class Block
    {
        public int Id { get; set; }
        public int BlockerId { get; set; }
        public virtual User Blocker { get; set; } = null!;  
        public int BlockedId { get; set; }
        public virtual User Blocked { get; set; } = null!;  
        public DateTime CreatedAt { get; set; }
    }
}
