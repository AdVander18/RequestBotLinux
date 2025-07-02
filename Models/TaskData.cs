using System;

namespace RequestBotLinux.Models
{
    public class TaskData
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string MessageText { get; set; }
        public string Status { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string CabinetNumber { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime Deadline { get; set; }

    }
}
