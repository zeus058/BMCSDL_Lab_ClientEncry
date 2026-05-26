namespace StudentManager.Helpers
{
    public static class CurrentUser
    {
        public static string MANV { get; set; } = "";
        public static string HOTEN { get; set; } = "";
        public static string EMAIL { get; set; } = "";
        public static string TENDN { get; set; } = "";
        public static string PUBKEY { get; set; } = "";

        public static void Clear()
        {
            MANV = HOTEN = EMAIL = TENDN = PUBKEY = "";
        }
    }
}
