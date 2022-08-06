

using System;
using CSUtil.Crypto;
using CSUtil.Logging;

namespace CSUtil.DB
{
    public static class DbUtils
    {
        public static Database.SQLCondition SQLp(this string str, object obj, Database.SQLCondition.ConditionTypes type = Database.SQLCondition.ConditionTypes.Equals, string junctionOp = Database.SQLCondition.J_AND)
        {
            return new Database.SQLCondition() { name = str, value = obj, junctionOp = junctionOp, type = type };
        }

        public static int GenerateUniqueId(this Database db, string table, string column, int tries = 10000)
        {
            for (int i = 0; i < tries; i++)
            {
                int random = Random.Shared.Next();
                var exist = db.Count(table, column.SQLp(random));
                if(exist == 0)
                    return random;
            }
            
            // Cannot generate unique id in tries
            Log.Error("Cannot generate unique id in " + tries + " times");
            throw new OverflowException("Cannot generate unique id");
        }

        public static string GenerateUniqueIdString(this Database db, string table, string column, int tries = 10000)
        {
            for (int i = 0; i < tries; i++)
            {
                string random = Password.GenerateToken();
                var exist = db.Count(table, column.SQLp(random));
                if(exist == 0)
                    return random;
            }
            
            // Cannot generate unique id in tries
            Log.Error("Cannot generate unique id in " + tries + " times");
            throw new OverflowException("Cannot generate unique id");
        }
    }
}
