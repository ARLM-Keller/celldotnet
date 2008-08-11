using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CellDotNet.Cuda
{
	class BasicBlock
	{
		public ListInstruction Head { get; private set; }
		public ListInstruction Tail { get; private set; }

		public IEnumerable<ListInstruction> Instructions
		{
			get
			{
				ListInstruction curr = Head;
				while (curr != null)
				{
					yield return curr;
					curr = curr.Next;
				}
			}
		}

//		public BasicBlock Next { get; set; }
//		public BasicBlock Previous { get; set; }

		//		public ICollection<ListInstruction> Heads { get; private set; }

		public void Append(ListInstruction newinst)
		{
			if (Tail != null)
				Tail.Next = newinst;
			Tail = newinst;

			if (Head == null)
				Head = newinst;

//			throw new NotImplementedException();
		}
	}
}
