using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.DB
{
    public class CustomDbTypeAttribute : Attribute
    {
        public string DbName { get; }
        public Type DbType { get; }

        public CustomDbTypeAttribute(string name, Type type)
        {
            DbName = name;
            DbType = type;
        }
    }
}
