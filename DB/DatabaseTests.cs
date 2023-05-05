using CSUtil.Reflection;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.DB
{
    public class DatabaseTests
    {
        [SqlTable(TABLE_NAME)]
        public class TestClass : IEquatable<TestClass>
        {
            public const string TABLE_NAME = "test_class";

            [SQLPrimary]
            [SQLSize(36)]
            public string Id { get; set; } = "";
            public string Name { get; set; } = "";
            public int? Number { get; set; } = null;
            [SQLSize(6)]
            public DateTime? Date { get; set; } = DateTime.Now;

            [SQLIgnore]
            public List<int> Tests { get; set; } = new List<int>();

            static int datesCounter = 0;
            public static TestClass Random()
            {
                var r = new Random();
                return new TestClass()
                {
                    Id = Guid.NewGuid().ToString(),
                    Name = Guid.NewGuid().ToString(),
                    Number = r.Next(),
                    Date = DateTime.UnixEpoch.AddDays(datesCounter++),
                    Tests = new List<int>() { r.Next(), r.Next(), r.Next() }
                };
            }

            public bool Equals(TestClass? other) => other != null
                && Id == other.Id
                && Name == other.Name
                && Number == other.Number
                && Date == other.Date
                && !Tests.Except(other.Tests).Any();
        }

        Database db;

        [SetUp]
        public void Setup()
        {
            db = new Database();
            string port = "3306";
            if(Environment.GetEnvironmentVariable("CSUTIL_TEST_DB_PORT_OVERRIDE") is string portOverride)
                port = portOverride;

            Assert.That(db.Connect("csutil_test", "csutil_test", "csutil_test", port: port), Is.True);
            try
            {
                DatabaseManager.DropStructure(db, Assembly.GetAssembly(typeof(TestClass)));
            }
            catch (Exception) { }
            
            DatabaseManager.CreateStructure(db, Assembly.GetAssembly(typeof(TestClass)));
        }

        [TearDown]
        public void Teardown()
        {
            DatabaseManager.DropStructure(db, Assembly.GetAssembly(typeof(TestClass)));
        }


        public void CheckReturnData(List<TestClass> data, List<TestClass> dbData)
        {
            var cpy = ClassCopier.CreateList<TestClass, TestClass>(data);

            // Ignore string and Tests are not copied because it's not a property
            Assert.That(dbData, Is.Not.EquivalentTo(cpy));

            // After setting ignore string and Tests to default, it should be equivalent
            foreach (var d in cpy)
                d.Tests.Clear();
            
            Assert.That(dbData, Is.EquivalentTo(cpy));
        }

        public List<TestClass> InsertArrayDB(int size)
        {
            var data = new List<TestClass>(size);
            for (var i = 0; i < size; i++)
                data.Add(TestClass.Random());
            
            db.InsertArray(data.ToArray(), TestClass.TABLE_NAME);

            var dbData = db.GetData<TestClass>(TestClass.TABLE_NAME);

            CheckReturnData(data, dbData);

            return dbData;
        }

        [Test]
        public void InsertData()
        {
            const int TEST_SIZE = 500;

            var data = new List<TestClass>(TEST_SIZE);
            for (var i = 0; i < TEST_SIZE; i++)
            {
                var tc = TestClass.Random();
                data.Add(tc);
                db.InsertData(tc, TestClass.TABLE_NAME);
            }

            var dbData = db.GetData<TestClass>(TestClass.TABLE_NAME);
            CheckReturnData(data, dbData);
        }

        [Test]
        public void InsertArray() => InsertArrayDB(500);

        [Test]
        public void UpdateData()
        {
            var dbData = InsertArrayDB(500);
            Assert.That(dbData.Count, Is.EqualTo(500));

            var newData = TestClass.Random();
            db.Update(newData, TestClass.TABLE_NAME, nameof(TestClass.Id).SQLp(dbData[0].Id));

            var nList = db.GetData<TestClass>(TestClass.TABLE_NAME);
            var newDataInList = nList.FirstOrDefault(x => x.Id == newData.Id);
            Assert.That(newDataInList, Is.Not.Null);
            Assert.That(newData, Is.EqualTo(newDataInList));
        }

        [Test]
        public void CountTest()
        {
            const int TEST_SIZE = 500;

            var dbData = InsertArrayDB(TEST_SIZE);
            Assert.That(dbData.Count, Is.EqualTo(TEST_SIZE));

            var count = db.Count(TestClass.TABLE_NAME);
            var countWhere = db.Count(TestClass.TABLE_NAME, nameof(TestClass.Id).SQLp(dbData[0].Id));

            Assert.That(count, Is.EqualTo(TEST_SIZE));
            Assert.That(countWhere, Is.EqualTo(1));
        }

        [Test]
        public void DeleteTest()
        {
            const int TEST_SIZE = 500;

            var dbData = InsertArrayDB(TEST_SIZE);
            Assert.That(dbData.Count, Is.EqualTo(TEST_SIZE));

            db.Delete(TestClass.TABLE_NAME, nameof(TestClass.Id).SQLp(dbData[0].Id));
            Assert.That(db.GetData<TestClass>(TestClass.TABLE_NAME).Count, Is.EqualTo(TEST_SIZE - 1));

            db.Delete(TestClass.TABLE_NAME);
            Assert.That(db.GetData<TestClass>(TestClass.TABLE_NAME).Count, Is.EqualTo(0));
        }

        [Test]
        public void ConditionTypes()
        {
            const int TEST_SIZE = 500;
            var data = InsertArrayDB(TEST_SIZE);
            Assert.That(data.Count, Is.EqualTo(TEST_SIZE));

            // Like
            var like = data.Where(x => x.Name.StartsWith("4"));
            var dbLike = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Name).SQLp("4%", Database.SQLCondition.ConditionTypes.Like));
            Assert.That(like, Is.EquivalentTo(dbLike));

            // Not like
            var notLike = data.Where(x => !x.Name.StartsWith("4"));
            var dbNotLike = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Name).SQLp("4%", Database.SQLCondition.ConditionTypes.NotLike));
            Assert.That(notLike, Is.EquivalentTo(dbNotLike));
            Assert.That(dbLike.Count + dbNotLike.Count, Is.EqualTo(TEST_SIZE));

            // Greater than
            DateTime borderDate = DateTime.UnixEpoch.AddDays(30);
            var gt = data.Where(x => x.Date > borderDate);
            var gtDb = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Date).SQLp(borderDate, Database.SQLCondition.ConditionTypes.GreaterThan));
            Assert.That(gt, Is.EquivalentTo(gtDb));

            // Greater than or equal
            var gteq = data.Where(x => x.Date >= borderDate);
            var gteqDb = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Date).SQLp(borderDate, Database.SQLCondition.ConditionTypes.GreaterThanOrEqual));
            Assert.That(gteq, Is.EquivalentTo(gteqDb));

            // Less than
            var lt = data.Where(x => x.Date < borderDate);
            var ltDb = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Date).SQLp(borderDate, Database.SQLCondition.ConditionTypes.LessThan));
            Assert.That(lt, Is.EquivalentTo(ltDb));

            // Greater than or equal
            var lteq = data.Where(x => x.Date <= borderDate);
            var lteqDb = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Date).SQLp(borderDate, Database.SQLCondition.ConditionTypes.LessThanOrEqual));
            Assert.That(lteq, Is.EquivalentTo(lteqDb));

            // Null
            var nullInts = new List<TestClass>(TEST_SIZE);
            for(var i = 0; i < TEST_SIZE; i++)
            {
                var tc = TestClass.Random();
                tc.Number = null;
                nullInts.Add(tc);
            }
            db.InsertArray(nullInts.ToArray(), TestClass.TABLE_NAME);
            var nulledValues = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Number).SQLp("", Database.SQLCondition.ConditionTypes.IsNull));
            CheckReturnData(nullInts, nulledValues);

            // Not null
            var notNulledValues = db.GetData<TestClass>(TestClass.TABLE_NAME, nameof(TestClass.Number).SQLp("", Database.SQLCondition.ConditionTypes.IsNotNull));
            Assert.That(data, Is.EquivalentTo(notNulledValues));
        }
    }
}
