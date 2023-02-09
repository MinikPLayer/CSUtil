﻿using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using CSUtil.Logging;

namespace CSUtil.DB
{
    public class SQLIgnoreAttribute : Attribute { }
    public class SQLSizeAttribute : Attribute 
    {
        public readonly int size;
        public SQLSizeAttribute(int size) { this.size = size; }
    }

    public class SQLPrimaryAttribute : Attribute { }

    public class Database
    {
        MySqlConnection con = null;
        object conLock = new object();

        public bool IsAlive
        {
            get
            {
                if (con == null)
                    return false;
                return !(con.State == System.Data.ConnectionState.Closed || con.State == System.Data.ConnectionState.Broken || con.State == System.Data.ConnectionState.Connecting);
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
            /*return s.Replace("'", "\\'")
                .Replace("\"", "\\\"")
                .Replace("b", "\\b")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r")
                .Replace("\t", "\\t")
                .Replace("\\", "\\\\");*/

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
                IsNotNull
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

        public static bool IsTypeEqual(Type dbType, Type localType)
        {
            return (dbType == typeof(int) && localType.IsEnum) || dbType == localType;
        }

        public List<MySqlParameter> GetParameters(ref string str, SQLCondition[] conditions)
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
                    if (conditions[i].type == SQLCondition.ConditionTypes.Like)
                        str += " LIKE ";
                    else if (conditions[i].type == SQLCondition.ConditionTypes.NotLike)
                        str += " NOT LIKE ";
                    else if (conditions[i].type == SQLCondition.ConditionTypes.IsNull)
                    {
                        str += " IS NULL";
                        continue;
                    }
                    else if (conditions[i].type == SQLCondition.ConditionTypes.IsNotNull)
                    {
                        str += " IS NOT NULL";
                        continue;
                    }
                    else
                        str += "=";


                    string s = "?c" + i.ToString();
                    str += s;
                    MySqlParameter param = null;
                    if (conditions[i].value.GetType() == typeof(byte[]))
                    {
                        param = new MySqlParameter(s, MySqlDbType.VarBinary);
                        param.Value = conditions[i].value;
                    }
                    else
                    {
                        param = new MySqlParameter(s, conditions[i].value);
                    }
                    parameters.Add(param);
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
                    if (IsTypeEqual(type, fields[j].PropertyType) && fields[j].Name == name)
                    {
                        found = true;
                        if (!Attribute.IsDefined(fields[j], typeof(SQLIgnoreAttribute)))
                            returnFields.Add(fields[j]);
                        break;
                    }
                }
                if (!found)
                {
                    Console.WriteLine($"[WARNING] Cannot find property {name} in {tType.FullName}");
                }
            }

            return returnFields;
        }

        public int Count(string table, params SQLCondition[] conditions)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            string str = $"SELECT COUNT(*) FROM {table}";

            /*if (conditions.Length > 0)
            {
                str += " WHERE ";
                for (int i = 0; i < conditions.Length; i++)
                {
                    str += conditions[i].name + "=";
                    string s = "?c" + i.ToString();
                    str += s;
                    parameters.Add(new MySqlParameter(s, conditions[i].value));

                    str += " " + conditions[i].junctionOp + " ";
                }
            }*/
            parameters = GetParameters(ref str, conditions);

            str += ";";


            MySqlCommand cmd = new MySqlCommand(str, con);

            for (int i = 0; i < parameters.Count; i++)
            {
                cmd.Parameters.Add(parameters[i]);
            }

