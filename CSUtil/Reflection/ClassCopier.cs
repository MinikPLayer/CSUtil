using System.Collections.Generic;
using System.Reflection;

namespace CSUtil.Reflection
{
    public static class ClassCopier
    {
        static PropertyInfo[] GetProperties<T>()
        {
            return typeof(T).GetProperties();
        }

        public static int Copy<T1, T2>(T1 source, T2 dest)
        {
            var t1p = GetProperties<T1>();
            var t2p = GetProperties<T2>();

            int copiedCount = 0;
            for (int i = 0; i < t1p.Length; i++)
            {
                for (int j = 0; j < t2p.Length; j++)
                {
                    if (t1p[i].Name == t2p[j].Name && t1p[i].PropertyType == t2p[j].PropertyType)
                    {
                        copiedCount++;
                        t2p[j].SetValue(dest, t1p[i].GetValue(source));
                        break;
                    }
                }
            }

            return copiedCount;
        }

        public static int CopyList<T1, T2>(List<T1> source, out List<T2> dest) where T2 : new()
        {
            var t1p = GetProperties<T1>();
            var t2p = GetProperties<T2>();

            dest = new List<T2>();

            for (int i = 0; i < source.Count; i++)
                dest.Add(new T2());

            int copiedCount = 0;
            for (int i = 0; i < t1p.Length; i++)
            {
                for (int j = 0; j < t2p.Length; j++)
                {
                    if (t1p[i].Name == t2p[j].Name && t1p[i].PropertyType == t2p[j].PropertyType)
                    {
                        copiedCount++;
                        for (int k = 0; k < source.Count; k++)
                        {
                            t2p[j].SetValue(dest[k], t1p[i].GetValue(source[k]));
                        }

                        break;
                    }
                }
            }

            return copiedCount;
        }

        public static int CopySingle<T1>(T1 source, T1 dest)
        {
            var t1p = GetProperties<T1>();

            int copiedCount = 0;
            for (int i = 0; i < t1p.Length; i++)
            {
                t1p[i].SetValue(dest, t1p[i].GetValue(source));
                copiedCount++;
            }

            return copiedCount;
        }
    }
}
