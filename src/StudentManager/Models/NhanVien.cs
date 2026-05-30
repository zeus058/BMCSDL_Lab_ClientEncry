namespace StudentManager.Models
{
    public class NhanVien
    {
        public string MANV { get; set; } = "";
        public string HOTEN { get; set; } = "";
        public string? EMAIL { get; set; }
        public string? TENDN { get; set; }
        public string? PUBKEY { get; set; }
        public byte[]? LUONG { get; set; }
    }
}
