using System.Collections.Generic;

namespace CellDotNet
{
	internal class IterativLivenessAnalyser
	{
		public static void Analyse(List<SpuBasicBlock> basicBlocks, out Set<VirtualRegister>[] liveIn,
		                           out Set<VirtualRegister>[] liveOut)
		{
			// NOTE instruktions nummereringen starter fra 0.

			Dictionary<int, Set<int>> succ = new Dictionary<int, Set<int>>();
//			Dictionary<int, Set<int>> pred = new Dictionary<int, Set<int>>();

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
//					pred.Add(inst.Index, new Set<int>());

					// Predecessor is not set to null at the beginning of each block.
					if (predecessor != null)
					{
						succ[instNr - 1].Add(instNr);
//						pred[instNr].Add(instNr - 1);
					}
					if (inst.JumpTarget != null)
						jumpSources[inst.JumpTarget].Add(inst);

					predecessor = inst;
					instNr++;
				}
			}

			int numberOfInst = instlist.Count;

			// Add jumps to predecessor og successor info.
			foreach (KeyValuePair<SpuBasicBlock, SpuInstruction> pair in blocks)
			{
				SpuBasicBlock bb = pair.Key;
				SpuInstruction firstinst = pair.Value;

				foreach (SpuInstruction js in jumpSources[bb])
				{
					succ[js.Index].Add(firstinst.Index);
//					pred[firstinst.Index].Add(js.Index);
				}
			}


			liveIn = new Set<VirtualRegister>[numberOfInst];
			liveOut = new Set<VirtualRegister>[numberOfInst];
			for (int i = 0; i < numberOfInst; i++)
			{
				liveIn[i] = new Set<VirtualRegister>();
				liveOut[i] = new Set<VirtualRegister>();
			}

			// Iterates until no changes.
			bool reIterate;
			do
			{
				reIterate = false;

				for (int i = numberOfInst - 1; i >= 0; i--)
				{
					if (i == 53)
						System.Console.WriteLine();

					Set<VirtualRegister> oldLiveIn = liveIn[i];
					Set<VirtualRegister> newlivein = new Set<VirtualRegister>();
					liveIn[i] = newlivein;

					Set<VirtualRegister> oldLiveOut = liveOut[i];
					Set<VirtualRegister> newliveout = new Set<VirtualRegister>();
					liveOut[i] = newliveout;

					SpuInstruction inst = instlist[i];

					// New live in.
					bool removeDef = false;
					if (inst.Def != null && !inst.Use.Contains(inst.Def))
						removeDef = true;
					newlivein.AddAll(inst.Use);
					newlivein.AddAll(oldLiveOut);
					if (removeDef)
						newlivein.Remove(inst.Def);

					foreach (int s in succ[i])
						newliveout.AddAll(liveIn[s]);

					// Treat caller saves register as if they were defined by the call instruction.
					if (inst.IsCall())
						foreach (VirtualRegister register in HardwareRegister.CallerSavesRegisters)
							newlivein.Add(register);

					if (!reIterate)
						reIterate = !oldLiveIn.Equals(newlivein) || !oldLiveOut.Equals(newliveout);
				}
			} while (reIterate);
		}
	}
}
