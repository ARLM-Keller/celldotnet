using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class ListInstructionSchedulerTest : UnitTest
	{
		[Test]
		public void TestDependencies()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			VirtualRegister three = w.WriteIl(3);
			w.WriteIlh(50); // throw away.
			w.WriteAi(three, 10);
			SpuInstruction ai_inst = w.LastInstruction;

			List<InstructionScheduleInfo> list = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			// "il"
			AreEqual(1, list[0].Dependents.Count);
			AreSame(ai_inst, Utilities.GetFirst(list[0].Dependents).Instruction);

			// "ilh"
			AreEqual(0, list[1].Dependents.Count);

			// "ai"
			AreEqual(0, list[2].Dependents.Count);
		}

		[Test]
		public void TestDependencies_Memory()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			w.WriteStqd(new VirtualRegister(), new VirtualRegister(),  0); // inst 0
			w.WriteIl(3); // throw away. inst 1
			w.WriteLqd(new VirtualRegister(), 0); // inst 2
			w.WriteLqd(new VirtualRegister(), 0); // inst 3

			List<InstructionScheduleInfo> list = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);

			// "stqd"
			AreEqual(2, list[0].Dependents.Count);

			// "il"
			AreEqual(0, list[1].Dependents.Count);

			// "lqd" 1
			AreEqual(0, list[2].Dependents.Count);
			// "lqd" 2
			AreEqual(0, list[3].Dependents.Count);
		}

		[Test]
		public void TestSchedule()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			VirtualRegister hundredReg = w.WriteIla(100);
			SpuInstruction hundredinst = w.LastInstruction;
			
			w.WriteAi(hundredReg, 50); // Uses hundred and should go to the end.
			SpuInstruction fiftyInst = w.LastInstruction;

			w.WriteIl(3);
			SpuInstruction ilInst = w.LastInstruction;

			// The scheduler wants to keep the tail.
			w.WriteStop();

			new ListInstructionScheduler().Schedule(w.CurrentBlock);
			List<SpuInstruction> ilist = new List<SpuInstruction>(w.CurrentBlock.Head.GetEnumerable());

			// "stqd"
			AreEqual(4, ilist.Count);
			AreSame(hundredinst, ilist[0]);
			AreSame(ilInst, ilist[1]);
			AreSame(fiftyInst, ilist[2]);
		}

		[Test]
		public void TestSchedulePostMethodCallMove()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			// Call.
			w.WriteBrsl(50); // inst 0

			// Moves hw reg 3 to another reg and should stay after the call inst.
			VirtualRegister destreg = w.NextRegister();
			w.WriteMove(HardwareRegister.GetHardwareArgumentRegister(3), destreg); // inst 1

			w.WriteAi(destreg, 50); // inst 2

			List<InstructionScheduleInfo> list = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			// Move depends on call.
			AreEqual(1, list[0].Dependents.Count);
			AreEqual(list[1], Utilities.GetFirst(list[0].Dependents));

			// Ai depends on move.
			AreEqual(1, list[1].Dependents.Count);
			AreEqual(list[2], Utilities.GetFirst(list[1].Dependents));
		}
	}
}