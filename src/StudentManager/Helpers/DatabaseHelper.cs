using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using StudentManager.ViewModels;

namespace StudentManager.Helpers
{
    public static class DatabaseHelper
    {
        private static string? _connectionString;

        public static string ConnectionString =>
            _connectionString ??= ConfigurationManager.ConnectionStrings["QLSVNhom"]?.ConnectionString
                ?? "Data Source=ZEUS;Initial Catalog=QLSVNhom;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;";

        public static IDbConnection GetConnection() => new SqlConnection(ConnectionString);

        public static bool TestConnection()
        {
            try
            {
                using var conn = GetConnection();
                conn.Open();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void LogQuery(string query, object? param = null)
        {
            MonitorViewModel.AddLog(query, param);
        }
    }
}
