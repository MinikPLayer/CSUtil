using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
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

        public IEnumerator<T> GetEnumerator() => list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();

        void ForceReorder()
        {
            list = new LinkedList<T>(Inverted ? list.OrderByDescending(x => x) : list.OrderBy(x => x));
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

        [Test]
        public void InvertTest()
        {
            const int size = 10_000;

            var randomList = new List<int>(size);
            var sortedList = new SortedLinkedList<int>();
            sortedList.Inverted = false;

            for (var i = 0; i < size; i++)
            {
                var rnd = Random.Shared.Next();
                randomList.Add(rnd);
                sortedList.Add(rnd);

                if (i == size / 2)
                    sortedList.Inverted = true;
            }

            var orderedList = randomList.OrderByDescending(x => x);
            var finalList = sortedList.ToList();
            Assert.That(finalList, Is.EqualTo(orderedList));
        }
    }
}
