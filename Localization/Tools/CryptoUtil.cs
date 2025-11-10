using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Localization.Tools
{
    public class CryptoUtil
    {
        private static readonly string ALGORITHM = "AES";
        private static readonly int KeySize = 128; // 可以选择 128, 192, 或 256 位密钥长度

        public static string GenerateKey()
        {
            using Aes aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        public static string Encrypt(string plainText, string keyString)
        {
            byte[] key = Convert.FromBase64String(keyString);
            byte[] iv;

            using Aes aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Key = key;
            aes.GenerateIV();
            iv = aes.IV;

            using var memoryStream = new MemoryStream();
            memoryStream.Write(iv, 0, iv.Length);

            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateEncryptor(), CryptoStreamMode.Write))
            {
                using var writer = new StreamWriter(cryptoStream);
                writer.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string Decrypt(string cipherText, string keyString)
        {
            byte[] fullCipher = Convert.FromBase64String(cipherText);
            byte[] iv = new byte[16];
            byte[] cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, iv, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            byte[] key = Convert.FromBase64String(keyString);

            using Aes aes = Aes.Create();
            aes.KeySize = KeySize;
            aes.Key = key;
            aes.IV = iv;

            using var memoryStream = new MemoryStream();
            using (var cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
            {
                using var writer = new StreamWriter(cryptoStream);
                writer.Write(cipher);
            }

            return Encoding.UTF8.GetString(memoryStream.ToArray());
        }
    }
}
