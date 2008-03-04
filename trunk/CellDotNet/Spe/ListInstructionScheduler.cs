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
using System.Diagnostics;
using C5;
using CellDotNet;

namespace CellDotNet.Spe
{
	/// <summary>
	/// Represents a <see cref="SpuInstruction"/> during instruction scheduling.
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
	class InstructionScheduleInfo
	{
		private SpuInstruction _instruction;

		private List<InstructionScheduleInfo> _dependents = new List<InstructionScheduleInfo>(3);


		private int? _criticalPathLength;

		private int _satisfiedDependencyCount;
		private int _dependencyCount;

		private string DebuggerDisplay
		{
			get { return _instruction.OpCode.Name + " (inst " + _instruction.SpuInstructionNumber + ", pri " + CriticalPathLength + ")"; }
		}

		public SpuInstruction Instruction
		{
			get { return _instruction; }
		}

		/// <summary>
		/// Instructions which must be scheduled after this instruction.
		/// </summary>
		public System.Collections.Generic.ICollection<InstructionScheduleInfo> Dependents
		{
			get { return _dependents; }
		}

		public int? CriticalPathLength
		{
			get { return _criticalPathLength; }
			set { _criticalPathLength = value; }
		}

		public int RemainingStalls
		{
			get { return _remainingStalls; }
			set { _remainingStalls = value; }
		}

		private int _remainingStalls;

		public InstructionScheduleInfo(SpuInstruction instruction)
		{
			_instruction = instruction;
		}

		public void NotifyDependency()
		{
			_dependencyCount++;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns>true if the instruction has just become ready.</returns>
		public bool NotifyDependencySatisfied()
		{
			_satisfiedDependencyCount++;


			Utilities.Assert(_dependencyCount >= _satisfiedDependencyCount, "_dependencyCount >= _satisfiedDependencyCount");

			return _satisfiedDependencyCount == _dependencyCount;
		}

		public void AddDependant(InstructionScheduleInfo dependant)
		{
			foreach (InstructionScheduleInfo isi in _dependents)
			{
				if (isi.Instruction == dependant.Instruction)
					return;
			}
			_dependents.Add(dependant);
			dependant.NotifyDependency();
		}

		public class CriticalPathComparer : IComparer<InstructionScheduleInfo>
		{
			public int Compare(InstructionScheduleInfo x, InstructionScheduleInfo y)
			{
				int v = x.CriticalPathLength.Value - y.CriticalPathLength.Value;
				if (v != 0)
					return v;
				return x.Instruction.SpuInstructionNumber - y.Instruction.SpuInstructionNumber;
			}
		}

		public class RemainingStallsComparer : IComparer<InstructionScheduleInfo>
		{
			public int Compare(InstructionScheduleInfo x, InstructionScheduleInfo y)
			{
				int v = x.RemainingStalls - y.RemainingStalls;
				if (v != 0)
					return v;
				return x.Instruction.SpuInstructionNumber - y.Instruction.SpuInstructionNumber;
			}
		}
	}

