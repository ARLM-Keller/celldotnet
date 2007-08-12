using NUnit.Framework;

namespace CellDotNet
{
	[TestFixture]
	public class SimpleRegAllocTest
	{
		//TODO lav test til modificeret SimpleRegAlloc

//		[Test]
//		public void SingleInst()
//		{
//			SimpleRegAlloc regalloc = new SimpleRegAlloc();
//
//			List<SpuInstruction> insts = new List<SpuInstruction>();
//
//			SpuInstruction inst = new SpuInstruction(SpuOpCode.a);
//			inst.Ra = new VirtualRegister(0);
//			inst.Rb = new VirtualRegister(1);
//			inst.Rt = new VirtualRegister(2);
//
//			insts.Add(inst);
//
//			// alloc returnere true hvis der forekommer spill.
//			if (regalloc.alloc(insts, 0))
//			{
//				throw new Exception();
//			}
//		}
//
//		[Test]
//		public void MultipleInst()
//		{
//			SimpleRegAlloc regalloc = new SimpleRegAlloc();
//
//			List<SpuInstruction> insts = new List<SpuInstruction>();
//
//			SpuInstruction inst = new SpuInstruction(SpuOpCode.a);
//			inst.Ra = new VirtualRegister(0);
//			inst.Rb = new VirtualRegister(1);
//			inst.Rt = new VirtualRegister(2);
//			insts.Add(inst);
//
//			inst = new SpuInstruction(SpuOpCode.sf);
//			inst.Ra = new VirtualRegister(3);
//			inst.Rb = new VirtualRegister(4);
//			inst.Rt = new VirtualRegister(5);
//			insts.Add(inst);
//
//
//			// alloc returnere true hvis der forekommer spill.
//			if (regalloc.alloc(insts, 0))
//			{
//				throw new Exception();
//			}
//		}
//
//		[Test, Ignore("Not implemented")]
//		public void Spill()
//		{
//			SimpleRegAlloc regalloc = new SimpleRegAlloc();
//
//			List<SpuInstruction> insts = new List<SpuInstruction>();
//
//			SpuInstruction inst;
//
//			List<VirtualRegister> spillreg = new List<VirtualRegister>();
//
//			for (int i = 0; i < 150; i++)
//			{
//				spillreg.Add(new VirtualRegister(i));
//			}
//
//			foreach (VirtualRegister r in spillreg)
//			{
//				inst = new SpuInstruction(SpuOpCode.a);
//				inst.Ra = new VirtualRegister(0);
//				inst.Rb = new VirtualRegister(1);
//				inst.Rt = r;
//				insts.Add(inst);
//			}
//
//			foreach (VirtualRegister r in spillreg)
//			{
//				inst = new SpuInstruction(SpuOpCode.a);
//				inst.Ra = r;
//				inst.Rb = new VirtualRegister(1);
//				inst.Rt = new VirtualRegister(1);
//				insts.Add(inst);
//			}
//
//			// alloc returnere true hvis der forekommer spill.
//			if (regalloc.alloc(insts, 0))
//			{
//				throw new Exception();
//			}
//		}
	}
}