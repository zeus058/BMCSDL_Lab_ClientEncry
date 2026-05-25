namespace StudentManager.Models
{
    public class BangDiem
    {
        public string MASV { get; set; } = "";
        public string MAHP { get; set; } = "";
        public byte[]? DIEMTHI { get; set; }
        public string? DIEMTHI_HEX { get; set; }
        public string? DIEMTHI_DECRYPTED { get; set; }
    }
}
