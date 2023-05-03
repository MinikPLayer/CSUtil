using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.DB
{
    public class SqlTableAttribute : Attribute
    {
        public string TableName { get; set; } = "";
        public SqlTableAttribute(string name)
        {
            this.TableName = name;
        }
    }

    public static class DatabaseManager
    {
        /// <summary>
        /// Creates database structure
        /// </summary>
        /// <param name="db">Database structure</param>
        /// <param name="assembly">Assembly to get SqlTableAttribute types from. Can be obtained using Assembly.GetExecutingAssembly()</param>
        /// <exception cref="NullReferenceException"></exception>
        public static void CreateStructure(Database db, Assembly assembly)
        {
            if (db is null)
                throw new NullReferenceException("Not initialized");

            var types = assembly.GetTypes().Where(t => t.IsDefined(typeof(SqlTableAttribute)));
            var tables = new List<(string table, Type type)>();
            tables.AddRange(types.Select(x =>
            {
                var attr = x.GetCustomAttribute<SqlTableAttribute>();
                if (attr == null)
                    return ("", x);

                return (attr.TableName, x);
            }).Where(x => !string.IsNullOrEmpty(x.Item1)));

            db.CreateDBStruct(tables);
        }
    }
}
