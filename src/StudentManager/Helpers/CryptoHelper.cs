using System;
using System.Security.Cryptography;
using System.Text;

namespace StudentManager.Helpers
{
    public static class CryptoHelper
    {
        /// <summary>
        /// Sinh cặp khóa RSA-2048 xác định từ mật khẩu và mã nhân viên.
        /// Cùng (password, manv) luôn sinh cùng cặp khóa.
        /// </summary>
        public static (string PublicKeyXml, string PrivateKeyXml) GenerateDeterministicKeyPair(string password, string manv)
        {
            return DeterministicRsa.Generate(password, manv);
        }

        /// <summary>Mã hóa chuỗi plaintext bằng RSA Public Key (XML).</summary>
        public static byte[] EncryptRSA(string plainText, string publicKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKeyXml);
            var data = Encoding.UTF8.GetBytes(plainText);
            return rsa.Encrypt(data, false);
        }

        /// <summary>Giải mã dữ liệu bằng RSA Private Key (XML).</summary>
        public static string DecryptRSA(byte[] cipherText, string privateKeyXml)
        {
            using var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKeyXml);
            var decryptedData = rsa.Decrypt(cipherText, false);
            return Encoding.UTF8.GetString(decryptedData);
        }

        /// <summary>Băm SHA-1 → byte[] (dùng cho mật khẩu).</summary>
        public static byte[] Sha1(string input)
        {
            using var sha1 = SHA1.Create();
            return sha1.ComputeHash(Encoding.UTF8.GetBytes(input));
        }

        /// <summary>Băm SHA-1 → chuỗi hex.</summary>
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