            lock (conLock)
            {
                var reader = cmd.ExecuteReader();
                reader.Read();
                var val = reader.GetInt32(0);
                reader.Close();
                return val;
            }
        }

        public int Delete(string table, params SQLCondition[] conditionParams)
        {
            string str = "DELETE FROM " + table;
            var parameters = GetParameters(ref str, conditionParams);

            str += ";";

            MySqlCommand cmd = new MySqlCommand(str, con);

            for (int i = 0; i < parameters.Count; i++)
            {
                cmd.Parameters.Add(parameters[i]);
            }

            lock (conLock)
            {
                return cmd.ExecuteNonQuery();
            }
        }

        static string ByteArrayToVarBinaryString(byte[] data)
        {
            return "0x" + BitConverter.ToString(data).Replace("-", "");
        }

        public int Update<T>(T value, string table, params SQLCondition[] conditionsParams)
        {
            return Update(value, table, null, conditionsParams);
        }

        public int Update<T>(T value, string table, List<PropertyInfo> fields, params SQLCondition[] conditionsParams)
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            string str = "UPDATE " + table + " SET ";
            if (fields == null)
                fields = GetProperties<T>(null);

            for (int i = 0; i < fields.Count; i++)
            {
                bool raw = false;
                string valueStr = "";
                if(fields[i].PropertyType == typeof(DateTime))
                {
                    DateTime dt = (DateTime)fields[i].GetValue(value);
                    valueStr = dt.ToString("yyyy-MM-dd hh:mm:ss");
                }
                else if (fields[i].PropertyType == typeof(System.Byte[]))
                {
                    valueStr = ByteArrayToVarBinaryString((byte[]) fields[i].GetValue(value) ?? new byte[] { } );
                    raw = true;
                }
                else if (fields[i].PropertyType == typeof(bool))
                {
                    valueStr = (bool)fields[i].GetValue(value) ? "1" : "0";
                }
                else
                {
                    valueStr = fields[i].GetValue(value).ToString();
                }

                valueStr = MakeQuerySafe(valueStr);

                
                str += fields[i].Name;
                if(raw)
                    str += '=' + valueStr;
                else
                    str += "=\'" + valueStr + "\'";

                if (i != fields.Count - 1)
                    str += ",";
            }
            parameters.AddRange(GetParameters(ref str, conditionsParams));
            str += ";";
            
            MySqlCommand cmd = new MySqlCommand(str, con);

            for (int i = 0; i < parameters.Count; i++)
            {
                cmd.Parameters.Add(parameters[i]);
            }

            lock (conLock)
            {
                return cmd.ExecuteNonQuery();
            }
        }

        public int InsertArray<T>(T[] values, string table, List<PropertyInfo> fields = null)
        {
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
                        string s = "?c" + it++.ToString();
                        str += s;
                        MySqlParameter param;
                        if (fields[i].GetValue(value).GetType() == typeof(byte[]))
                        {
                            param = new MySqlParameter(s, MySqlDbType.VarBinary);
                            param.Value = fields[i].GetValue(value);
                        }
                        else
                        {
                            param = new MySqlParameter(s, fields[i].GetValue(value));
                        }
                        parameters.Add(param);

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
                MySqlCommand cmd = new MySqlCommand(str, con);
                for (int i = 0; i < parameters.Count; i++)
                {
                    cmd.Parameters.Add(parameters[i]);
                }
                return cmd.ExecuteNonQuery();
            }
        }

        public int InsertData<T>(T value, string table, List<PropertyInfo> fields = null)
        {
            return InsertArray(new T[1] { value }, table, fields);
        }

        public List<T> GetData<T>(string table, params SQLCondition[] conditionsParams) where T : new()
        {
            return GetData<T>(table, "*", "", null, conditionsParams);
        }

        public List<T> GetData<T>(string table, string sortBy, params SQLCondition[] conditionParams) where T : new()
        {
            return GetData<T>(table, "*", sortBy, null, conditionParams);
        }

        /// <summary>
        /// Wrapper for easier use of SendQuery function. 
        /// WARNING! I've tried for it to be safe, but it's probably not. Use with caution!
        /// </summary>
        /// <typeparam name="T">Type of result struct, it MUST match with DB table struct</typeparam>
        /// <param name="table">Table to send query to</param>
        /// <param name="queryFilter">Filter for query, example: "*" for all, "(p1, p2)" for columns p1 and p2</param>
        /// <param name="fields">Fields to populate in the struct, leave null to auto populate</param>
        /// <returns>List of structs containing data from query</returns>
        public List<T> GetData<T>(string table, string queryFilter, string orderBy, List<PropertyInfo> fields, params SQLCondition[] conditionsParams) where T : new()
        {
            List<MySqlParameter> parameters = new List<MySqlParameter>();
            string str = "SELECT " + queryFilter + " FROM " + table;
            str = MakeQuerySafe(str);

            parameters = GetParameters(ref str, conditionsParams);

            if (orderBy.Length > 0)
            {
                str += " ORDER BY " + orderBy;
            }

            str += ";";

            MySqlCommand cmd = new MySqlCommand(str, con);

            for (int i = 0; i < parameters.Count; i++)
            {
                cmd.Parameters.Add(parameters[i]);
            }

            return SendQuery<T>(cmd, fields);
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
            if (cmd.Connection != con)
            {
                throw new Exception("Unauthorized SQL query, bad connection");
            }

            List<T> ret = new List<T>();
            //MySqlCommand cmd = new MySqlCommand(query, con);
            lock (conLock)
            {
                MySqlDataReader rdr = cmd.ExecuteReader();

                if (fields == null)
                    fields = GetProperties<T>(rdr);

                while (rdr.Read())
                {
                    object[] values = new object[rdr.FieldCount];
                    // Get row
                    rdr.GetValues(values);

                    if (values.Length != fields.Count)
                    {
                        throw new Exception("Struct fields don't match SQL table fields");
                    }

                    T newT = new T();
                    for (int i = 0; i < fields.Count; i++)
                    {
                        if (values[i].GetType() == typeof(DBNull))
                            fields[i].SetValue(newT, null);
                        else
                            fields[i].SetValue(newT, values[i]);
                    }

                    ret.Add(newT);
                }
                rdr.Close();
                return ret;
            }
        }

        public void SendQuery(string query)
        {
            MySqlCommand cmd = new MySqlCommand(query, con);
            lock (conLock)
            {
                cmd.ExecuteNonQuery();
            }
        }

        bool CheckTableExists(string name)
        {
            string db = con.Database;

            MySqlCommand cmd = new MySqlCommand($"SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_SCHEMA = '{db}' AND TABLE_NAME = '{name}'; ", con);
            bool good = false;
            lock (conLock)
            {
                MySqlDataReader rdr = cmd.ExecuteReader();

                // If true it means that we have an return - which means we have anything
                good = rdr.Read();

                rdr.Close();
            }

            return good;
        }

        string GetDbTypeName(PropertyInfo info)
        {
            if(info.PropertyType == typeof(string))
            {
                var attr = info.GetCustomAttribute<SQLSizeAttribute>();
                if (attr != null)
                    return "VARCHAR(" + attr.size.ToString() + ")";
                else
                    return "TEXT";
            }
            if(info.PropertyType == typeof(byte[]))
            {
                var attr = info.GetCustomAttribute<SQLSizeAttribute>();
                if (attr == null)
                    throw new Exception("Size attribute is required for byte arrays");
                else
                    return $"VARBINARY({attr.size})";
            }
            if(info.PropertyType == typeof(int) || info.PropertyType.IsEnum)
                return "INT";

            if (info.PropertyType == typeof(long))
                return "BIGINT";

            if (info.PropertyType == typeof(float))
                return "FLOAT";

            if (info.PropertyType == typeof(double))
                return "DOUBLE";

            if(info.PropertyType == typeof(TimeSpan))
                return "TIME";


            if (info.PropertyType.IsArray)
                throw new Exception("Arrays other than byte[] are not yet supported");

            return info.PropertyType.Name.ToUpper();
        }

        public string GetPrimaryKey(string table)
        {
            string cmdText = $"SHOW KEYS FROM `{table}` WHERE Key_name = 'PRIMARY'";
            MySqlCommand cmd = new MySqlCommand(cmdText, con);

            var rdr = cmd.ExecuteReader();

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
                    string cText = $"CREATE TABLE `{con.Database}`.`{tableName}` (";
                    for (int j = 0; j < properties.Count;j++)
                    {
                        if (properties[j].GetCustomAttribute<SQLPrimaryAttribute>() != null)
                            primary = properties[j].Name;

                        cText += "`" + properties[j].Name + "` " + GetDbTypeName(properties[j]);
                        if (j != properties.Count - 1)
                            cText += ", ";
                    }

                    cText += ");";

                    MySqlCommand c = new MySqlCommand(cText, con);
                    c.ExecuteNonQuery();
                    
                    if(primary != "")
                    {
                        c = new MySqlCommand($"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primary}`);", con);
                        c.ExecuteNonQuery();
                    }

                    continue;
                }

                MySqlCommand cmd = new MySqlCommand($"SELECT * FROM {tableName};", con);
                var rdr = cmd.ExecuteReader();
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
                        if(IsTypeEqual(col.DataType, prop.PropertyType) &&
                            col.ColumnName == prop.Name)
                        {
                            found = true;
                            break;
                        }
                    }

                    if(!found)
                    {
                        Log.Normal($"\t\tAdding column `{prop.Name}` to `{tableName}`...");
                        MySqlCommand c = new MySqlCommand($"ALTER TABLE `{tableName}` ADD `{prop.Name}` {GetDbTypeName(prop)};", con);
                        c.ExecuteNonQuery();
                    }
                }

                var tablePrimary = GetPrimaryKey(tableName);
                if(tablePrimary != primaryKey)
                {
                    if(tablePrimary != "")
                        new MySqlCommand($"ALTER TABLE `{tableName}` DROP PRIMARY KEY;", con).ExecuteNonQuery();

                    if (primaryKey != "")
                        new MySqlCommand($"ALTER TABLE `{tableName}` ADD PRIMARY KEY (`{primaryKey}`)", con).ExecuteNonQuery();
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
                    con = new MySqlConnection(connectionString);
                    con.Open();
                }
                catch(MySqlException e)
                {
                    Log.FatalError("Cannot connect to database: \n" + e.ToString());
                    throw new Exception("Cannot connect to database");
                }
            }

            return IsAlive;
        }

        public bool Connect(string ip, int port, string username, string password, string dbName)
        {
            return Connect("server=" + ip + ";user=" + username + ";database=" + dbName + ";port=" + port.ToString() + ";password=" + password);
        }
    }
}