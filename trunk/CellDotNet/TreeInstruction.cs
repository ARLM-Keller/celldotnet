using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// Represents an instruction in an instruction tree.
	/// </summary>
	class TreeInstruction
	{
		private TreeInstruction _left;
		public TreeInstruction Left
		{
			get { return _left; }
			set { _left = value; }
		}

		private TreeInstruction _right;
		public TreeInstruction Right
		{
			get { return _right; }
			set { _right = value; }
		}

		private OpCode _opcode;
		public OpCode Opcode
		{
			get { return _opcode; }
			set { _opcode = value; }
		}


		private object _operand;
		public object Operand
		{
			get { return _operand; }
			set { _operand = value; }
		}

		private int _offset = -1;
		/// <summary>
		/// Offset in the IL stream, where applicable; otherwise -1. Used for branching.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

/*
		/// <summary>
		/// Does not work!!
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TreeInstruction> IterateInorder()
		{
			Stack<TreeInstruction> s = new Stack<TreeInstruction>();
			TreeInstruction ptr = this;

			do
			{
				while (ptr != null)
				{
					s.Push(ptr);
					ptr = ptr.Left;
				}

				ptr = s.Pop();

//				action(ptr);
				yield return ptr;

				ptr = ptr.Right;

			} while (s.Count != 0);
		}
*/

		public IEnumerable<TreeInstruction> IterateInorder()
		{
			List<TreeInstruction> list = new List<TreeInstruction>();
			BuildPreorder(list);
			return list;
		}

		private void BuildPreorder(List<TreeInstruction> list)
		{
			list.Add(this);
			if (Left != null)
				Left.BuildPreorder(list);
			if (Right != null)
				Right.BuildPreorder(list);
		}

		public int TreeSize
		{
			get
			{
				int i = 0;
				foreach (TreeInstruction instruction in IterateInorder())
				{
					i++;
				}
				return i;
			}
		}
	}
}
