using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace StudentManager.Helpers
{
    /// <summary>
    /// Sinh cặp khóa RSA-2048 xác định (deterministic) từ mật khẩu và mã nhân viên.
    /// Cùng một (password, manv) luôn tạo ra cùng một cặp khóa RSA.
    /// Thuật toán: PBKDF2-SHA256 → AES-CTR DRBG → RSA Prime Generation.
    /// </summary>
    public static class DeterministicRsa
    {
        private const int KeySize = 2048;
        private const int HalfKeyBits = KeySize / 2;        // 1024 bits per prime
        private const int PrimeBytes = HalfKeyBits / 8;     // 128 bytes
        private const int ModulusBytes = KeySize / 8;       // 256 bytes
        private const int KdfIterations = 100_000;
        private const int MillerRabinRounds = 20;

        /// <summary>
        /// Sinh cặp khóa RSA-2048 xác định từ mật khẩu và mã nhân viên.
        /// </summary>
        public static (string PublicKeyXml, string PrivateKeyXml) Generate(string password, string manv)
        {
            // Bước 1: Derive 32-byte seed từ (password, manv) bằng PBKDF2-SHA256
            byte[] salt = Encoding.UTF8.GetBytes("QLSVNhom_DeterministicRSA_v1_" + manv);
            byte[] seed;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, KdfIterations, HashAlgorithmName.SHA256))
            {
                seed = kdf.GetBytes(32);
            }

            // Bước 2: Tạo DRBG xác định dựa trên AES-256 Counter Mode
            var drbg = new AesCtrDrbg(seed);

            // Bước 3: Sinh hai số nguyên tố 1024-bit
            var p = GeneratePrime(drbg, HalfKeyBits);
            var q = GeneratePrime(drbg, HalfKeyBits);

            // Đảm bảo p > q
            if (p < q)
            {
                var tmp = p; p = q; q = tmp;
            }

            // Bước 4: Tính toán các tham số RSA
            var n = p * q;
            var e = new BigInteger(65537);
            var phi = (p - BigInteger.One) * (q - BigInteger.One);
            var d = ModInverse(e, phi);
            var dp = d % (p - BigInteger.One);
            var dq = d % (q - BigInteger.One);
            var inverseQ = ModInverse(q, p);

            // Bước 5: Xây dựng RSAParameters và export XML
            var rsaParams = new RSAParameters
            {
                Modulus = ToFixedBigEndian(n, ModulusBytes),
                Exponent = new byte[] { 1, 0, 1 }, // 65537
                D = ToFixedBigEndian(d, ModulusBytes),
                P = ToFixedBigEndian(p, PrimeBytes),
                Q = ToFixedBigEndian(q, PrimeBytes),
                DP = ToFixedBigEndian(dp, PrimeBytes),
                DQ = ToFixedBigEndian(dq, PrimeBytes),
                InverseQ = ToFixedBigEndian(inverseQ, PrimeBytes)
            };

            using var rsa = new RSACryptoServiceProvider(KeySize);
            rsa.ImportParameters(rsaParams);

            return (rsa.ToXmlString(false), rsa.ToXmlString(true));
        }

        #region Prime Generation

        /// <summary>Sinh số nguyên tố có độ dài bitLength bits.</summary>
        private static BigInteger GeneratePrime(AesCtrDrbg drbg, int bitLength)
        {
            int byteLength = bitLength / 8;

            while (true)
            {
                byte[] bytes = drbg.GetBytes(byteLength);

                // Đặt bit cao nhất = 1 (đảm bảo đủ độ dài) và bit thấp nhất = 1 (số lẻ)
                bytes[0] |= 0x80;
                bytes[byteLength - 1] |= 0x01;

                // BigInteger dùng little-endian, byte array ta tạo ở big-endian → đảo ngược
                Array.Reverse(bytes);

                // Thêm byte 0 ở cuối để BigInteger hiểu là số dương
                var candidate = new BigInteger(AppendZero(bytes));

                if (candidate.Sign > 0 && IsProbablePrime(candidate, drbg))
                    return candidate;
            }
        }

        /// <summary>Miller-Rabin primality test xác định (dùng DRBG).</summary>
        private static bool IsProbablePrime(BigInteger n, AesCtrDrbg drbg)
        {
            if (n < 2) return false;
            if (n == 2 || n == 3) return true;
            if (n.IsEven) return false;

            // Phân tích n-1 = 2^r * d
            BigInteger d = n - 1;
            int r = 0;
            while (d.IsEven)
            {
                d >>= 1;
                r++;
            }

            int byteLength = n.ToByteArray().Length;

            for (int i = 0; i < MillerRabinRounds; i++)
            {
                // Chọn a ngẫu nhiên trong khoảng [2, n-2] dùng DRBG
                BigInteger a;
                do
                {
                    byte[] aBytes = drbg.GetBytes(byteLength);
                    a = new BigInteger(AppendZero(aBytes));
                    a = BigInteger.Remainder(a, n - 3);
                    if (a.Sign < 0) a += (n - 3);
                    a += 2;
                } while (a < 2 || a >= n - 1);

                BigInteger x = BigInteger.ModPow(a, d, n);

                if (x == 1 || x == n - 1)
                    continue;

                bool found = false;
                for (int j = 0; j < r - 1; j++)
                {
                    x = BigInteger.ModPow(x, 2, n);
                    if (x == n - 1)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                    return false;
            }

            return true;
        }

        #endregion

        #region Math Helpers

        /// <summary>Tính nghịch đảo modular: a^(-1) mod m (Extended Euclidean Algorithm).</summary>
        private static BigInteger ModInverse(BigInteger a, BigInteger m)
        {
            BigInteger g = BigInteger.GreatestCommonDivisor(a, m);
            if (g != BigInteger.One)
                throw new ArithmeticException("Không tồn tại nghịch đảo modular.");

            // Extended Euclidean Algorithm
            BigInteger old_r = a, r_val = m;
            BigInteger old_s = BigInteger.One, s = BigInteger.Zero;

            while (r_val != BigInteger.Zero)
            {
                BigInteger quotient = BigInteger.Divide(old_r, r_val);
                BigInteger temp = r_val;
                r_val = old_r - quotient * r_val;
                old_r = temp;

                temp = s;
                s = old_s - quotient * s;
                old_s = temp;
            }

            return ((old_s % m) + m) % m;
        }

        /// <summary>
        /// Chuyển BigInteger sang byte array big-endian có độ dài cố định.
        /// RSAParameters yêu cầu big-endian, không có byte dấu.
        /// </summary>
        private static byte[] ToFixedBigEndian(BigInteger value, int length)
        {
            // BigInteger.ToByteArray() trả về little-endian + có thể có byte dấu (0x00) ở cuối
            byte[] bytes = value.ToByteArray();

            // Xóa byte dấu nếu có
            int srcLen = bytes.Length;
            if (srcLen > 1 && bytes[srcLen - 1] == 0)
                srcLen--;

            // Đảo ngược sang big-endian
            byte[] result = new byte[length];
            int offset = length - srcLen;

            for (int i = 0; i < srcLen && i < length; i++)
            {
                result[offset + i] = bytes[srcLen - 1 - i];
            }

            return result;
        }

        /// <summary>Thêm byte 0 ở cuối array để BigInteger constructor hiểu là số dương.</summary>
        private static byte[] AppendZero(byte[] bytes)
        {
            byte[] result = new byte[bytes.Length + 1];
            Array.Copy(bytes, result, bytes.Length);
            // result[bytes.Length] = 0 (mặc định)
            return result;
        }

        #endregion
    }

    /// <summary>
    /// AES-256 Counter Mode DRBG (Deterministic Random Bit Generator).
    /// Dùng AES-256 ECB để mã hóa counter tăng dần → tạo dòng byte xác định.
    /// </summary>
    internal sealed class AesCtrDrbg : IDisposable
    {
        private readonly Aes _aes;
        private readonly ICryptoTransform _encryptor;
        private long _counter;
        private byte[] _buffer;
        private int _bufferPos;

        public AesCtrDrbg(byte[] seed)
        {
            if (seed.Length != 32)
                throw new ArgumentException("Seed phải có độ dài 32 bytes (256-bit).");

            _aes = Aes.Create();
            _aes.KeySize = 256;
            _aes.Key = seed;
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
            _encryptor = _aes.CreateEncryptor();
            _counter = 0;
            _buffer = Array.Empty<byte>();
            _bufferPos = 0;
        }

        /// <summary>Lấy count bytes xác định từ DRBG.</summary>
        public byte[] GetBytes(int count)
        {
            byte[] result = new byte[count];
            int written = 0;

            while (written < count)
            {
                if (_bufferPos >= _buffer.Length)
                    RefillBuffer();

                int toCopy = Math.Min(count - written, _buffer.Length - _bufferPos);
                Array.Copy(_buffer, _bufferPos, result, written, toCopy);
                written += toCopy;
                _bufferPos += toCopy;
            }

            return result;
        }

        private void RefillBuffer()
        {
            // Tạo block 16-byte từ counter
            byte[] counterBlock = new byte[16];
            byte[] counterBytes = BitConverter.GetBytes(_counter);
            Array.Copy(counterBytes, 0, counterBlock, 0, 8);
            _counter++;

            // Mã hóa AES-ECB → 16 bytes output xác định
            _buffer = _encryptor.TransformFinalBlock(counterBlock, 0, 16);
            _bufferPos = 0;
        }

        public void Dispose()
        {
            _encryptor?.Dispose();
            _aes?.Dispose();
        }
    }
}
