using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using CSUtil.Logging;
using System.Linq;
using Newtonsoft.Json.Linq;
using System.ComponentModel;
using System.Data.SqlTypes;

namespace CSUtil.DB
{
    [AttributeUsage(AttributeTargets.Class)]
    public class SQLCaseSensitiveAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property)]
    public class SQLIgnoreAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    public class SQLPrimaryAttribute : Attribute { }
    [AttributeUsage(AttributeTargets.Property)]
    public class SQLSizeAttribute : Attribute 
    {
        public readonly int size;
        public SQLSizeAttribute(int size) { this.size = size; }
    }

    public class SQLNullableAttribute : Attribute
    {
        public readonly bool Nullable;
        public SQLNullableAttribute(bool nullable) { this.Nullable = nullable; }
    }

    public class Database
    {
        private string? _connectionString = null;
        private MySqlConnection? _con;
        private MySqlConnection Con
        {
            get
            {
                if (_con == null)
                    throw new NullReferenceException("Database connection not initialized");

                if (!IsAlive && _connectionString != null)
                {
                    if(!Connect(_connectionString))
                        throw new Exception("Database connection failed");
                }

                return _con;
            }
        }
        object conLock = new object();

        bool IsAlive
        {
            get
            {
                if (_con == null)
                    return false;
                
                return !(_con.State == ConnectionState.Closed || _con.State == ConnectionState.Broken || _con.State == ConnectionState.Connecting);
            }
        }

        Dictionary<char, string> unsafeChars = new Dictionary<char, string>()
        {
            { '\'', "\\'"},
            { '\"', "\\\"" },
            { '\b', "\\b" },
            { '\n', "\\n" },
            { '\r', "\\r" },
            { '\t', "\\t" },
            { '\\', "\\\\" },
            { '%', "\\%" },
        };

        /// <summary>
        /// Makes query (part) safe, escapes dangerous characters, etc
        /// </summary>
        /// <param name="s">Query string</param>
        /// <returns>Safe query string</returns>
        public string MakeQuerySafe(string s)
        {
            string str = "";
            for (int i = 0; i < s.Length; i++)
            {
                if (unsafeChars.ContainsKey(s[i]))
                    str += unsafeChars[s[i]];
                else
                    str += s[i];
            }

            return str;
        }

        public struct SQLCondition
        {
            public enum ConditionTypes
            {
                Equals,
                Like,
                NotLike,
                IsNull,
                IsNotNull,
                GreaterThan,
                GreaterThanOrEqual,
                LessThan,
                LessThanOrEqual,
            }

            public ConditionTypes type;
            public string name;
            public object value;

            /// <summary>
            /// Junction string, examples: "AND", "OR"
            /// </summary>
            public string junctionOp;

            public const string J_AND = "AND";
            public const string J_OR = "OR";
        }

        MySqlDataReader ExecuteReader(MySqlCommand cmd)
        {
            try
            {
                var rdr = cmd.ExecuteReader();
                return rdr;
            }
            catch (MySqlException e)
            {
                if(!Connect(_connectionString!))
                    throw new Exception("Database connection failed");
                
                var rdr = cmd.ExecuteReader();
                return rdr;
            }
        }

        public static bool IsTypeEqual(Type dbType, PropertyInfo propInfo, bool dbTypeNullable)
        {
            var localType = propInfo.PropertyType;
            var customType = propInfo.GetCustomAttribute<CustomDbTypeAttribute>();
            if (customType != null)
            {
                return customType.DbType == dbType;
            }
            else
            {
                if(dbTypeNullable)
                {
                    var nullableAttribute = propInfo.GetCustomAttribute<SQLNullableAttribute>();
                    if (Nullable.GetUnderlyingType(localType) is Type t)
                    {
                        localType = t;
                    }
                    else if (nullableAttribute == null || !nullableAttribute.Nullable)
                    {
                        return false;
                    }
                }

                return (dbType == typeof(int) && localType.IsEnum) || dbType == localType;
            }
        }

        public MySqlParameter GetParameter(ref string str, int index, object? value)
        {
            string s = "?c" + index.ToString();
            str += s;

            MySqlParameter param;
            if (value == null)
                return new MySqlParameter(s, DBNull.Value);

            var customAttr = value.GetType().GetCustomAttribute<CustomDbTypeAttribute>();
            if (customAttr != null)
            {
                param = new MySqlParameter(s, value.ToString());
            }
            else if (value.GetType() == typeof(byte[]))
            {
                param = new MySqlParameter(s, MySqlDbType.VarBinary);
                param.Value = value;
            }
            else
            {
                param = new MySqlParameter(s, value);
            }

            return param;
        }

        public List<MySqlParameter> GetWhereParameters(ref string str, SQLCondition[] conditions, int startIndex = 0)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            if (conditions.Length > 0)
            {
                str += " WHERE ";
                for (int i = 0; i < conditions.Length; i++)
                {
                    if (i > 0)
                        str += " " + conditions[i].junctionOp + " ";

                    str += conditions[i].name;
                    str += conditions[i].type switch
                    {
                        SQLCondition.ConditionTypes.Like => " LIKE ",
                        SQLCondition.ConditionTypes.NotLike => " NOT LIKE ",
                        SQLCondition.ConditionTypes.IsNull => " IS NULL",
                        SQLCondition.ConditionTypes.IsNotNull => " IS NOT NULL",
                        SQLCondition.ConditionTypes.GreaterThan => " > ",
                        SQLCondition.ConditionTypes.GreaterThanOrEqual => " >= ",
                        SQLCondition.ConditionTypes.LessThan => " < ",
                        SQLCondition.ConditionTypes.LessThanOrEqual => " <= ",
                        _ => "=",
                    };

                    if (conditions[i].type != SQLCondition.ConditionTypes.IsNull && conditions[i].type != SQLCondition.ConditionTypes.IsNotNull)
                        parameters.Add(GetParameter(ref str, startIndex + i, conditions[i].value));
                    
                }
            }

            return parameters;
        }

        List<PropertyInfo> GetProperties<T>(MySqlDataReader rdr = null) => GetProperties(typeof(T), rdr);

        List<PropertyInfo> GetProperties(Type tType, MySqlDataReader rdr = null)
        {
            var fields = new List<PropertyInfo>();
            fields.AddRange(tType.GetProperties());
            if (rdr == null)
            {
                for (int i = 0; i < fields.Count; i++)
                {
                    // Ignore field with attribute SQLIgnore
                    if (Attribute.IsDefined(fields[i], typeof(SQLIgnoreAttribute)))
                    {
                        fields.RemoveAt(i);
                        i--;
                    }
                }

                return fields;
            }

            var returnFields = new List<PropertyInfo>();

            var schema = rdr.GetColumnSchema();
            for (int i = 0; i < schema.Count; i++)
            {
                var type = schema[i].DataType;
                var name = schema[i].ColumnName;
                bool found = false;
                for (int j = 0; j < fields.Count; j++)
                {
                    if (IsTypeEqual(type, fields[j], (bool)schema[i].AllowDBNull) && fields[j].Name == name)
                    {
                        found = true;
                        if (!Attribute.IsDefined(fields[j], typeof(SQLIgnoreAttribute)))
                            returnFields.Add(fields[j]);
                        break;
                    }
                }

                if (!found)
                    Console.WriteLine($"[WARNING] Cannot find property {name} in {tType.FullName}");
                
            }

            return returnFields;
        }

        public int Count(string table, params SQLCondition[] conditions)
        {
            string str = $"SELECT COUNT(*) FROM {table}";
            var parameters = GetWhereParameters(ref str, conditions);
            str += ";";

            MySqlCommand cmd = new MySqlCommand(str, Con);
            for (int i = 0; i < parameters.Count; i++)
                cmd.Parameters.Add(parameters[i]);   

            lock (conLock)
            {
                var reader = ExecuteReader(cmd);
                reader.Read();
                var val = reader.GetInt32(0);
                reader.Close();
                return val;
            }
        }

        public int Delete(string table, params SQLCondition[] conditionParams)
        {
            string str = "DELETE FROM " + table;
            var parameters = GetWhereParameters(ref str, conditionParams);
            str += ";";

            MySqlCommand cmd = new MySqlCommand(str, Con);
            for (int i = 0; i < parameters.Count; i++)
                cmd.Parameters.Add(parameters[i]);

            lock (conLock)
                return cmd.ExecuteNonQuery();
        }

        public int Update<T>(T value, string table, params SQLCondition[] conditionsParams) => Update(value, table, null, conditionsParams);
        public int Update<T>(T value, string table, List<PropertyInfo> fields, params SQLCondition[] conditionsParams)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            string str = "UPDATE " + table + " SET ";
            if (fields == null)
                fields = GetProperties<T>(null);

            int it = 0;
            for (int i = 0; i < fields.Count; i++)
            {
                str += fields[i].Name + "=";
                parameters.Add(GetParameter(ref str, it++, fields[i].GetValue(value)));

                if (i != fields.Count - 1)
                    str += ",";
            }
            parameters.AddRange(GetWhereParameters(ref str, conditionsParams, it));
            str += ";";
            
            MySqlCommand cmd = new MySqlCommand(str, Con);
            for (int i = 0; i < parameters.Count; i++)
                cmd.Parameters.Add(parameters[i]);

            lock (conLock)
                return cmd.ExecuteNonQuery();
        }

        public int InsertArray<T>(T[] values, string table, List<PropertyInfo> fields = null)
        {
            if (values.Length == 0)
                return 0;
            
            string str = "INSERT INTO " + table + "(";
            if (fields == null)
                fields = GetProperties<T>(null);

            for (int i = 0; i < fields.Count; i++)
            {
                str += fields[i].Name;

                if (i != fields.Count - 1)
                    str += ",";
                else
                    str += " ) ";
            }

            List<MySqlParameter> parameters = new List<MySqlParameter>();
            if (fields.Count > 0)
            {
                int it = 0;
                str += " VALUES";
                for (int j = 0; j < values.Length; j++)
                {
                    var value = values[j];

                    str += "(";
                    for (int i = 0; i < fields.Count; i++)
                    {
                        parameters.Add(GetParameter(ref str, it++, fields[i].GetValue(value)));

                        if (i != fields.Count - 1)
                            str += ",";
                        else
                            str += ")";
                    }

                    if (j != values.Length - 1)
                        str += ",";
                }

                str += ";";
            }

            lock (conLock)
            {
                MySqlCommand cmd = new MySqlCommand(str, Con);
                for (int i = 0; i < parameters.Count; i++)
                    cmd.Parameters.Add(parameters[i]);
                
                return cmd.ExecuteNonQuery();
            }
        }

        public int InsertData<T>(T value, string table, List<PropertyInfo> fields = null) => InsertArray(new T[1] { value }, table, fields);
        public List<T> GetData<T>(string table, params SQLCondition[] conditionsParams) where T : new() => GetData<T>(table, "*", "", null, -1, conditionsParams);
        public List<T> GetData<T>(string table, string orderBy, params SQLCondition[] conditionParams) where T : new() => GetData<T>(table, "*", orderBy, null, -1, conditionParams);
        public List<T> GetDataLimit<T>(string table, int limit, params SQLCondition[] conditionParams) where T : new() => GetData<T>(table, "*", "", null, limit, conditionParams);
        public List<T> GetDataLimit<T>(string table, string orderBy, int limit, params SQLCondition[] conditionParams) where T : new() => GetData<T>(table, "*", orderBy, null, limit, conditionParams);

        /// <summary>
        /// Wrapper for easier use of SendQuery function. 
        /// WARNING! I've tried for it to be safe, but it's probably not. Use with caution!
        /// </summary>
        /// <typeparam name="T">Type of result struct, it MUST match with DB table struct</typeparam>
        /// <param name="table">Table to send query to</param>
        /// <param name="queryFilter">Filter for query, example: "*" for all, "(p1, p2)" for columns p1 and p2</param>
        /// <param name="fields">Fields to populate in the struct, leave null to auto populate</param>
        /// <returns>List of structs containing data from query</returns>
        public List<T> GetData<T>(string table, string queryFilter, string orderBy, List<PropertyInfo> fields, int limit, params SQLCondition[] conditionsParams) where T : new()
        {
            string str = "SELECT " + queryFilter + " FROM " + table;
            str = MakeQuerySafe(str);
            var parameters = GetWhereParameters(ref str, conditionsParams);

            if (!string.IsNullOrEmpty(orderBy))
                str += " ORDER BY " + orderBy;

            if (limit > 0)
                str += " LIMIT " + limit.ToString();

            str += ";";

            MySqlCommand cmd = new MySqlCommand(str, Con);
            for (int i = 0; i < parameters.Count; i++)
                cmd.Parameters.Add(parameters[i]);  

            return SendQuery<T>(cmd, fields);
        }

        /// <summary>
        /// Sends a raw SQL request, use ?c{i} to add a parameter, where i is the index of the parameter in the arguments array (for example ?c0, ?c1, ?c2, etc.)
        /// </summary>
        /// <typeparam name="T">Return type</typeparam>
        /// <param name="sql">SQL command</param>
        /// <param name="arguments">Parameters</param>
        /// <returns>SQL returns</returns>
        public List<T> RunSQL<T>(string sql, params object?[] arguments) where T : new()
        {
            var cmd = new MySqlCommand(sql, Con);
            for(var i = 0; i < arguments.Length; i++)
            {
                if(!sql.Contains($"?c{i}"))
                    throw new ArgumentException("Sql doesn't contain reference to argument " + i);

                cmd.Parameters.AddWithValue($"?c{i}", arguments[i]);
            }

            // MySQL automatically checks if sql parameters ?c{i} are included, so no need to check it here

            return SendQuery<T>(cmd);
        }

        /// <summary>
        /// Sends query to databse and returns result
        /// </summary>
        /// <typeparam name="T">Type of result struct, it MUST match with DB table struct</typeparam>
        /// <param name="query">Query to send (ex. "SELECT * FROM table WHERE x=1")</param>
        /// <param name="fields">Fields to populate in the struct, leave null to auto populate</param>
        /// <returns>List of structs containing data from query</returns>
        private List<T> SendQuery<T>(MySqlCommand cmd, List<PropertyInfo> fields = null) where T : new()
        {
            if (cmd.Connection != Con)
            {
                throw new Exception("Unauthorized SQL query, bad connection");
            }

            List<T> ret = new List<T>();
            //MySqlCommand cmd = new MySqlCommand(query, con);
            lock (conLock)
            {
                MySqlDataReader rdr = ExecuteReader(cmd);
                while (rdr.Read())
                {
                    object[] values = new object[rdr.FieldCount];
                    // Get row
                    rdr.GetValues(values);

                    // Check for single types (like int, string, etc)
                    var tp = typeof(T);
                    if (Nullable.GetUnderlyingType(tp) is Type t)
                        tp = t;

                    if (!tp.IsClass && tp.IsValueType && values.Length == 1)
                    {
                        var value = values[0];
                        var lst = new List<T>
                        {
                            (T)Convert.ChangeType(value, tp)
                        };
                        return lst;
                    }

                    if (fields == null)
                        fields = GetProperties<T>(rdr);

                    if (values.Length != fields.Count)
                    {
                        throw new Exception("Struct fields don't match SQL table fields");
                    }

                    T newT = new T();
                    for (int i = 0; i < fields.Count; i++)
                    {
                        if (!fields[i].CanWrite)
                            continue;
                            
                        if (values[i].GetType() == typeof(DBNull))
                        {
                            fields[i].SetValue(newT, null);
                        }
                        else
                        {
                            var val = values[i];
                            var type = val.GetType();
                            if (type != fields[i].PropertyType && !fields[i].PropertyType.IsEnum)
                            {
                                var converter = TypeDescriptor.GetConverter(fields[i].PropertyType);
                                if (!converter.CanConvertFrom(type))
                                    throw new ArgumentException($"Cannot convert from {type} to {fields[i].PropertyType}");

                                val = converter.ConvertFrom(val);
                            }
                            
                            fields[i].SetValue(newT, val);

                        }
                    }
                    ret.Add(newT);
                }
                rdr.Close();
                return ret;
            }
        }

        public void SendQuery(string query)
        {
            MySqlCommand cmd = new MySqlCommand(query, Con);
            lock (conLock)
                cmd.ExecuteNonQuery();
        }

        bool CheckTableExists(string name)
        {
            string db = Con.Database;

            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{db}' AND TABLE_NAME = '{name}'; ", Con);
            bool good = false;
            lock (conLock)
            {
                MySqlDataReader rdr = ExecuteReader(cmd);

                // If true it means that we have an return - which means we have anything
                good = rdr.Read();

                rdr.Close();
            }

            return good;
        }

        string GetDbTypeName(Type type, PropertyInfo info)
        {
            var dbType = type.GetCustomAttribute<CustomDbTypeAttribute>();
            if (dbType != null)
                return dbType.DbName;

            var attr = info.GetCustomAttribute<SQLSizeAttribute>();
            if (type == typeof(string))
            {            
                if (attr != null)
                    return "VARCHAR(" + attr.size.ToString() + ")";
                else
                    return "TEXT";
            }
            if (type == typeof(byte[]))
            {
                if (attr == null)
                    throw new Exception("Size attribute is required for byte arrays");
                else
                    return $"VARBINARY({attr.size})";
            }

            var sizeName = attr == null ? "" : $"({attr.size})";
            if (type == typeof(int) || type.IsEnum)
                return "INT" + sizeName;

            if (type == typeof(long))
                return "BIGINT" + sizeName;

            if (type == typeof(float))
                return "FLOAT" + sizeName;

            if (type == typeof(double))
                return "DOUBLE" + sizeName;

            if (type == typeof(TimeSpan))
                return "TIME" + sizeName;


            if (type.IsArray)
                throw new Exception("Arrays other than byte[] are not yet supported");

            return type.Name.ToUpper() + sizeName;
        }

        string GetDbTypeName(PropertyInfo info, bool nullability = false)
        {
            Type type;
            string nullStr;

            var nullableAtrribute =
                info.GetCustomAttributes().OfType<SQLNullableAttribute>().FirstOrDefault();

            if (Nullable.GetUnderlyingType(info.PropertyType) is Type t && (nullableAtrribute == null || nullableAtrribute.Nullable != false))
            {
                type = t;
                nullStr = " NULL";
            }
            else if (nullableAtrribute != null && nullableAtrribute.Nullable == true)
            {
                type = info.PropertyType;
                nullStr = " NULL";
            }
            else
            {
                type = info.PropertyType;
                nullStr = " NOT NULL";
            }

            return GetDbTypeName(type, info) + (nullability ? nullStr : "");
        }

        public string GetPrimaryKey(string table)
        {
            string cmdText = $"SHOW KEYS FROM `{table}` WHERE Key_name = 'PRIMARY'";
            MySqlCommand cmd = new MySqlCommand(cmdText, Con);

            var rdr = ExecuteReader(cmd);

            bool good = rdr.Read();
            string ret = "";
            if(good)
            {
                object[] values = new object[rdr.FieldCount];
                rdr.GetValues(values);

                ret = (string)values[4];
            }

            rdr.Close();
            return ret;
        }

        public void DropTable(string table)
        {
            Log.Normal("Dropping table \"" + table + "\"...");
            var c = new MySqlCommand($"DROP TABLE `{table}`;", Con);
            c.ExecuteNonQuery();
            Log.Normal("Dropping table done");
        }

        public void CreateDBStruct(List<(string, Type)> tables)
        {
            Log.Normal("Checking tables...");
            for(int i = 0;i<tables.Count;i++)
            {
                Type type = tables[i].Item2;
                string tableName = tables[i].Item1;
                Log.Normal($"\tChecking table `{tableName}`...");

                var properties = GetProperties(type);
                // Skip empty tables
                if(properties.Count == 0)
                    continue;

                if(!CheckTableExists(tableName))
                {
                    string primary = "";

                    Log.Normal($"\t\tCreating table `{tableName}`...");
                    string cText = $"CREATE TABLE `{Con.Database}`.`{tableName}` (";
                    for (int j = 0; j < properties.Count;j++)
                    {
                        if (properties[j].GetCustomAttribute<SQLPrimaryAttribute>() != null)
                            primary = properties[j].Name;

                        cText += "`" + properties[j].Name + "` " + GetDbTypeName(properties[j], true);
                        if (j != properties.Count - 1)
                            cText += ", ";
                    }

                    cText += ")";
                    if (type.GetCustomAttribute<SQLCaseSensitiveAttribute>() != null)
                        cText += " COLLATE utf8_bin";

                    cText += ";";

                    MySqlCommand c = new MySqlCommand(cText, Con);
                    c.ExecuteNonQuery();
                    
                    if(primary != "")
                    {
                        c = new MySqlCommand($"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primary}`);", Con);
                        c.ExecuteNonQuery();
                    }

                    continue;
                }

                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {tableName};", Con);
                var rdr = ExecuteReader(cmd);
                var columns = rdr.GetColumnSchema();
                rdr.Close();


                string primaryKey = "";
                // Find changes and fix them
                for (int j = 0;j<properties.Count;j++)
                {
                    var prop = properties[j];

                    if (prop.GetCustomAttribute<SQLIgnoreAttribute>() != null)
                        continue;

                    if(prop.GetCustomAttribute<SQLPrimaryAttribute>() != null)
                        primaryKey = prop.Name;

                    bool found = false;
                    for (int k = 0; k < columns.Count;k++)
                    {
                        var col = columns[k];
                        if(IsTypeEqual(col.DataType, prop, (bool)col.AllowDBNull) &&
                            col.ColumnName == prop.Name)
                        {
                            found = true;
                            break;
                        }
                    }

                    if(!found)
                    {
                        Log.Normal($"\t\tAdding column `{prop.Name}` to `{tableName}`...");
                        MySqlCommand c = new MySqlCommand($"ALTER TABLE `{tableName}` ADD `{prop.Name}` {GetDbTypeName(prop, true)};", Con);
                        c.ExecuteNonQuery();
                    }
                }

                var tablePrimary = GetPrimaryKey(tableName);
                if(tablePrimary != primaryKey)
                {
                    if(tablePrimary != "")
                        new MySqlCommand($"ALTER TABLE `{tableName}` DROP PRIMARY KEY;", Con).ExecuteNonQuery();

                    if (primaryKey != "")
                        new MySqlCommand($"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primaryKey}`)", Con).ExecuteNonQuery();
                }
            }
            Log.Normal("Checking tables done");
        }

        public bool Connect(string connectionString)
        {
            lock (conLock)
            {
                try
                {
                    _con = new MySqlConnection(connectionString);
                    _con.Open();
                    _connectionString = connectionString;
                }
                catch(MySqlException e)
                {
                    Log.FatalError("Cannot connect to database: \n" + e.ToString());
                    throw new Exception("Cannot connect to database");
                }
            }

            return IsAlive;
        }

        public bool Connect(string username, string password, string dbName, string ip = "127.0.0.1", string port = "3306")
        {
            return Connect("server=" + ip + ";user=" + username + ";database=" + dbName + ";port=" + port.ToString() + ";password=" + password);
        }
    }
}
