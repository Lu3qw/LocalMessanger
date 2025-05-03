using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalMessangerClient.ViewModels
{
    public class MessageViewModel
    {
        public string Content { get; set; }
        public string Time { get; set; }
        public bool IsMe { get; set; }
    }
}