	/// <summary>
	/// Schedules SPU instructions.
	/// </summary>
	class ListInstructionScheduler
	{
		public List<InstructionScheduleInfo> DetermineDependencies(SpuBasicBlock bb)
		{
			List<InstructionScheduleInfo> instlist = new List<InstructionScheduleInfo>();

			if (bb.Head == null)
				return instlist;

			// Play it safe: Memory ops, method calls and channel ops keeps their ordering.
			InstructionScheduleInfo lastMemCallChan = null;

			List<VirtualRegister> instUse = new List<VirtualRegister>();

			// Contains the last instruction which has read or written a register.
			Dictionary<VirtualRegister, InstructionScheduleInfo> regTouch = new Dictionary<VirtualRegister, InstructionScheduleInfo>();
			foreach (SpuInstruction inst in bb.Head.GetEnumerable())
			{
				InstructionScheduleInfo isi = new InstructionScheduleInfo(inst);
				instlist.Add(isi);

				// Keep ordering of some types of opcodes.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryWrite) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryRead) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.ChannelAccess) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.Branch) != 0)
				{
					if (lastMemCallChan != null)
						lastMemCallChan.AddDependant(isi);

					lastMemCallChan = isi;
				}

				// Handle method calls.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MethodCall) != 0)
				{
					SpuRoutine routine = inst.ObjectWithAddress as SpuRoutine;
					InstructionScheduleInfo prev;
					bool hasHandledReg3 = false;
					if (routine != null)
					{
						hasHandledReg3 = routine.Parameters.Count > 0;
						for (int i = 0; i < routine.Parameters.Count; i++)
						{
							VirtualRegister usedRegister = HardwareRegister.GetHardwareArgumentRegister(i);
							if (regTouch.TryGetValue(usedRegister, out prev))
								prev.AddDependant(isi);
							regTouch[usedRegister] = isi;
						}
					}

					// Assume that there is a return value.
					if (!hasHandledReg3)
					{
						if (regTouch.TryGetValue(HardwareRegister.HardwareReturnValueRegister, out prev))
							prev.AddDependant(isi);
						regTouch[HardwareRegister.HardwareReturnValueRegister] = isi;
					}
				}
				else
				{
					// Order register defines.
					instUse.Clear();
					inst.AppendUses(instUse);
					VirtualRegister def = inst.Def;
					if (def != null)
						instUse.Add(def);
					foreach (VirtualRegister usedRegister in instUse)
					{
						InstructionScheduleInfo prev;
						if (regTouch.TryGetValue(usedRegister, out prev) && prev != isi)
							prev.AddDependant(isi);
						regTouch[usedRegister] = isi;
					}
				}
			}

			return instlist;
		}

		public void Prioritize(List<InstructionScheduleInfo> schedlist, out List<InstructionScheduleInfo> initiallyReady)
		{
			initiallyReady = new List<InstructionScheduleInfo>();

			foreach (InstructionScheduleInfo isi in schedlist)
			{
				if (isi.CriticalPathLength == null)
				{
					// Since we haven't calculated a priority yet, this instructions has no dependencies (in this block).
					initiallyReady.Add(isi);
					CalculatePriority(isi);
					Utilities.AssertNotNull(isi.CriticalPathLength, "isi.CriticalPathLength");
				}
			}
		}

		/// <summary>
		/// Calculates the priority of the instruction as the maximum priority of any dependant
		/// instruction plus this instruction's latency.
		/// </summary>
		/// <param name="isi"></param>
		private void CalculatePriority(InstructionScheduleInfo isi)
		{
			int maxprio = 0;
			foreach (InstructionScheduleInfo dep in isi.Dependents)
			{
				if (dep.CriticalPathLength == null)
					CalculatePriority(dep);
				maxprio = Math.Max(maxprio, dep.CriticalPathLength.Value);
			}

			isi.CriticalPathLength = maxprio + isi.Instruction.OpCode.Latency;
		}

		public void Schedule(SpuBasicBlock block)
		{
			if (block.Head == null)
				return;

			List<InstructionScheduleInfo> schedlist = DetermineDependencies(block);
			List<InstructionScheduleInfo> initiallyReady;
			Prioritize(schedlist, out initiallyReady);
			SpuInstruction newhead = SchedulePrioritizedInstructions(schedlist, initiallyReady);
			block.Head = newhead;
		}

		/// <summary>
		/// Schedules prioritized instructions.
		/// </summary>
		/// <param name="schedlist"></param>
		/// <param name="initiallyReady"></param>
		/// <returns>The new head.</returns>
		public SpuInstruction SchedulePrioritizedInstructions(List<InstructionScheduleInfo> schedlist, List<InstructionScheduleInfo> initiallyReady)
		{
			TreeSet<InstructionScheduleInfo> readySet = new TreeSet<InstructionScheduleInfo>(new InstructionScheduleInfo.RemainingStallsComparer());
			TreeSet<InstructionScheduleInfo> totallyReadySet = new TreeSet<InstructionScheduleInfo>(new InstructionScheduleInfo.CriticalPathComparer());
			totallyReadySet.AddAll(initiallyReady);

			int instnum = 0;
			SpuInstruction head = null;
			SpuInstruction tail = null;

			InstructionScheduleInfo lastAndMaybeBranchInstruction = schedlist[schedlist.Count - 1];
			if ((lastAndMaybeBranchInstruction.Instruction.OpCode.SpecialFeatures & 
				(SpuOpCodeSpecialFeatures.Branch | SpuOpCodeSpecialFeatures.Control)) == 0)
				lastAndMaybeBranchInstruction = null;

			while ((readySet.Count + totallyReadySet.Count) != 0)
			{
				InstructionScheduleInfo isi = GetNextInstruction(totallyReadySet, readySet, instnum);
				if (isi == lastAndMaybeBranchInstruction)
					continue;

				if (head == null)
				{
					head = isi.Instruction;
					head.Prev = null;
					head.Next = null;

					tail = head;
				}
				else
				{
					SpuInstruction newtail = isi.Instruction;
					newtail.Prev = tail;
					newtail.Next = null;
					tail.Next = newtail;
					tail = newtail;
				}

				instnum++;
			}


			if (lastAndMaybeBranchInstruction != null)
			{
				lastAndMaybeBranchInstruction.Instruction.Next = null;
				if (tail != null)
					tail.Next = lastAndMaybeBranchInstruction.Instruction;
				if (head == null)
					head = lastAndMaybeBranchInstruction.Instruction;
			}

			return head;
		}

