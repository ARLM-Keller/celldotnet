using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// An extended basic block.
	/// </summary>
	class BasicBlock
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
	}
}
