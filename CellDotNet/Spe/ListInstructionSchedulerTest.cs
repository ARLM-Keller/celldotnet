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

#if UNITTEST

using System;
using System.Collections.Generic;
using NUnit.Framework;



namespace CellDotNet.Spe
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
			AreEqual(1, list[0].Dependents.Count);

			// "il"
			AreEqual(0, list[1].Dependents.Count);

			// "lqd" 1
			AreEqual(1, list[2].Dependents.Count);
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

			List<InstructionScheduleInfo> isilist = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			AreEqual(1, isilist[0].Dependents.Count);
			AreEqual(0, isilist[1].Dependents.Count);
			AreEqual(0, isilist[2].Dependents.Count);

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
			w.WriteBrsl(HardwareRegister.LR, 0); // inst 0

			// Moves hw reg 3 to another reg and should stay after the call inst.
			VirtualRegister destreg = w.NextRegister();
			w.WriteMove(HardwareRegister.HardwareReturnValueRegister, destreg); // inst 1

			w.WriteAi(destreg, 50); // inst 2

			List<InstructionScheduleInfo> list = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			// Move depends on call.
			AreEqual(1, list[0].Dependents.Count);
			AreEqual(list[1], Utilities.GetFirst(list[0].Dependents));

			// Ai depends on move.
			AreEqual(1, list[1].Dependents.Count);
			AreEqual(list[2], Utilities.GetFirst(list[1].Dependents));
		}

		[Test]
		public void TestScheduleMoveRetVal()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();

			w.WriteBrsl(HardwareRegister.LR, 0); // inst 0
			SpuInstruction brsl1 = w.LastInstruction;

			VirtualRegister retval = w.NextRegister();
			w.WriteMove(HardwareRegister.HardwareReturnValueRegister, retval); // inst 1
			SpuInstruction move = w.LastInstruction;

			w.WriteBrsl(HardwareRegister.LR, 0); // inst 2
			SpuInstruction brsl2 = w.LastInstruction;

			List<InstructionScheduleInfo> list = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			AreEqual(3, list.Count);

			AreEqual(brsl1, list[0].Instruction);
			// one dependent because of inst 1 (move) , and another one because of method call ordering.
			AreEqual(2, list[0].Dependents.Count);
			
			AreEqual(move, list[1].Instruction);
			AreEqual(1, list[1].Dependents.Count);

			AreEqual(brsl2, list[2].Instruction);
			AreEqual(0, list[2].Dependents.Count);
		}

		static int ComputePotentialDualIssueCount(IEnumerable<SpuInstruction> instructions)
		{
			int score = 0;

			IList<SpuInstruction> list = instructions as IList<SpuInstruction>;
			if (list == null)
				list = new List<SpuInstruction>(instructions);

			for (int i = 0; i < list.Count; i++)
			{
				if (i%2 == 0 && list[i].OpCode.Pipeline == SpuPipeline.Even)
				{
					if (list.Count > i + 1 && list[i + 1].OpCode.Pipeline == SpuPipeline.Odd)
					{
						i++;
						score++;
					}
				}
			}

			return score;
		}

		[Test]
		public void TestDualIssueIncrease()
		{
			SpuInstructionWriter w = new SpuInstructionWriter();
			w.BeginNewBasicBlock();
			w.WriteShlqbi(w.NextRegister(), w.NextRegister()); // odd, 4
			w.WriteRot(w.NextRegister(), w.NextRegister()); // even, 4
			w.WriteShlqbi(w.NextRegister(), w.NextRegister()); // odd, 4
			w.WriteRot(w.NextRegister(), w.NextRegister()); // even, 4

			List<InstructionScheduleInfo> schedulelist = new ListInstructionScheduler().DetermineDependencies(w.CurrentBlock);
			AreEqual(0, schedulelist[0].Dependents.Count);
			AreEqual(0, schedulelist[1].Dependents.Count);
			AreEqual(0, schedulelist[2].Dependents.Count);
			AreEqual(0, schedulelist[3].Dependents.Count);
			AreEqual(0, ComputePotentialDualIssueCount(w.CurrentBlock.Head.GetEnumerable()));

			new ListInstructionScheduler().Schedule(w.CurrentBlock);
			List<SpuInstruction> instlist = new List<SpuInstruction>(w.CurrentBlock.Head.GetEnumerable());
			AreEqual(SpuPipeline.Even, instlist[0].OpCode.Pipeline);
			AreEqual(SpuPipeline.Odd, instlist[1].OpCode.Pipeline);
			AreEqual(SpuPipeline.Even, instlist[2].OpCode.Pipeline);
//			AreEqual(SpuPipeline.Odd, instlist[3].OpCode.Pipeline);
			AreEqual(2, ComputePotentialDualIssueCount(w.CurrentBlock.Head.GetEnumerable()));
		}
	}
}


#endif