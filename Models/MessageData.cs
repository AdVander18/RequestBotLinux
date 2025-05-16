using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBotLinux.Models
{
    public class MessageData
    {
        public string Username { get; set; }
        public string MessageText { get; set; }
        public DateTime Timestamp { get; set; }
        public bool IsFromAdmin { get; set; }
    }
}
