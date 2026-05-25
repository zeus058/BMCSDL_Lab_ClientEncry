namespace StudentManager.Models
{
    public class HocPhan
    {
        public string MAHP { get; set; } = "";
        public string TENHP { get; set; } = "";
        public int? SOTC { get; set; }

        public string DisplayName => $"{MAHP} - {TENHP}";
    }
}
