namespace RequestBotLinux.Models
{
    public class Equipment
    {
        public int Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string OS { get; set; } = string.Empty;
        public int CabinetId { get; set; }
    }
}
