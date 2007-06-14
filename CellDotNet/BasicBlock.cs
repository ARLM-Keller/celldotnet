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
	internal class BasicBlock
	{
		private BasicBlock _next;

		public BasicBlock Next
		{
			get { return _next; }
			set { _next = value; }
		}

		private List<TreeInstruction> _roots = new List<TreeInstruction>();

		/// <summary>
		/// Roots of the tree representation.
		/// </summary>
		public IList<TreeInstruction> Roots
		{
			get { return _roots; }
		}


		private Set<BasicBlock> _ingoing = new Set<BasicBlock>();

		/// <summary>
		/// Ingoing basic blocks.
		/// </summary>
		public Set<BasicBlock> Ingoing
		{
			get { return _ingoing; }
		}


		private Set<BasicBlock> _outgoing = new Set<BasicBlock>();

		/// <summary>
		/// Outgoing basic blocks.
		/// </summary>
		public Set<BasicBlock> Outgoing
		{
			get { return _outgoing; }
		}

		public int Offset
		{
			get 
			{
				return _roots[0].FirstOffset;
			}
		}
	}
}