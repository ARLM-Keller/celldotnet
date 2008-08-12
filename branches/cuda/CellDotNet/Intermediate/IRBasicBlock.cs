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

namespace CellDotNet.Intermediate
{
	/// <summary>
	/// A basic block for trees.
	/// </summary>
	internal class IRBasicBlock
	{
		private IRBasicBlock _next;

		public IRBasicBlock Next
		{
			get { return _next; }
			set { _next = value; }
		}

		private int _blockNumber;

		/// <summary>
		/// A simple sequence number without meaning.
		/// </summary>
		public int BlockNumber
		{
			get { return _blockNumber; }
		}

		public IRBasicBlock(int blockNumber)
		{
			_blockNumber = blockNumber;
		}

		private readonly List<TreeInstruction> _roots = new List<TreeInstruction>();

		/// <summary>
		/// Roots of the tree representation.
		/// </summary>
		public List<TreeInstruction> Roots
		{
			get { return _roots; }
		}


		private readonly HashSet<IRBasicBlock> _ingoing = new HashSet<IRBasicBlock>();

		/// <summary>
		/// Ingoing basic blocks.
		/// </summary>
		public HashSet<IRBasicBlock> Ingoing
		{
			get { return _ingoing; }
		}


		private readonly HashSet<IRBasicBlock> _outgoing = new HashSet<IRBasicBlock>();

		/// <summary>
		/// Outgoing basic blocks.
		/// </summary>
		public HashSet<IRBasicBlock> Outgoing
		{
			get { return _outgoing; }
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

		/// <summary>
		/// Applies <paramref name="converter"/> to each node in the tree; when the converter return non-null, the current
		/// node is replaced with the return value.
		/// </summary>
		/// <param name="blocks"></param>
		/// <param name="converter"></param>
		static public void ConvertTreeInstructions(IEnumerable<IRBasicBlock> blocks, Converter<TreeInstruction, TreeInstruction> converter)
		{
			foreach (IRBasicBlock block in blocks)
			{
				for (int r = 0; r < block.Roots.Count; r++)
				{
					TreeInstruction root = block.Roots[r];
					TreeInstruction newRoot = TreeInstruction.ForeachTreeInstruction(root, converter);
					if (newRoot != null)
					{
						block.Roots[r] = newRoot;
					}
				}
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
	}
}