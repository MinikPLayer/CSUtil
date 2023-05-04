using CSUtil.Crypto;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
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

        public static bool IsSameType(Type t1, Type t2)
        {
            if (t1 == t2)
                return true;

            if (Nullable.GetUnderlyingType(t1) is Type nnt1)
                t1 = nnt1;

            if (Nullable.GetUnderlyingType(t2) is Type nnt2)
                t2 = nnt2;

            return t1 == t2;
        }

        public static T Create<T>(object source) where T: new()
        {
            T dest = new T();
            var t1p = source.GetType().GetProperties();
            var t2p = GetProperties<T>();

            for (int i = 0; i < t1p.Length; i++)
            {
                for (int j = 0; j < t2p.Length; j++)
                {
                    if (t1p[i].Name == t2p[j].Name && IsSameType(t1p[i].PropertyType, t2p[j].PropertyType))
                    {
                        if(t2p[j].CanWrite && t1p[i].CanRead)
                            t2p[j].SetValue(dest, t1p[i].GetValue(source));
                        
                        break;
                    }
                }
            }

            return dest;
        }
        
        public static List<T2> CreateList<T1, T2>(List<T1> source) where T2 : new()
        {
            var t1p = GetProperties<T1>();
            var t2p = GetProperties<T2>();

            var dest = new List<T2>();

            for (int i = 0; i < source.Count; i++)
                dest.Add(new T2());

            for (int i = 0; i < t1p.Length; i++)
            {
                for (int j = 0; j < t2p.Length; j++)
                {
                    if (t1p[i].Name == t2p[j].Name && IsSameType(t1p[i].PropertyType, t2p[j].PropertyType))
                    {
                        if(t2p[j].CanWrite && t1p[i].CanRead)
                            for (int k = 0; k < source.Count; k++)
                                t2p[j].SetValue(dest[k], t1p[i].GetValue(source[k]));

                        break;
                    }
                }
            }

            return dest;
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
                    if (t1p[i].Name == t2p[j].Name && IsSameType(t1p[i].PropertyType, t2p[j].PropertyType))
                    {
                        if (t2p[j].CanWrite && t1p[i].CanRead)
                        {
                            t2p[j].SetValue(dest, t1p[i].GetValue(source));
                            copiedCount++;
                        }
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
                    if (t1p[i].Name == t2p[j].Name && IsSameType(t1p[i].PropertyType, t2p[j].PropertyType))
                    {
                        if (t2p[j].CanWrite && t1p[i].CanRead)
                        {
                            copiedCount++;
                            for (int k = 0; k < source.Count; k++)
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
                if (t1p[i].CanWrite && t1p[i].CanRead)
                {
                    t1p[i].SetValue(dest, t1p[i].GetValue(source));
                    copiedCount++;
                }
            }

            return copiedCount;
        }
    }

    public class ClassCopierTests
    {
        class St1
        {
            public string P1 { get; set; } = "";
            public int P2 { get; set; } = 0;
            public byte[] P3 { get; set; } = new byte[0];
            public DateTime D4 { get; set; } = DateTime.MinValue;
        }

        class St2
        {
            public string? P1 { get; set; } = null;
            public int? P2 { get;  set; } = null;
            public DateTime? D4 { get; set; } = null;

            public int? Other { get; set; } = null;
        }

        class St3
        {
            public string? P1 = null;
            public int? P2 = null;
            public DateTime? D4 = null;
        }

        static St1 GetTestData()
        {
            var bytes = new byte[Random.Shared.Next(10, 20)];
            for (var i = 0; i < bytes.Length; i++)
                bytes[i] = (byte)Random.Shared.Next(0, 255);

            return new St1 { P1 = Password.GenerateToken(), P2 = Random.Shared.Next(), P3 = bytes, D4 = DateTime.Now };
        }

        void CompareSt1St2(St1 source, St2 dest)
        {
            Assert.That(dest.P1, Is.EqualTo(source.P1));
            Assert.That(dest.P2, Is.EqualTo(source.P2));
            Assert.That(dest.D4, Is.EqualTo(source.D4));

            Assert.That(dest.Other, Is.Null);
        }

        [Test]
        public void Single()
        {
            St1 source = GetTestData();
            var dest = ClassCopier.Create<St2>(source);

            St2 dest2 = new St2();
            var count = ClassCopier.Copy<St1, St2>(source, dest2);

            Assert.That(count, Is.EqualTo(3));

            CompareSt1St2(source, dest);
            CompareSt1St2(source, dest2);
        }

        [Test]
        public void List()
        {
            const int length = 10_000;

            var source = new List<St1>();
            for (var i = 0; i < length; i++)
                source.Add(GetTestData());

            var count = ClassCopier.CopyList(source, out List<St2> dest);
            var dest2 = ClassCopier.CreateList<St1, St2>(source);

            Assert.That(count, Is.EqualTo(3));

            Assert.That(source.Count, Is.EqualTo(dest.Count));
            Assert.That(source.Count, Is.EqualTo(dest2.Count));

            for (var i = 0; i < source.Count; i++)
            {
                CompareSt1St2(source[i], dest[i]);
                CompareSt1St2(source[i], dest2[i]);
            }
        }

        [Test]
        public void IgnoreNonProperties()
        {
            var source = GetTestData();
            var dest = ClassCopier.Create<St3>(source);
            var dest2 = new St3();
            var count = ClassCopier.Copy<St1, St3>(source, dest2);

            Assert.That(count, Is.EqualTo(0));
            Assert.That(dest.P1, Is.Null);
            Assert.That(dest.P2, Is.Null);
            Assert.That(dest.D4, Is.Null);
        }

        [Test]
        public void CreateSingle()
        {
            var source = GetTestData();
            var dest = ClassCopier.Create<St2>(source);

            CompareSt1St2(source, dest);

            source.P1 += "abcd";

            Assert.That(source.P1, Is.Not.EqualTo(dest.P1));
        }
    }
}
