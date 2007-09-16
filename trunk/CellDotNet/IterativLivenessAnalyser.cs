using System.Collections.Generic;

namespace CellDotNet
{
	internal class IterativLivenessAnalyser
	{
		public static void Analyse(List<SpuBasicBlock> basicBlocks, out Set<VirtualRegister>[] liveIn, out Set<VirtualRegister>[] liveOut)
		{
			// NOTE instruktions nummereringen starter fra 0.

			List<List<int>> succ = new List<List<int>>();
			List<List<int>> pred = new List<List<int>>();

			Dictionary<SpuBasicBlock, SpuInstruction> jumpTargets = new Dictionary<SpuBasicBlock, SpuInstruction>();
			Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>> jumpSources =
				new Dictionary<SpuBasicBlock, LinkedList<SpuInstruction>>();

			Dictionary<SpuInstruction, int> spuinstToInt = new Dictionary<SpuInstruction, int>();
			List<SpuInstruction> intToSpuinst = new List<SpuInstruction>();

			foreach (SpuBasicBlock bb in basicBlocks)
				jumpSources[bb] = new LinkedList<SpuInstruction>();

			SpuInstruction predecessor = null;

			int instNr = 0;

			foreach (SpuBasicBlock bb in basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				if (inst != null)
				{
					jumpTargets.Add(bb, inst);
				}
				while (inst != null)
				{
					succ.Add(new List<int>());
					pred.Add(new List<int>());

					spuinstToInt.Add(inst, instNr);
					intToSpuinst.Add(inst);

					if (predecessor != null)
					{
						succ[instNr - 1].Add(instNr);
						pred[instNr].Add(instNr - 1);
					}
					if (inst.JumpTarget != null)
						jumpSources[inst.JumpTarget].AddLast(inst);

					predecessor = inst;
					inst = inst.Next;
					instNr++;
				}
			}

			int numberOfInst = instNr;

			liveIn = new Set<VirtualRegister>[numberOfInst];
			liveOut = new Set<VirtualRegister>[numberOfInst];

			// Patch predecessore og successor with jump info
			foreach (KeyValuePair<SpuBasicBlock, SpuInstruction> pair in jumpTargets)
			{
				SpuBasicBlock bb = pair.Key;
				SpuInstruction i = pair.Value;

				int iNr = spuinstToInt[i];
				foreach (SpuInstruction s in jumpSources[bb])
				{
					int sNr = spuinstToInt[s];
					succ[sNr].Add(iNr);
					pred[iNr].Add(sNr);
				}
			}

			for (int i = 0; i < numberOfInst; i++)
			{
				liveIn[i] = new Set<VirtualRegister>();
				liveOut[i] = new Set<VirtualRegister>();
			}

			// Iterates until nochanges.
			bool reIterate;
			do
			{
				reIterate = false;

				for (int i = numberOfInst - 1; i >= 0; i--)
				{
					if(i == 53)
						System.Console.WriteLine();

					Set<VirtualRegister> oldLiveIn = liveIn[i];

					Set<VirtualRegister> oldLiveOut = liveOut[i];

					liveOut[i] = new Set<VirtualRegister>();
					foreach (int s in succ[i])
						liveOut[i].AddAll(liveIn[s]);

					liveIn[i] = new Set<VirtualRegister>();
					liveIn[i].AddAll(liveOut[i]);

					SpuInstruction inst = intToSpuinst[i];

					if (inst.Def != null)
						liveIn[i].Remove(inst.Def);

					// Treate callersaves register as if they vere defined by the call instruction
					if (inst.IsCall())
						foreach (VirtualRegister register in HardwareRegister.CallerSavesVirtualRegisters)
							liveIn[i].Add(register);

					foreach (VirtualRegister register in inst.Use)
						liveIn[i].Add(register);

					if (!reIterate)
						reIterate |= !oldLiveIn.Equals(liveIn[i]) || !oldLiveOut.Equals(liveOut[i]);
				}
			} while (reIterate);
		}
	}
}
