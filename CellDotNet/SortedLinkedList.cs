using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	// p. 210 - 214 programming C#
	// Note/TODO This class dosn't fully implements IList and super interfaces.
	internal class SortedLinkedList<T> : IList<T>
	{
		private int count = 0;
		private IComparer<T> comparer;
		private Node<T> _head = null;
		private Node<T> _tail = null;

		public SortedLinkedList(IComparer<T> comparer)
		{
			this.comparer = comparer;
		}

		public T Head
		{
			get { return (_head != null) ? _head.Data : default(T); }
		}

		public T Tail
		{
			get { return _tail != null ? _tail.Data : default(T); }
		}

		public T RemoveHead()
		{
			T data = this[0];
			RemoveAt(0);
			return data;
		}

		public T RemoveTail()
		{
			T data = this[count - 1];
			RemoveAt(count - 1);
			return data;
		}

		public Node<T> getNodeAt(int index)
		{
			if (index >= count) return null;

			Node<T> n = null;

			if (index < count/2)
			{
				n = _head;
				for (int i = 1; i <= index; i++) n = n.Next;
			}
			else
			{
				n = _tail;
				for (int i = count - 2; i >= index; i--) n = n.Prev;
			}

			return n;
		}

		private void Remove(Node<T> node)
		{
			if (node.Prev != null && node.Next != null)
			{
				node.Prev.Next = node.Next;
				node.Next.Prev = node.Prev;
			}
			else if (node.Prev != null)
			{
				node.Prev.Next = null;
				_tail = node.Prev;
			}
			else if (node.Next != null)
			{
				node.Next.Prev = null;
				_head = node.Next;
			}
			else
			{
				_head = null;
				_tail = null;
			}
		}

		#region IList<T> Members

		public int IndexOf(T item)
		{
			throw new Exception("The method or operation is not implemented."); //TODO
		}

		public void Insert(int index, T item)
		{
			if (index > count) return;
			RemoveAt(index);
			Add(item);
		}

		public void RemoveAt(int index)
		{
			if (index >= count) return;

			Node<T> n = getNodeAt(index);

			if (n.Prev != null)
			{
				if (n.Next != null)
				{
					n.Prev.Next = n.Next;
					n.Next.Prev = n.Prev;
				}
				else
				{
					n.Prev.Next = null;
					_tail = n.Prev;
				}
			}
			else
			{
				if (n.Next != null)
				{
					n.Next.Prev = null;
					_head = n.Next;
				}
				else
				{
					_head = null;
					_tail = null;
				}
			}

			count--;
		}

		public T this[int index]
		{
			get
			{
				if (index >= count) return default(T);

				Node<T> n = getNodeAt(index);

				return n.Data;
			}
			set
			{
				RemoveAt(index);
				Add(value);
			}
		}

		#endregion

		#region ICollection<T> Members

		public void Add(T item)
		{
			if(count > 200)
				System.Console.Write("");

			if (_head == null)
			{
				_head = new Node<T>(item);
				_tail = _head;
			}
			else
			{
				Node<T> newNode = new Node<T>(item);
				_head = _head.Add(newNode, comparer);
				if (newNode.Next == null) _tail = newNode;
			}
			count++;
		}

		public void Clear()
		{
			count = 0;
			_head = null;
			_tail = null;
		}

		public bool Contains(T item)
		{
			throw new Exception("The method or operation is not implemented."); //TODO
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			throw new Exception("The method or operation is not implemented."); //TODO
		}

		public int Count
		{
			get { return count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public bool Remove(T item)
		{
			Node<T> node = _head;
			while (node != null)
			{
				if (node.Data.Equals(item))
				{
					Remove(node);
					count--;
					return true;
				}
				node = node.Next;
			}
			return false;
		}

		#endregion

		#region IEnumerable<T> Members

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			Node<T> curr = _head;
			while (curr != null)
			{
				yield return curr.Data;
				curr = curr.Next;
			}
			//			return new SortedListEnumerator(this);
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new SortedListEnumerator(this);
		}

		#endregion

		public override string ToString()
		{
			StringBuilder result = new StringBuilder();

			foreach (T t in this)
			{
				result.Append(t.ToString());
				result.Append(" ");
			}
			return result.ToString();
		}

		public class Node<S>
		{
			private S data;
			private Node<S> next;
			private Node<S> prev;

			public Node(S data)
			{
				this.data = data;
			}

			public S Data
			{
				get { return this.data; }
			}

			public Node<S> Next
			{
				get { return this.next; }
				set { this.next = value; }
			}

			public Node<S> Prev
			{
				get { return this.prev; }
				set { this.prev = value; }
			}

			public Node<S> Add(Node<S> newNode, IComparer<S> comparer)
			{
				if (comparer.Compare(data, newNode.data) > 0)
				{
					if (prev != null)
					{
						prev.next = newNode;
						newNode.prev = this.prev;
					}

					newNode.next = this;
					this.prev = newNode;

					return newNode;
				}
				else
				{
					if (this.next != null)
					{
						this.next.Add(newNode, comparer);
					}
					else
					{
						this.next = newNode;
						newNode.prev = this;
					}

					return this;
				}
			}
		}

		public class SortedListEnumerator : IEnumerator<T>
		{
			private SortedLinkedList<T> list;

			private Node<T> currentNode = null;

			private bool startOfList = true;

			public SortedListEnumerator(SortedLinkedList<T> list)
			{
				this.list = list;
			}

			T IEnumerator<T>.Current
			{
				get
				{
					if (currentNode == null)
						throw new InvalidOperationException();

					return currentNode.Data;
				}
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				if (currentNode == null)
				{
					if (startOfList)
						currentNode = list._head;
				}
				else
				{
					currentNode = currentNode.Next;
				}
				startOfList = false;

				return currentNode != null;
			}

			public void Reset()
			{
				startOfList = true;
				currentNode = list._head;
			}

			public object Current
			{
				get
				{
					if (currentNode == null)
						throw new InvalidOperationException();

					return currentNode.Data;
				}
			}
		}
	}
}