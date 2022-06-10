using PWDTK_NETCore;
using System;
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

        public const int saltLength = 64;
        public static byte[] GenerateSalt()
        {
            return PWDTK.GetRandomSalt(saltLength);
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
