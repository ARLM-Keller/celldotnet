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
		public virtual IEnumerable<TreeInstruction> GetChildInstructions()
		{
			if (Left != null)
				yield return Left;
			if (Right != null)
				yield return Right;
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

		private StackTypeDescription _stackTyp;
		public StackTypeDescription StackType
		{
			get { return _stackTyp; }
			set { _stackTyp = value; }
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
	}
}
