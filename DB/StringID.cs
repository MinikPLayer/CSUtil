using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.DB
{
    [TypeConverter(typeof(StringIDConverter))]
    [CustomDbType("VARCHAR(36)", typeof(string))]
    public class StringID
    {
        private string id = "";

        private StringID(string id)
        {
            this.id = id;
        }

        public static implicit operator string(StringID id) => id.id;
        public static implicit operator StringID(string data) => new StringID(data);

        public static StringID Random() => new StringID(Guid.NewGuid().ToString());
        public static StringID Random(Database db, string table, string column) => db.GenerateUniqueIdString(table, column);
        public static StringID Empty() => new StringID("");

        public override string ToString() => id;
    }

    public class StringIDConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType) => sourceType == typeof(string);
        public override bool CanConvertTo(ITypeDescriptorContext? context, [NotNullWhen(true)] Type? destinationType) => destinationType == typeof(string);

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is not string s)
                throw new ArgumentException("Invalid type");

            var sId = (StringID)s;
            return sId;
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is not StringID id || destinationType != typeof(string))
                throw new ArgumentException("Invalid type");

            return id.ToString();
        }
    }
}
