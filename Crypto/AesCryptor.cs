using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.Crypto
{
    public static class AesCryptor
    {
        public static (byte[] key, byte[] iv) GenerateRandomKey()
        {
            var key = Password.GenerateSalt(16);
            var iv = Password.GenerateSalt(16);

            return (key, iv);
        }

        public static byte[] Encrypt(string data, byte[] key, byte[] iv) => Encrypt(Encoding.UTF8.GetBytes(data), key, iv);
        public static byte[] Encrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                using (var encryptor = aes.CreateEncryptor())
                    return encryptor.TransformFinalBlock(data, 0, data.Length);
                
            }
        }

        public static string DecryptString(byte[] data, byte[] key, byte[] iv) => Encoding.UTF8.GetString(Decrypt(data, key, iv));
        public static byte[] Decrypt(byte[] data, byte[] key, byte[] iv)
        {
            using (var aes = System.Security.Cryptography.Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = System.Security.Cryptography.CipherMode.CBC;
                aes.Padding = System.Security.Cryptography.PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                    return decryptor.TransformFinalBlock(data, 0, data.Length);
                
            }
        }
    }
}
