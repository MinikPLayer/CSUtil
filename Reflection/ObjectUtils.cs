using System.Reflection;

namespace CSUtil.Reflection
{
    public static class ObjectUtils
    {
        public static void SetNotNullFields<T>(ref T targetObject, T setSource)
        {
            var tp = ClassCopier.GetProperties<T>();
            foreach (var t in tp)
            {
                if(!t.CanWrite)
                    continue;
                
                var val = t.GetValue(setSource);
                if (val == null)
                    continue;
                
                t.SetValue(targetObject, val);
            }
        }
    }
}