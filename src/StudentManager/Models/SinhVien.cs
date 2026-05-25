using System;

namespace StudentManager.Models
{
    public class SinhVien
    {
        public string MASV { get; set; } = "";
        public string HOTEN { get; set; } = "";
        public DateTime? NGAYSINH { get; set; }
        public string DIACHI { get; set; } = "";
        public string MALOP { get; set; } = "";
        public string TENDN { get; set; } = "";

        public string DisplayName => $"{MASV} - {HOTEN}";
    }
}
