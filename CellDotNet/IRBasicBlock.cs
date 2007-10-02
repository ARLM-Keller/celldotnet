using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// An extended basic block.
	/// </summary>
	/// <remarks>
	/// From Mono basic block documentation:
	/// <para>
	///  A basic block can have multiple exits just fine, as long as the point of
	/// 'departure' is the last instruction in the basic block. Extended basic
	/// blocks, on the other hand, may have instructions that leave the block
	/// midstream. The important thing is that they cannot be _entered_
	/// midstream, ie, execution of a basic block (or extened bb) always start
	/// at the beginning of the block, never in the middle.
	/// </para>
	/// </remarks>
	internal class IRBasicBlock
	{
		private IRBasicBlock _next;

		public IRBasicBlock Next
		{
			get { return _next; }
			set { _next = value; }
		}

		private List<TreeInstruction> _roots = new List<TreeInstruction>();

		/// <summary>
		/// Roots of the tree representation.
		/// </summary>
		public List<TreeInstruction> Roots
		{
			get { return _roots; }
		}


		private Set<IRBasicBlock> _ingoing = new Set<IRBasicBlock>();

		/// <summary>
		/// Ingoing basic blocks.
		/// </summary>
		public Set<IRBasicBlock> Ingoing
		{
			get { return _ingoing; }
		}


		private Set<IRBasicBlock> _outgoing = new Set<IRBasicBlock>();

		/// <summary>
		/// Outgoing basic blocks.
		/// </summary>
		public Set<IRBasicBlock> Outgoing
		{
			get { return _outgoing; }
		}

		public int Offset
		{
			get 
			{
				return _roots[0].GetFirstInstructionWithOffset().Offset;
			}
		}

		public IEnumerable<TreeInstruction> EnumerateInstructions()
		{
			foreach (TreeInstruction root in Roots)
			{
				foreach (TreeInstruction inst in root.IterateSubtree())
					yield return inst;
			}
		}

		static public void ForeachTreeInstruction(IEnumerable<IRBasicBlock> blocks, Action<TreeInstruction> action)
		{
			foreach (IRBasicBlock block in blocks)
			{
				foreach (TreeInstruction inst in block.EnumerateInstructions())
					action(inst);
			}
		}

		public static IEnumerable<TreeInstruction> EnumerateTreeInstructions(List<IRBasicBlock> blocks)
		{
			foreach (IRBasicBlock block in blocks)
			{
				foreach (TreeInstruction inst in block.EnumerateInstructions())
					yield return inst;
			}
		}

		public String toString()
		{
			String result = "";
			foreach (TreeInstruction root in _roots)
			{
				result += string.Format("{0}\n", root.DebuggerTreeDisplay());
			}
			return result;
		}
	}
}