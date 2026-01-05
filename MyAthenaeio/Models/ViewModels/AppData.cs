namespace MyAthenaeio.Models.ViewModels
{
    public class AppData
    {
        public int ScanCount { get; set; }
        public List<ScanLogEntry> ScanLog { get; set; } = new();
    }
}
