using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerServer.EF
{
    public class BannedUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public DateTime BanDate { get; set; }
    }
}
