using PWDTK_NETCore;
using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Text;

namespace CSUtil.Crypto
{
    public static class Password
    {
        public struct HashedPassword
        {
            public byte[] hash;
            public byte[] salt;
        }

        const int tokenLength = 128;
        public static string GenerateToken(int length = -1)
        {
            if(length < 0)
                length = tokenLength;

            var salt = GenerateSalt(length);
            var token = Convert.ToBase64String(salt);
            return token.Substring(0, tokenLength);
        }

        public const int saltLength = 64;
        public static byte[] GenerateSalt(int length = -1)
        {
            if (length < 0)
                length = saltLength;

            return PWDTK.GetRandomSalt(length);
        }

        public static HashedPassword GetPasswordHash(string password, byte[] salt = null)
        {
            if (salt == null)
                salt = GenerateSalt();

            var hash = PWDTK.PasswordToHash(salt, password);
            return new HashedPassword() { salt = salt, hash = hash };
        }

        public static bool ComparePasswords(byte[] pass1, byte[] pass2)
        {
            if (pass1.Length != pass2.Length)
                return false;

            for (int i = 0; i < pass1.Length; i++)
            {
                if (pass1[i] != pass2[i])
                    return false;
            }

            return true;
        }

    }
}
