using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Represents a <see cref="SpuInstruction"/> during instruction scheduling.
	/// </summary>
	class InstructionScheduleInfo
	{
		private SpuInstruction _instruction;

		private List<InstructionScheduleInfo> _dependents = new List<InstructionScheduleInfo>(3);

		private int? _priority;

		private int _satisfiedDependencyCount;
		private int _dependencyCount;

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

			InstructionScheduleInfo lastMemWrite = null;
			InstructionScheduleInfo lastChannelAccess = null;
			InstructionScheduleInfo lastCall = null;

			List<VirtualRegister> instUse = new List<VirtualRegister>();

			// Contains the last instruction which has defined a register.
			Dictionary<VirtualRegister, InstructionScheduleInfo> defs = new Dictionary<VirtualRegister, InstructionScheduleInfo>();
			Set<InstructionScheduleInfo> hwRegDefsForMethods = new Set<InstructionScheduleInfo>();
			foreach (SpuInstruction inst in bb.Head.GetEnumerable())
			{
				InstructionScheduleInfo isi = new InstructionScheduleInfo(inst);
				instlist.Add(isi);

				// Order memory accesses.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryWrite) != 0)
				{
					if (lastMemWrite != null)
						lastMemWrite.AddDependant(isi);
					lastMemWrite = isi;
				}
				else if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MemoryRead) != 0 && lastMemWrite != null)
				{
					lastMemWrite.AddDependant(isi);
				}

				// Order channel access.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.ChannelAccess) != 0)
				{
					if (lastChannelAccess != null)
						lastChannelAccess.AddDependant(isi);
					lastChannelAccess = isi;
				}

				// Order method calls.
				if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.MethodCall) != 0)
				{
					if (lastCall != null)
						lastCall.AddDependant(isi);
					lastCall = isi;

					// Order previous instructions which moved into hw regs.
					foreach (InstructionScheduleInfo hwdef in hwRegDefsForMethods)
						hwdef.AddDependant(isi);

					hwRegDefsForMethods.Clear();
				}

				// Order moves from hw regs after last call.
				if (inst.OpCode == SpuOpCode.move && inst.Ra.IsRegisterSet && lastCall != null)
				{
					lastCall.AddDependant(isi);
				}

				// Order register defines.
				instUse.Clear();
				inst.AppendUses(instUse);
				foreach (VirtualRegister usedReg in instUse)
				{
					InstructionScheduleInfo prev;
					if (defs.TryGetValue(usedReg, out prev))
						prev.AddDependant(isi);
				}

				VirtualRegister def = inst.Def;
				if (def != null)
				{
					defs[def] = isi;
					if (def.IsRegisterSet)
						hwRegDefsForMethods.Add(isi);
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
			List<InstructionScheduleInfo> schedlist = DetermineDependencies(block);
			List<InstructionScheduleInfo> initiallyReady;
			Prioritize(schedlist, out initiallyReady);
			SpuInstruction newhead = SchedulePrioritizedInstructions(schedlist, initiallyReady);
			block.Head = newhead;
		}

		class SimplePriorityQueue<TKey, TValue>
		{
			private SortedDictionary<TKey, List<TValue>> _dict = new SortedDictionary<TKey, List<TValue>>();

			public bool IsEmpty
			{
				get { return _dict.Count == 0; }
			}

			public void Add(TKey key, TValue value)
			{
				List<TValue> l;
				if (!_dict.TryGetValue(key, out l))
				{
					l = new List<TValue>();
					_dict.Add(key, l);
				}
				else
				{
					if (l.Contains(value))
						throw new ArgumentException("The element is already present.");
				}
				l.Add(value);
			}

			public void Remove(TKey key, TValue value)
			{
				List<TValue> l;
				if (!_dict.TryGetValue(key, out l))
					throw new ArgumentException();

				int i = 0;
				foreach (TValue item in l)
				{
					if (item.Equals(value))
					{
						if (l.Count == 1)
							_dict.Remove(key);
						else
							l.RemoveAt(i);
						return;
					}

					i++;
				}

				throw new ArgumentException();
			}

			public IList<TValue> GetFirstValues()
			{
				return Utilities.GetFirst(_dict.Values);
			}
		}

		/// <summary>
		/// Schedules prioritized instructions.
		/// </summary>
		/// <param name="schedlist"></param>
		/// <param name="initiallyReady"></param>
		/// <returns>The new head.</returns>
		public SpuInstruction SchedulePrioritizedInstructions(List<InstructionScheduleInfo> schedlist, List<InstructionScheduleInfo> initiallyReady)
		{
			// Determine initial ready set.
			SimplePriorityQueue<int, InstructionScheduleInfo> readySet = new SimplePriorityQueue<int, InstructionScheduleInfo>();
			foreach (InstructionScheduleInfo isi in initiallyReady)
			{
				// Use the negated priority to enumerate the biggest first.
				readySet.Add(-isi.Priority.Value, isi);
			}

			int instnum = 0;
			SpuInstruction head = null;
			SpuInstruction tail = null;
			InstructionScheduleInfo lastAndMaybeBranchInstruction = schedlist[schedlist.Count-1];
			while (!readySet.IsEmpty)
			{
				InstructionScheduleInfo isi = GetNextInstruction(readySet, instnum);
				if (isi == lastAndMaybeBranchInstruction)
					continue;

				foreach (InstructionScheduleInfo dependant in isi.Dependents)
				{
					bool isready = dependant.NotifyDependencySatisfied();
					if (isready)
						readySet.Add(-dependant.Priority.Value, dependant);
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
		/// <param name="set"></param>
		/// <param name="instnum"></param>
		/// <returns></returns>
		private InstructionScheduleInfo GetNextInstruction(SimplePriorityQueue<int, InstructionScheduleInfo> set, int instnum)
		{
			InstructionScheduleInfo candidate = null;
			SpuPipeline requestedPipeline = instnum%2 == 0 ? SpuPipeline.Even : SpuPipeline.Odd;

			foreach (InstructionScheduleInfo isi in set.GetFirstValues())
			{
				if (candidate == null)
					candidate = isi;
				else if (isi.Priority < candidate.Priority)
					break;

				if (isi.Instruction.OpCode.Pipeline == requestedPipeline)
				{
					candidate = isi;
					break;
				}
			}

			set.Remove(-candidate.Priority.Value, candidate);

			return candidate;
		}
	}
}
