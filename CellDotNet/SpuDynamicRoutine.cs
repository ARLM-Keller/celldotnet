using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CellDotNet
{
	/// <summary>
	/// Represents anything that can be emitted as SPU code.
	/// </summary>
	abstract class SpuDynamicRoutine : SpuRoutine
	{
		protected SpuDynamicRoutine()
		{
		}

		protected SpuDynamicRoutine(string name) : base(name)
		{
		}


		public override ReadOnlyCollection<MethodParameter> Parameters
		{
			get { throw new NotImplementedException(); }
		}

		public override StackTypeDescription ReturnType
		{
			get { throw new NotImplementedException(); }
		}

		/// <summary>
		/// Implementations of this method should return the emitted binary code.
		/// </summary>
		/// <returns></returns>
		public abstract int[] Emit();

		/// <summary>
		/// Override this one in order to have addresses patched.
		/// <para>
		/// See <see cref="MethodCompiler"/> for an example.
		/// </para>
		/// </summary>
		public abstract void PerformAddressPatching();


		/// <summary>
		/// Returns an enumerator for the routine, if possible.
		/// <para>
		/// This is supposed to be used for disassembly, so it should only be called once
		/// the routine is done and has been patched.
		/// </para>
		/// </summary>
		/// <returns></returns>
		/// <exception cref="InvalidOperationException">If the routine does not support this operation.</exception>
		public virtual IEnumerable<SpuInstruction> GetFinalInstructions()
		{
			throw new InvalidOperationException();
		}

		public virtual IEnumerable<SpuInstruction> GetInstructions()
		{
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Replaces <see cref="SpuBasicBlock"/> and <see cref="ObjectWithAddress"/> operands 
		/// stored within the instructions with their numeric offsets.
		/// </summary>
		/// <param name="bblist">
		/// The entire list of basic blocks for the method/routine. It MUST contain
		/// all basic blocks, including the outer prolog and epilog; otherwise the offsets 
		/// will not be calculated correctly.
		/// </param>
		/// <param name="epilogStart">
		/// The first basic block of the epilog. 
		/// This one is also contained in the <paramref name="bblist"/> list.
		/// </param>
		protected void PerformAddressPatching(List<SpuBasicBlock> bblist, SpuBasicBlock epilogStart)
		{
			// All offsets are byte offset from start of method; 
			// that is, from the ObjectWithAddress.

			List<KeyValuePair<int, SpuInstruction>> branchlist = new List<KeyValuePair<int, SpuInstruction>>();
			int curroffset = 0;
			foreach (SpuBasicBlock bb in bblist)
			{
				bb.Offset = curroffset;
				if (bb.Head == null)
					continue;

				foreach (SpuInstruction inst in bb.Head.GetEnumerable())
				{
					if (inst.JumpTarget != null)
						branchlist.Add(new KeyValuePair<int, SpuInstruction>(curroffset, inst));
					else if (inst.OpCode == SpuOpCode.ret)
					{
						inst.OpCode = SpuOpCode.br;
						if (epilogStart == null)
						{
							// Make sure that we've got an epilog bb to branch to.
							throw new ArgumentException("epilogStart is null, but a ret opcode was encountered.");
						}
						inst.JumpTarget = epilogStart;
						branchlist.Add(new KeyValuePair<int, SpuInstruction>(curroffset, inst));
					}
					else if (inst.ObjectWithAddress != null)
					{
						Utilities.Assert(inst.ObjectWithAddress.Offset > 0, "Bad ObjectWithAddress offset: " + inst.ObjectWithAddress.Offset + ". Type: " + inst.ObjectWithAddress.GetType().Name);

						int diff = inst.ObjectWithAddress.Offset - (Offset + curroffset);

						Utilities.Assert(diff % 4 == 0, "branch offset not multiple of four bytes: " + diff);
						Utilities.Assert(inst.OpCode != SpuOpCode.brsl || (diff < 1024*127 || diff > -1024*127), "Branch offset for brsl is not whitin bounds " + -1024*127 + " and " + 1024*127 + ": " + diff);

						inst.Constant = diff >> 2; // instructions and therefore branch offsets are 4-byte aligned and the ISA uses that fact.
					}

					curroffset += 4;
				}
			}

			// Insert offsets.
			foreach (KeyValuePair<int, SpuInstruction> branchpair in branchlist)
			{
				SpuBasicBlock targetbb = branchpair.Value.JumpTarget;

				int relativebranchbytes = targetbb.Offset - branchpair.Key;
				// Branch offset operands don't use the last two bytes, since all
				// instructions are 4-byte aligned.
				branchpair.Value.Constant = relativebranchbytes >> 2;
			}
		}
	}
}
