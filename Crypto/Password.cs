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

        static (char start, char end)[] randomCharRanges = new (char start, char end)[]
        {
            ('a', 'z'),
            ('A', 'Z'),
            ('0', '9')
        };
        
        static char GetRandomChar()
        {
            int r1 = Random.Shared.Next(0, randomCharRanges.Length);
            int r2 = Random.Shared.Next(0, randomCharRanges[r1].end - randomCharRanges[r1].start + 1);
            
            return (char)(randomCharRanges[r1].start + r2);
        }

        const int tokenLength = 64;
        public static string GenerateToken(int length = -1)
        {
            if(length < 0)
                length = tokenLength;
                
            string ret = "";
            for (int i = 0; i < length; i++)
                ret += GetRandomChar();
                
            return ret;
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
