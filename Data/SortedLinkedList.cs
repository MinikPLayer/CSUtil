using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.Data
{
    public class SortedLinkedList<T> : IEnumerable<T>
        where T : IComparable<T>
    {
        protected LinkedList<T> list = new LinkedList<T>();

        protected bool inverted = false;
        public bool Inverted
        {
            get => inverted;
            set
            {
                if (value == inverted)
                    return;

                inverted = value;
                ForceReorder();
            }
        }

        public int Count => list.Count;

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        public void ForceReorder()
        {
            list = new LinkedList<T>(Inverted ? list.OrderByDescending(x => x) : list.OrderBy(x => x));
        }

        public void Clear() => list.Clear();

        public void AddRange(IEnumerable<T> items)
        {
            foreach (var o in items)
                list.AddFirst(o);

            ForceReorder();
        }

        public void Add(T item)
        {
            LinkedListNode<T>? lastNode = null;
            LinkedListNode<T>? currentNode = list.First;

            while (currentNode != null)
            {
                if (item.CompareTo(currentNode.Value) == (inverted ? 1 : -1))
                    break;

                lastNode = currentNode;
                currentNode = currentNode.Next;
            }

            if (lastNode == null)
            {
                list.AddFirst(item);
                return;
            }

            list.AddAfter(lastNode, item);
        }

        public SortedLinkedList() { }

        public SortedLinkedList(IEnumerable<T> data) => AddRange(data);
    }

    public class SortedLinkedListTests
    { 
        [Test]
        public void StaticTest()
        {
            var list = new SortedLinkedList<int>();
            list.Add(2);
            list.Add(1);
            list.Add(3);
            list.Add(7);

            var finalList = list.ToList();
            Assert.That(finalList, Is.EqualTo(new List<int> { 1, 2, 3, 7 }));
        }

        [Test]
        [TestCase(false)]
        [TestCase(true)]
        public void RandomTest(bool inverted)
        {
            const int size = 10_000;

            var randomList = new List<int>(size);
            var sortedList = new SortedLinkedList<int>();
            sortedList.Inverted = inverted;

            for (var i = 0; i < size; i++)
            {
                var rnd = Random.Shared.Next();
                randomList.Add(rnd);
                sortedList.Add(rnd);
            }

            var orderedList = (inverted ? randomList.OrderByDescending(x => x) : randomList.OrderBy(x => x)).ToList();
            var finalList = sortedList.ToList();
            Assert.That(finalList, Is.EqualTo(orderedList));
        }

        const int TEST_SIZE = 10_000;
        [Test]
        public void InvertTest()
        {
            var randomList = new List<int>(TEST_SIZE);
            var sortedList = new SortedLinkedList<int>();
            sortedList.Inverted = false;

            for (var i = 0; i < TEST_SIZE; i++)
            {
                var rnd = Random.Shared.Next();
                randomList.Add(rnd);
                sortedList.Add(rnd);

                if (i == TEST_SIZE / 2)
                    sortedList.Inverted = true;
            }

            var orderedList = randomList.OrderByDescending(x => x);
            var finalList = sortedList.ToList();
            Assert.That(finalList, Is.EqualTo(orderedList));
        }

        [Test]
        public void TestAddRange()
        {
            var randomList = new List<int>(TEST_SIZE);
            for (var i = 0; i < TEST_SIZE; i++)
                randomList.Add(i);

            var sortedList = new SortedLinkedList<int>();
            sortedList.AddRange(randomList);
            Assert.That(sortedList.Count, Is.EqualTo(TEST_SIZE));

            sortedList.AddRange(randomList);
            Assert.That(sortedList.Count, Is.EqualTo(TEST_SIZE * 2));

            var enumerator = sortedList.GetEnumerator();
            for(var i = 0; i < TEST_SIZE; i++)
            {
                enumerator.MoveNext();
                var i1 = enumerator.Current;
                enumerator.MoveNext();
                var i2 = enumerator.Current;

                Assert.That(i1, Is.EqualTo(i2));
            }
        }

        [Test]
        public void TimeTestNonInverted()
        {
            var list = new SortedLinkedList<int>();
            var time1 = DateTime.Now;
            for (var i = 0; i < TEST_SIZE; i++)
                list.Add(i);
            var time2 = DateTime.Now;

            var logBuilder = new StringBuilder();
            logBuilder.Append("Time for ");
            logBuilder.Append(TEST_SIZE);
            logBuilder.Append(" projects: ");
            logBuilder.Append((time2 - time1).TotalSeconds);
            logBuilder.Append("s");

            Console.WriteLine(logBuilder.ToString());
        }

        [Test]
        public void TimeTestInverted()
        {
            var list = new SortedLinkedList<int>();
            var time1 = DateTime.Now;
            for (var i = 0; i < TEST_SIZE; i++)
                list.Add(TEST_SIZE - i);
            var time2 = DateTime.Now;

            var logBuilder = new StringBuilder();
            logBuilder.Append("Time for ");
            logBuilder.Append(TEST_SIZE);
            logBuilder.Append(" projects: ");
            logBuilder.Append((time2 - time1).TotalSeconds);
            logBuilder.Append("s");

            Console.WriteLine(logBuilder.ToString());
        }

        [Test]
        public void TimeTestRandom()
        {
            var list = new SortedLinkedList<int>();
            var time1 = DateTime.Now;
            for (var i = 0; i < TEST_SIZE; i++)
                list.Add(Random.Shared.Next());

            var time2 = DateTime.Now;

            var logBuilder = new StringBuilder();
            logBuilder.Append("Time for ");
            logBuilder.Append(TEST_SIZE);
            logBuilder.Append(" projects: ");
            logBuilder.Append((time2 - time1).TotalSeconds);
            logBuilder.Append("s");

            Console.WriteLine(logBuilder.ToString());
        }

        [Test]
        public void TimeTestAddRangeNonInverted()
        {
            var list = new List<int>(TEST_SIZE);
            var sortedList = new SortedLinkedList<int>();
            var time1 = DateTime.Now;
            for (var i = 0; i < TEST_SIZE; i++)
                list.Add(i);

            sortedList.AddRange(list);
            var time2 = DateTime.Now;

            var logBuilder = new StringBuilder();
            logBuilder.Append("Time for ");
            logBuilder.Append(TEST_SIZE);
            logBuilder.Append(" projects: ");
            logBuilder.Append((time2 - time1).TotalSeconds);
            logBuilder.Append("s");

            Console.WriteLine(logBuilder.ToString());
        }
    }
}