		/// <summary>
		/// Returns the instruction with the highest priority. If multiple instructions have maximum priority,
		/// it will attempt to find an instruction which it can issue on <paramref name="instnum"/> and hopefully
		/// be dual-issued.
		/// </summary>
		private InstructionScheduleInfo GetNextInstruction(TreeSet<InstructionScheduleInfo> totallyReadySet,
		                                                   TreeSet<InstructionScheduleInfo> readySet, int instnum)
		{
			SpuPipeline requestedPipeline = instnum % 2 == 0 ? SpuPipeline.Even : SpuPipeline.Odd;

			int sum1 = readySet.Count + totallyReadySet.Count;
			Utilities.DebugAssert(sum1 != 0, "sum1 != 0");

			foreach (InstructionScheduleInfo ready in readySet)
				ready.RemainingStalls--;

			// Move from ready to totally ready.
			InstructionScheduleInfo rItem = null;
			while (readySet.Count != 0)
			{
				InstructionScheduleInfo candidate = readySet.FindMin();
				if (candidate.RemainingStalls > 0)
					break;

				Utilities.DebugAssert(!totallyReadySet.Contains(candidate));
				bool s = totallyReadySet.Add(readySet.DeleteMin());
				Utilities.DebugAssert(s);

				if (rItem == null)
					rItem = candidate;
				else if (candidate.Instruction.OpCode.Pipeline == requestedPipeline)
					rItem = candidate;
			}

			// Look for item to return in TR.
			InstructionScheduleInfo trItem = null;
			foreach (InstructionScheduleInfo candidate in totallyReadySet.Backwards())
			{
				if (trItem == null)
					trItem = candidate;
				if (candidate.CriticalPathLength.Value != trItem.CriticalPathLength.Value)
					break;
				if (candidate.Instruction.OpCode.Pipeline == requestedPipeline)
				{
					trItem = candidate;
					break;
				}
			}

			Utilities.DebugAssert(totallyReadySet.Count == 0 || trItem != null);

			if (trItem != null)
			{
				bool s = totallyReadySet.Remove(trItem);
				Utilities.DebugAssert(s);
			}
			else if (rItem == null)
			{
				Utilities.DebugAssert(readySet.Count != 0);

				// Didn't find anything so far. Take one from the ready set.
				foreach (InstructionScheduleInfo candidate in readySet)
				{
					if (rItem == null)
						rItem = candidate;
					if (rItem.RemainingStalls != candidate.RemainingStalls)
						break;
					if (candidate.Instruction.OpCode.Pipeline == requestedPipeline)
					{
						rItem = candidate;
						break;
					}
				}
				readySet.Remove(rItem);
			}

			int sum2 = readySet.Count + totallyReadySet.Count;
			Utilities.DebugAssert(sum2 == sum1 - 1, "1: " + sum1 + " " + sum2);

			InstructionScheduleInfo item = trItem ?? rItem;

			foreach (InstructionScheduleInfo dependant in item.Dependents)
			{
				bool becameReady = dependant.NotifyDependencySatisfied();
				if (becameReady)
				{
					dependant.RemainingStalls = item.Instruction.OpCode.Latency;
					readySet.Add(dependant);
				}
			}

			return item;
		}
	}
}
