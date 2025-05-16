using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RequestBotLinux.Models
{
    public class Cabinet
    {
        public int Id { get; set; }
        public string Number { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
