using System;
using System.Collections.Generic;
using System.IO;

namespace CellDotNet
{
	static internal class IterativeLivenessAnalyser
	{
		/// <summary>
		/// 
		/// </summary>
		/// <param name="basicBlocks"></param>
		/// <param name="liveIn"></param>
		/// <param name="liveOut"></param>
		/// <param name="includeUnusedDefsInLiveOut">
		/// If true, any virtual register that is defined but never used will be included in <paramref name="liveOut"/>.
		/// This is necessary for the linear register allocator, since it constructs its intervals from the live out information;
		/// and a virtual register that doesn't have an interval will not get a register.
		/// </param>
		public static void Analyze(List<SpuBasicBlock> basicBlocks, out Set<VirtualRegister>[] liveIn,
		                           out Set<VirtualRegister>[] liveOut, bool includeUnusedDefsInLiveOut)
		{
			// NOTE instruktions nummereringen starter fra 0.

			Dictionary<int, Set<int>> succ = new Dictionary<int, Set<int>>();

			Dictionary<SpuBasicBlock, SpuInstruction> blocks = new Dictionary<SpuBasicBlock, SpuInstruction>();
			Dictionary<SpuBasicBlock, List<SpuInstruction>> jumpSources = new Dictionary<SpuBasicBlock, List<SpuInstruction>>();

			List<SpuInstruction> instlist = new List<SpuInstruction>();

			foreach (SpuBasicBlock bb in basicBlocks)
				jumpSources[bb] = new List<SpuInstruction>();

			// Build predecessor and successor sets without taking jumps into account.
			SpuInstruction predecessor = null;
			int instNr = 0;
			foreach (SpuBasicBlock bb in basicBlocks)
			{
				// This could cause trouble with branches to empty blocks.
				if (bb.Head == null)
					continue;
				blocks.Add(bb, bb.Head);

				foreach (SpuInstruction inst in bb.Head.GetEnumerable())
				{
					inst.Index = instNr;
					instlist.Add(inst);

					succ.Add(inst.Index, new Set<int>());

					// Predecessor is not set to null at the beginning of each block.
					if (predecessor != null)
					{
						succ[instNr - 1].Add(instNr);
					}
					if (inst.JumpTarget != null)
						jumpSources[inst.JumpTarget].Add(inst);

					predecessor = inst;
					instNr++;
				}
			}

//			PrintSuccessors(instlist, succ);


			int numberOfInst = instlist.Count;

			// Add jumps to predecessor og successor info.
			foreach (KeyValuePair<SpuBasicBlock, SpuInstruction> pair in blocks)
			{
				SpuBasicBlock bb = pair.Key;
				SpuInstruction firstinst = pair.Value;

				foreach (SpuInstruction js in jumpSources[bb])
				{
					succ[js.Index].Add(firstinst.Index);
				}
			}

//			Console.WriteLine("After addition of jumps:");
//			PrintSuccessors(instlist, succ);


			liveIn = new Set<VirtualRegister>[numberOfInst];
			liveOut = new Set<VirtualRegister>[numberOfInst];
			for (int i = 0; i < numberOfInst; i++)
			{
				liveIn[i] = new Set<VirtualRegister>();
				liveOut[i] = new Set<VirtualRegister>();
			}

			// Iterates until no changes.
			bool reIterate;
			List<VirtualRegister> instUse = new List<VirtualRegister>();
			do
			{
				reIterate = false;

				for (int i = numberOfInst - 1; i >= 0; i--)
				{
					Set<VirtualRegister> oldLiveIn = liveIn[i];
					Set<VirtualRegister> newlivein = new Set<VirtualRegister>();
					liveIn[i] = newlivein;

					Set<VirtualRegister> oldLiveOut = liveOut[i];
					Set<VirtualRegister> newliveout = new Set<VirtualRegister>();
					liveOut[i] = newliveout;

					SpuInstruction inst = instlist[i];


					// New live in.
					bool removeDef = false;
					instUse.Clear();
					inst.AppendUses(instUse);

					if (inst.Def != null && !instUse.Contains(inst.Def))
						removeDef = true;
					newlivein.AddAll(instUse);
					newlivein.AddAll(oldLiveOut);
					if (removeDef)
						newlivein.Remove(inst.Def);

					foreach (int s in succ[i])
						newliveout.AddAll(liveIn[s]);

//					// Treat caller saves register as if they were defined by the call instruction.
					// Disabled, since we currently only allocate to callee saves.
//					if (inst.IsCall())
//						foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
//							newlivein.Add(register);

					if (!reIterate)
						reIterate = !oldLiveIn.Equals(newlivein) || !oldLiveOut.Equals(newliveout);
				}
			} while (reIterate);

			if (includeUnusedDefsInLiveOut)
			{
				for (int i = 0; i < instlist.Count; i++)
				{
					SpuInstruction inst = instlist[i];
					if (inst.Def != null)
						liveOut[i].Add(inst.Def);

//					// This is a hack for vregs which only are used immediately after begin defined.
//					// It will make sure that their live interval contains the use, while at the same time
//					// allowig the interval constructor to easily avoid giving defined vregs which are never used a lifetime 
//					// of of more than one instruction.
//					if (i + 1 < instlist.Count && instlist[i+1].Use.Contains(inst.Def))
//						liveOut[i + 1].Add(inst.Def);
				}
			}
		}

		private static void PrintSuccessors(List<SpuInstruction> instlist, Dictionary<int, Set<int>> succ)
		{
			StringWriter sw = new StringWriter();
			for (int i = 0; i < instlist.Count; i++)
			{
				SpuInstruction inst = instlist[i];
				sw.Write("{0,2:d} {1}: ", i, inst.OpCode.Name);
				foreach (int si in succ[i])
				{
					SpuInstruction succinst = instlist[si];
					sw.Write("({0} {1})", si, succinst.OpCode.Name);
				}
				sw.WriteLine();
			}
			Console.WriteLine("Successors: ");
			Console.WriteLine(sw.GetStringBuilder());
		}
	}
}
