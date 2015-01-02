using System;
using System.Collections.Generic;
using System.Text;
using Wintellect.PowerCollections;

namespace FreecellSolver
{

	public class PriorityQueue<T>: ICollection<T>
	{
		private OrderedBag<T> _items;

		public T[] Items
		{
			get { return _items.ToArray(); }
		}

		public PriorityQueue(): 
			this(Comparer<T>.Default)
		{
		}

		public PriorityQueue(IComparer<T> comparer)
		{
			_items = new OrderedBag<T>(comparer);
		}

		public PriorityQueue(Comparison<T> comparison)
		{
			_items = new OrderedBag<T>(comparison);
		}


		public void Enqueue(T item)
		{
			_items.Add(item);
		}

		public T Dequeue()
		{
			return _items.RemoveFirst();
		}

		public void Add(T item)
		{
			_items.Add(item);
		}

		public void Clear()
		{
			_items.Clear();
		}

		public bool Contains(T item)
		{
			return _items.Contains(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			_items.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get { return _items.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			return _items.Remove(item);
		}

		public long RemoveAll(Predicate<T> predicate)
		{
			long count = _items.Count;
			_items.RemoveAll(predicate);

			return count - _items.Count;
		}

		public IEnumerator<T> GetEnumerator()
		{
			return _items.GetEnumerator();
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

	}
}
