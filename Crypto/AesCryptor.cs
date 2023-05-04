using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.Crypto
{
    public static class AesCryptor
    {
        public const int KEY_LENGTH = 16;

        public static (byte[] key, byte[] iv) GenerateRandomKey()
        {
            var key = Password.GenerateSalt(KEY_LENGTH);
            var iv = Password.GenerateSalt(KEY_LENGTH);

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

    public class AesCryptorTests
    {
        [Test]
        [TestCase(1)]
        [TestCase(256)]
        [TestCase(512)]
        [TestCase(2130)]
        [TestCase(0, "abcdąć!@#$%^&*()🙂")]
        public void TestRoundTrip(int length, string? overrideData = null)
        {
            var (key, iv) = AesCryptor.GenerateRandomKey();
            Assert.That(key, Is.Not.Null);
            Assert.That(iv, Is.Not.Null);
            Assert.That(key.Length, Is.EqualTo(AesCryptor.KEY_LENGTH));
            Assert.That(iv.Length, Is.EqualTo(AesCryptor.KEY_LENGTH));

            string data = overrideData ?? Password.GenerateToken(length);

            var encrypted = AesCryptor.Encrypt(data, key, iv);
            var decrypted = AesCryptor.DecryptString(encrypted, key, iv);

            Assert.That(encrypted, Is.Not.Null);
            Assert.That(decrypted, Is.Not.Null);
            Assert.That(data, Is.EqualTo(decrypted));
        }
    }
}
