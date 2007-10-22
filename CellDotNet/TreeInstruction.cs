// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// Represents an instruction in an instruction tree.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
	class TreeInstruction
	{
		public TreeInstruction(IROpCode opcode)
		{
			_opcode = opcode;
		}

		public TreeInstruction(IROpCode opcode, StackTypeDescription stackType, object operand, int offset)
		{
			_opcode = opcode;
			_operand = operand;
			_offset = offset;
			_stackType = stackType;
		}

		public TreeInstruction(IROpCode opcode, StackTypeDescription stackType, object operand, int offset, TreeInstruction left)
		{
			_left = left;
			_opcode = opcode;
			_operand = operand;
			_offset = offset;
			_stackType = stackType;
		}

		public TreeInstruction(IROpCode opcode, StackTypeDescription stackType, object operand, int offset, TreeInstruction left, TreeInstruction right)
		{
			_left = left;
			_opcode = opcode;
			_operand = operand;
			_offset = offset;
			_stackType = stackType;
			_right = right;
		}

		private TreeInstruction _left;
		public TreeInstruction Left
		{
			get { return _left; }
			set { _left = value; }
		}

		/// <summary>
		/// Returns left and right if they are non-null.
		/// <para>Overridden implementations may return more - as does <see cref="MethodCallInstruction"/>.</para>
		/// </summary>
		/// <returns></returns>
		public virtual TreeInstruction[] GetChildInstructions()
		{
			Utilities.PretendVariableIsUsed(DebuggerDisplay);
			Utilities.PretendVariableIsUsed(SubTreeText);

			if (Right != null)
				return new TreeInstruction[] {Left, Right};
			else if (Left != null)
				return new TreeInstruction[] {Left};
			else
				return new TreeInstruction[0];
		}

		/// <summary>
		/// Replaces the specified child with <paramref name="newchild"/>.
		/// </summary>
		/// <param name="childIndex"></param>
		/// <param name="newchild"></param>
		public virtual void ReplaceChild(int childIndex, TreeInstruction newchild)
		{
			switch (childIndex)
			{
				case 0:
					Utilities.AssertNotNull(Left, "Left");
					Left = newchild;
					break;
				case 1:
					Utilities.AssertNotNull(Left, "Right");
					Right = newchild;
					break;
				default:
					throw new ArgumentOutOfRangeException("childIndex");
			}
		}

		private string DebuggerDisplay
		{
			get
			{
				if (Operand is string || Operand is int)
				{
					return "" + Opcode.IRCode + " " + Operand;
				}
				else if (Operand is LocalVariableInfo)
				{
					LocalVariableInfo r = (LocalVariableInfo)Operand;
					return string.Format("{0} V_{1} ({2})", Opcode, r.LocalIndex, r.LocalType.Name);
				}
				else if (Operand is FieldInfo)
				{
					FieldInfo f = (FieldInfo) Operand;
					return string.Format("{0} {1} ({2})", Opcode, f.Name, f.FieldType.Name);
				}
				else
					return Opcode.IRCode.ToString();
			}
		}

		public string DebuggerTreeDisplay()
		{
			String leftString = (_left != null)? _left.DebuggerTreeDisplay() : "";
			String rightString = (_right != null) ? _right.DebuggerTreeDisplay() : "";

			if(_left == null && _right == null)
				return string.Format("{0} {1}", DebuggerDisplay, _offset);
			else
				return string.Format("{0} {1} [{2}, {3}]", DebuggerDisplay, _offset, leftString, rightString);
		}

		private string SubTreeText
		{
			get { return new TreeDrawer().DrawSubTree(this); }
		}

		private TreeInstruction _right;
		public TreeInstruction Right
		{
			get { return _right; }
			set { _right = value; }
		}

		private IROpCode _opcode;
		public IROpCode Opcode
		{
			get { return _opcode; }
			set { _opcode = value; }
		}

		#region Operand

		private object _operand;
		public object Operand
		{
			get { return _operand; }
			set { _operand = value; }
		}

		public MethodBase OperandAsMethod
		{
			get { return _operand as MethodBase; }
		}

		public MethodCompiler OperandAsMethodCompiler
		{
			get { return _operand as MethodCompiler; }
		}

		public PpeMethod OperandAsPpeMethod
		{
			get { return _operand as PpeMethod; }
		}

		public IRBasicBlock OperandAsBasicBlock
		{
			get {return _operand as IRBasicBlock; }
		}

		public int OperandAsInt32
		{
			get { return (int) _operand; }
		}

		public MethodVariable OperandAsVariable
		{
			get { return _operand as MethodVariable; }
		}

		public FieldInfo OperandAsField
		{
			get { return _operand as FieldInfo; }
		}

		#endregion

		private int _offset = -1;
		/// <summary>
		/// Offset in the IL stream, where applicable; otherwise -1. Used for branching.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
			set { _offset = value; }
		}

		private StackTypeDescription _stackType;
		public StackTypeDescription StackType
		{
			get { return _stackType; }
			set { _stackType = value; }
		}

		#region Tree iteration / checking.

		/// <summary>
		/// Applies <paramref name="converter"/> to each node in the tree; when the converter return non-null, the current
		/// node is replaced with the return value.
		/// </summary>
		/// <param name="root"></param>
		/// <param name="converter"></param>
		/// <returns></returns>
		public static TreeInstruction ForeachTreeInstruction(TreeInstruction root, Converter<TreeInstruction, TreeInstruction> converter)
		{
			if (root == null)
				return null;

			TreeInstruction newRoot = null;

			Stack<TreeInstruction> parentlist = new Stack<TreeInstruction>();
			Stack<int> childIndexList = new Stack<int>();

			TreeInstruction parent = null;
			int childIndex = 0;

			TreeInstruction inst = root;

			do
			{
				TreeInstruction newInst = converter(inst);

				if (newInst != null)
				{
					inst = newInst;
					if (parent != null)
						parent.ReplaceChild(childIndex, newInst);
					else
						newRoot = newInst;
				}

				// Go to the nest instruction.
				if (inst.GetChildInstructions().Length > 0)
				{
					parentlist.Push(parent);
					childIndexList.Push(childIndex);

					parent = inst;
					childIndex = 0;

					inst = inst.GetChildInstructions()[0];
				}
				else if (parent != null && childIndex + 1 < parent.GetChildInstructions().Length)
				{
					inst = parent.GetChildInstructions()[++childIndex];
				}
				else if (parent != null)
				{
					while(parent != null && childIndex + 1 >= parent.GetChildInstructions().Length)
					{
						parent = parentlist.Pop();
						childIndex = childIndexList.Pop();
					}

					if(parent != null)
						inst = parent.GetChildInstructions()[++childIndex];

					//					parrent = parrentlist.Peek();
					//					chieldIndex = chieldIndexList.Peek();
				}
			} while (parent != null);
			return newRoot;
		}

		/// <summary>
		/// For checking tree construction.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TreeInstruction> IterateSubtree()
		{
			List<TreeInstruction> list = new List<TreeInstruction>();
			BuildPreorder(list);
			return list;
		}

		/// <summary>
		/// For checking tree construction.
		/// </summary>
		/// <param name="list"></param>
		internal virtual void BuildPreorder(List<TreeInstruction> list)
		{
			list.Add(this);
			if (Left != null)
				Left.BuildPreorder(list);
			if (Right != null)
				Right.BuildPreorder(list);
		}

		#endregion

		/// <summary>
		/// Returns the first instruction in the tree; that is, the instruction in the far left side.
		/// </summary>
		/// <returns></returns>
		public TreeInstruction GetFirstInstruction()
		{
			TreeInstruction parent = this;
			TreeInstruction child = null;

			do
			{
				if (child != null)
					parent = child;

				Utilities.TryGetFirst(parent.GetChildInstructions(), out child);
			} while (child != null);

			return parent;
		}

		/// <summary>
		/// Returns the first instruction in the tree that has an address.
		/// Instructions that were not in the IL do not have an offset and therefore
		/// cannot be targets for branches.
		/// </summary>
		/// <returns></returns>
		public TreeInstruction GetFirstInstructionWithOffset()
		{
			TreeInstruction parent = this;
			TreeInstruction child = null;

			do
			{
				if (child != null)
					parent = child;

				Utilities.TryGetFirst(parent.GetChildInstructions(), out child);
			} while (child != null && child.Offset >= 0);

//			if (parent.Offset < 0)
//				throw new ILSemanticErrorException("Can't find first instruction with offset.");

			return parent;
		}
	}
}
