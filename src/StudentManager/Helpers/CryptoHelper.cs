using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace StudentManager.Helpers
{
    public static class CryptoHelper
    {
        private static readonly string KeyDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Keys");

        static CryptoHelper()
        {
            if (!Directory.Exists(KeyDirectory))
                Directory.CreateDirectory(KeyDirectory);
        }

        public static string GetPublicKeyPath(string manv) => Path.Combine(KeyDirectory, $"{manv}_public.xml");

        public static string GetPrivateKeyPath(string manv) => Path.Combine(KeyDirectory, $"{manv}_private.xml");

        public static bool HasLocalKeyPair(string manv) =>
            File.Exists(GetPublicKeyPath(manv)) && File.Exists(GetPrivateKeyPath(manv));

        public static (string PublicKeyXml, string PrivateKeyXml) GenerateRSAKeyPair(int keySize = 512)
        {
            using var rsa = new RSACryptoServiceProvider(keySize);
            return (rsa.ToXmlString(false), rsa.ToXmlString(true));
        }

        public static void SavePrivateKeyLocal(string manv, string privateKeyXml)
        {
            File.WriteAllText(GetPrivateKeyPath(manv), privateKeyXml);
        }

        public static void SavePublicKeyLocal(string manv, string publicKeyXml)
        {
            File.WriteAllText(GetPublicKeyPath(manv), publicKeyXml);
        }

        public static string? LoadPrivateKeyLocal(string manv)
        {
            var path = GetPrivateKeyPath(manv);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static string? LoadPublicKeyLocal(string manv)
        {
            var path = GetPublicKeyPath(manv);
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }

        public static byte[] EncryptRSA(string plainText, string publicKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXml);
            var data = Encoding.UTF8.GetBytes(plainText);
            return rsa.Encrypt(data, false);
        }

        public static string DecryptRSA(byte[] cipherText, string privateKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
            var decryptedData = rsa.Decrypt(cipherText, false);
            return Encoding.UTF8.GetString(decryptedData);
        }

        public static byte[] Sha1(string input)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        public static string Sha1Hex(string input)
        {
            using var sha1 = SHA1.Create();
            var hash = sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
            var builder = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}
