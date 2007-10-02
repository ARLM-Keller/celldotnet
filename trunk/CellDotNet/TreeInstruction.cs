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

		/// <summary>
		/// For checking tree construction.
		/// </summary>
		public int TreeSize
		{
			get
			{
				int i = 0;
				foreach (TreeInstruction instruction in IterateSubtree())
				{
					i++;
					Utilities.PretendVariableIsUsed(instruction);
				}
				return i;
			}
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
