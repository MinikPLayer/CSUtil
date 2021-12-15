

namespace CSUtil.DB
{
    public static class DbUtils
    {
        public static Database.SQLCondition SQLp(this string str, object obj, Database.SQLCondition.ConditionTypes type = Database.SQLCondition.ConditionTypes.Equals, string junctionOp = Database.SQLCondition.J_AND)
        {
            return new Database.SQLCondition() { name = str, value = obj, junctionOp = junctionOp, type = type };
        }
    }
}
