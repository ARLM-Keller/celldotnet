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


		private int? _priority;

		private int _satisfiedDependencyCount;
		private int _dependencyCount;

		private string DebuggerDisplay
		{
			get { return _instruction.OpCode.Name + " (inst " + _instruction.SpuInstructionNumber + ", pri " + Priority + ")"; }
		}

		public SpuInstruction Instruction
		{
			get { return _instruction; }
		}

		/// <summary>
		/// Instructions which must be scheduled after this instruction.
		/// </summary>
		public ICollection<InstructionScheduleInfo> Dependents
		{
			get { return _dependents; }
		}

		public int? Priority
		{
			get { return _priority; }
			set { _priority = value; }
		}

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

				// Order memory accesses.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryWrite) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryRead) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.ChannelAccess) != 0 ||
					(inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MethodCall) != 0)
				{
					if (lastMemCallChan != null)
						lastMemCallChan.AddDependant(isi);

					lastMemCallChan = isi;
				}

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
				if (isi.Priority == null)
				{
					// Since we haven't calculated a priority yet, this instructions has no dependencies (in this block).
					initiallyReady.Add(isi);
					CalculatePriority(isi);
					Utilities.AssertNotNull(isi.Priority, "isi.Priority");
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
				if (dep.Priority == null)
					CalculatePriority(dep);
				maxprio = Math.Max(maxprio, dep.Priority.Value);
			}

			isi.Priority = maxprio + isi.Instruction.OpCode.Latency;
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

		//		class SimplePriorityQueue<TKey, TValue>
		//		{
		//			private SortedDictionary<TKey, List<TValue>> _dict = new SortedDictionary<TKey, List<TValue>>();
		//
		//			public bool IsEmpty
		//			{
		//				get { return _dict.Count == 0; }
		//			}
		//
		//			public void Add(TKey key, TValue value)
		//			{
		//				List<TValue> l;
		//				if (!_dict.TryGetValue(key, out l))
		//				{
		//					l = new List<TValue>();
		//					_dict.Add(key, l);
		//				}
		//				else
		//				{
		//					if (l.Contains(value))
		//						throw new ArgumentException("The element is already present.");
		//				}
		//				l.Add(value);
		//			}
		//
		//			public void Remove(TKey key, TValue value)
		//			{
		//				List<TValue> l;
		//				if (!_dict.TryGetValue(key, out l))
		//					throw new ArgumentException();
		//
		//				int i = 0;
		//				foreach (TValue item in l)
		//				{
		//					if (item.Equals(value))
		//					{
		//						if (l.Count == 1)
		//							_dict.Remove(key);
		//						else
		//							l.RemoveAt(i);
		//						return;
		//					}
		//
		//					i++;
		//				}
		//
		//				throw new ArgumentException();
		//			}
		//
		//			public IList<TValue> GetFirstValues()
		//			{
		//				return Utilities.GetFirst(_dict.Values);
		//			}
		//		}

		/// <summary>
		/// Schedules prioritized instructions.
		/// </summary>
		/// <param name="schedlist"></param>
		/// <param name="initiallyReady"></param>
		/// <returns>The new head.</returns>
		public SpuInstruction SchedulePrioritizedInstructions(List<InstructionScheduleInfo> schedlist, List<InstructionScheduleInfo> initiallyReady)
		{
			Set<InstructionScheduleInfo> readySet = new Set<InstructionScheduleInfo>();
			foreach (InstructionScheduleInfo isi in initiallyReady)
				readySet.Add(isi);


			int instnum = 0;
			SpuInstruction head = null;
			SpuInstruction tail = null;
			InstructionScheduleInfo lastAndMaybeBranchInstruction = schedlist[schedlist.Count - 1];
			while (readySet.Count != 0)
			{
				InstructionScheduleInfo isi = GetNextInstruction(readySet, instnum);
				if (isi == lastAndMaybeBranchInstruction)
					continue;

				foreach (InstructionScheduleInfo dependant in isi.Dependents)
				{
					bool isready = dependant.NotifyDependencySatisfied();
					if (isready)
						readySet.Add(dependant);
				}

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

			lastAndMaybeBranchInstruction.Instruction.Next = null;
			if (tail != null)
				tail.Next = lastAndMaybeBranchInstruction.Instruction;
			if (head == null)
				head = lastAndMaybeBranchInstruction.Instruction;

			return head;
		}

		/// <summary>
		/// Returns the instruction with the highest priority. If multiple instructions have maximum priority,
		/// it will attempt to find an instruction which is can issue on <paramref name="instnum"/> and hopefully
		/// be dual-issued.
		/// </summary>
		private InstructionScheduleInfo GetNextInstruction(Set<InstructionScheduleInfo> readySet, int instnum)
		{
			SpuPipeline requestedPipeline = instnum % 2 == 0 ? SpuPipeline.Even : SpuPipeline.Odd;

			// Largest priority at the end.
			List<InstructionScheduleInfo> readylist =new List<InstructionScheduleInfo>(readySet);
			readylist.Sort(delegate(InstructionScheduleInfo x, InstructionScheduleInfo y)
			               	{
								return x.Priority.Value - y.Priority.Value;
			               	});

			int highestPriority = readylist[readySet.Count - 1].Priority.Value;
			int chosenIndex = -1;
			for (int i = readySet.Count - 1; i >= 0; i--)
			{
				if (readylist[i].Priority != highestPriority)
				{
					chosenIndex = i + 1;
					break;
				}
				
				if (readylist[i].Instruction.OpCode.Pipeline == requestedPipeline)
				{
					chosenIndex = i;
					break;
				}
			}
			if (chosenIndex == -1)
				chosenIndex = readylist.Count - 1;


			InstructionScheduleInfo item = readylist[chosenIndex];
//			readylist.RemoveAt(chosenIndex);
			readySet.Remove(item);

			return item;
		}
	}
}
