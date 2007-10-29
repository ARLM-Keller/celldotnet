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
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Serialization;

namespace CellDotNet.Spe
{
	partial class SpuInstructionWriter
	{
		private List<SpuBasicBlock> _basicBlocks = new List<SpuBasicBlock>();
		/// <summary>
		/// The instructions that have been written.
		/// </summary>
		public List<SpuBasicBlock> BasicBlocks
		{
			get { return _basicBlocks; }
		}

		public int GetInstructionCount()
		{
			int count = 0;
			foreach (SpuBasicBlock bb in _basicBlocks)
				count += bb.GetInstructionCount();

			return count;
		}

		/// <summary>
		/// Creates a list from the instructions in the basic blocks. You probably don't want to use
		/// this method for anything but experimenting.
		/// </summary>
		/// <returns></returns>
		public List<SpuInstruction> GetAsList()
		{
			List<SpuInstruction> list = new List<SpuInstruction>();
			foreach (SpuBasicBlock bb in _basicBlocks)
			{
				SpuInstruction inst = bb.Head;

				if (inst != null)
					list.AddRange(inst.GetEnumerable());
			}

			return list;
		}

		private int _regnum = 1;

		public VirtualRegister NextRegister()
		{
			return new VirtualRegister(_regnum++);
		}

		/// <summary>
		/// Marks the start of a new <see cref="SpuBasicBlock"/>. You need to call this or <see cref="AppendBasicBlock"/> on a new instance
		/// before using the WriteXXX methods
		/// </summary>
		public void BeginNewBasicBlock()
		{
			_basicBlocks.Add(new SpuBasicBlock());
			_lastInstruction = null;
		}

		/// <summary>
		/// Appends a <see cref="SpuBasicBlock"/>. You need to call this or <see cref="BeginNewBasicBlock"/> on a new instance
		/// before using the WriteXXX methods
		/// </summary>
		public void AppendBasicBlock(SpuBasicBlock basicBlock)
		{
			_basicBlocks.Add(basicBlock);
			_lastInstruction = basicBlock.Head;
			if(_lastInstruction != null)
				while (_lastInstruction.Next != null)
					_lastInstruction = _lastInstruction.Next;
		}

		public SpuBasicBlock CurrentBlock
		{
			get
			{
				if (_basicBlocks.Count == 0)
					throw new InvalidOperationException("No BB has been started.");
				return _basicBlocks[_basicBlocks.Count - 1];
			}
		}

		public SpuInstruction LastInstruction
		{
			get { return _lastInstruction; }
		}

		internal void AddInstructionManually(SpuInstruction inst)
		{
			// Let's keep the public method separate from the private.
			AddInstruction(inst);
		}

		private SpuInstruction _lastInstruction;
		private void AddInstruction(SpuInstruction inst)
		{
			if (_lastInstruction != null)
			{
				inst.Prev = _lastInstruction;

				_lastInstruction.Next = inst;
				_lastInstruction = inst;
			}
			else
			{
				// New bb.
				Utilities.Assert(_basicBlocks.Count != 0, "BeginNewBasicBlock() has not been called.");
				_basicBlocks[_basicBlocks.Count - 1].Head = inst;
				_lastInstruction = inst;
			}
		}

		static private void AssertRegisterNotNull(VirtualRegister reg, string regname)
		{
			if (reg == null)
				throw new ArgumentException("Register argument " + regname + " is null.");
		}

		#region Formats

		private VirtualRegister WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb)
		{
			VirtualRegister rt = NextRegister();
			WriteRR(opcode, ra, rb, rt);
			return rt;
		}

		private void WriteRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rt)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rb;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRR2(SpuOpCode opcode, VirtualRegister ra)
		{
			return WriteRR(opcode, ra, NextRegister());
		}

		private VirtualRegister WriteRR1(SpuOpCode opcode)
		{
			return WriteRR(opcode, NextRegister(), NextRegister());
		}

		private VirtualRegister WriteRR2DE(SpuOpCode opcode, VirtualRegister ra, bool D, bool E)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rt = NextRegister();
			inst.Constant = (D) ? 0x20 : 0x00; // 0x20 = 0100000b
			inst.Constant |= (E) ? 0x10 : 0x00; // 0x10 = 0010000b
			AddInstruction(inst);
			return inst.Rt;
		}

		private VirtualRegister WriteRRR(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rb, VirtualRegister rc)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rb;
			inst.Rc = rc;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private VirtualRegister WriteRI7(SpuOpCode opcode, VirtualRegister ra, int value)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Constant = value & 0x0000007F; //NOTE muligivs undersøge om value passer i 7 bit.
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private VirtualRegister WriteRI8(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Constant = scale & 0x000000ff;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private VirtualRegister WriteRI10(SpuOpCode opcode, VirtualRegister ra, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Constant = scale & 0x000003ff; //NOTE muligivs undersøge om value passer i 10 bit.
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private void WriteRI10Sourced(SpuOpCode opcode, VirtualRegister ra, VirtualRegister rt, int scale)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = ra;
			inst.Rb = rt;
			inst.Constant = scale & 0x000003ff; //NOTE muligivs undersøge om value passer i 10 bit.
			AddInstruction(inst);
		}

		private VirtualRegister WriteRI16(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		private void WriteRI16Sourced(SpuOpCode opcode, VirtualRegister rt, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Ra = rt;
			inst.Constant = symbol & 0x0000ffff;
			AddInstruction(inst);
		}

		private void WriteRI16x(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			AddInstruction(inst);
		}

		private VirtualRegister WriteRI18(SpuOpCode opcode, int symbol)
		{
			SpuInstruction inst = new SpuInstruction(opcode);
			inst.Constant = symbol;
			inst.Rt = NextRegister();
			AddInstruction(inst);
			return inst.Rt;
		}

		#endregion

		// custom instructions ===============================================

		/// <summary>
		/// Pseudo instruction.
		/// </summary>
		/// <param name="src"></param>
		/// <param name="dest"></param>
		public void WriteMove(VirtualRegister src, VirtualRegister dest)
		{
			Utilities.AssertArgumentNotNull(src, "src");
			Utilities.AssertArgumentNotNull(dest, "dest");

			// TODO brug Ori som generel move instruktion.
			// set usesymbolicmove to false to generate code that will allow the
			// simple cell test program to run (20070715)

			bool useSymbolicMove = true;
			if (useSymbolicMove)
			{
				SpuInstruction inst = new SpuInstruction(SpuOpCode.move);
				inst.Ra = src;
				inst.Rt = dest;

				AddInstruction(inst);
			}
			else
			{
				WriteOri(dest, src, 0);
//				SpuInstruction iload = new SpuInstruction(SpuOpCode.il);
//				iload.Constant = 0;
////				iload.Rt = NextRegister();
//				iload.Rt = HardwareRegister.GetHardwareRegister(75);
//				AddInstruction(iload);
//
//				SpuInstruction ior = new SpuInstruction(SpuOpCode.or);
//				ior.Ra = iload.Rt;
//				ior.Rb = src;
//				ior.Rt = dest;
//				AddInstruction(ior);
			}
		}

		/// <summary>
		/// Writes a return pseudo-instruction that must be patched to be a branch
		/// to the epilog.
		/// </summary>
		public void WriteReturn()
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.ret);
			AddInstruction(inst);
		}

		public void WriteDebugStop(VirtualRegister r, ObjectWithAddress debugValueObject)
		{
			WriteStqr(r, debugValueObject);
			WriteStop();
			LastInstruction.Constant = (int)SpuStopCode.DebuggerBreakpoint;
		}

		public void WriteStop(SpuStopCode stopCode)
		{
			Utilities.AssertArgumentRange(((uint)stopCode >> 14) == 0, "stopCode", stopCode);

			WriteStop();
			LastInstruction.Constant = (int) stopCode;
		}

		#region Branching

		/// <summary>
		/// This will generate an instruction that must be patched with a <see cref="SpuBasicBlock"/>.
		/// </summary>
		public void WriteBranch(SpuOpCode branchopcode)
		{
			SpuInstruction inst = new SpuInstruction(branchopcode);
			AddInstruction(inst);
		}

		/// <summary>
		/// This will generate an instruction that must be patched with a <see cref="SpuBasicBlock"/>.
		/// </summary>
		/// <param name="branchopcode"></param>
		/// <param name="condition"></param>
		/// <param name="target"></param>
		public void WriteConditionalBranch(SpuOpCode branchopcode, VirtualRegister condition, ObjectWithAddress target)
		{
			SpuInstruction inst = new SpuInstruction(branchopcode);
			inst.ObjectWithAddress = target;
			inst.Rt = condition;
			AddInstruction(inst);
		}

		/// <summary>
		/// This will generate an instruction that must be patched with a <see cref="SpuBasicBlock"/>.
		/// </summary>
		/// <param name="branchopcode"></param>
		/// <param name="target"></param>
		public void WriteBranchAndSetLink(SpuOpCode branchopcode, ObjectWithAddress target)
		{
			SpuInstruction inst = new SpuInstruction(branchopcode);
			inst.ObjectWithAddress = target;
			inst.Rt = HardwareRegister.LR;
			AddInstruction(inst);
		}

		public void WriteBrsl(ObjectWithAddress owa)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.brsl);
			inst.ObjectWithAddress = owa;
			inst.Rt = HardwareRegister.LR;
			AddInstruction(inst);
		}

		#endregion

		#region Load/store

		public void WriteStore(VirtualRegister rt, ObjectWithAddress target)
		{
			WriteStore(rt, target, 0);
		}

		public void WriteStore(VirtualRegister rt, ObjectWithAddress target, int quadWordOffset)
		{
			AssertRegisterNotNull(rt, "rt");

			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqr);
			inst.Constant = quadWordOffset*4;
			inst.ObjectWithAddress = target;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		/// <summary>
		/// Stores the 4-bytes <paramref name="value"/>.
		/// It is stored in the quadword which is <paramref name="pointerQwOffset"/> quadwords offset from <paramref name="ptr"/>
		/// in the word which is determined by the low four bits of the address (<paramref name="ptr"/>) plus <paramref name="wordNumberAddend"/>.
		/// <para>Use this one to store to a fixed offset.</para>
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="pointerQwOffset"></param>
		/// <param name="value"></param>
		/// <param name="wordNumberAddend"></param>
		public void WriteStoreWord(VirtualRegister ptr, int pointerQwOffset, int wordNumberAddend, VirtualRegister value)
		{
			VirtualRegister loadedvalue = WriteLqd(ptr, pointerQwOffset);
			VirtualRegister mask = WriteCwd(ptr, wordNumberAddend * 4);
			VirtualRegister combined = WriteShufb(value, loadedvalue, mask);
			WriteStqd(combined, ptr, pointerQwOffset);
		}

		/// <summary>
		/// Loads a word from a static offset from a pointer.
		/// </summary>
		/// <param name="ptr"></param>
		/// <param name="pointerQwOffset"></param>
		/// <param name="wordNumber"></param>
		/// <returns></returns>
		public VirtualRegister WriteLoadWord(VirtualRegister ptr, int pointerQwOffset, int wordNumber)
		{
			Utilities.AssertArgumentRange(wordNumber >= 0 && wordNumber <= 3, "wordNumber", wordNumber, "wordNumber >= 0 && wordNumber <= 3");

			VirtualRegister quad = WriteLqd(ptr, pointerQwOffset);

			// Move word to preferred slot if necessary.
			if (wordNumber != 0)
				return WriteShlqbyi(quad, wordNumber * 4);
			else
				return quad;
		}

		public void WriteStoreDoubleWord(VirtualRegister ptr, int pointerQwOffset, int doubleWordNumberAddend, VirtualRegister value)
		{
			VirtualRegister loadedvalue = WriteLqd(ptr, pointerQwOffset);
			VirtualRegister mask = WriteCdd(ptr, doubleWordNumberAddend * 8);
			VirtualRegister combined = WriteShufb(value, loadedvalue, mask);
			WriteStqd(combined, ptr, pointerQwOffset);
		}

		public VirtualRegister WriteLoadDoubleWord(VirtualRegister ptr, int pointerQwOffset, int doubleWordNumber)
		{
			Utilities.AssertArgumentRange(doubleWordNumber >= 0 && doubleWordNumber <= 1, "doubleWordNumber", doubleWordNumber, "doubleWordNumber >= 0 && doubleWordNumber <= 1");

			VirtualRegister quad = WriteLqd(ptr, pointerQwOffset);

			// Move word to preferred slot if necessary.
			if (doubleWordNumber != 0)
				return WriteShlqbyi(quad, doubleWordNumber * 8);
			else
				return quad;
		}

		/// <summary>
		/// This will generate an instruction that must be patched with a <see cref="SpuBasicBlock"/>.
		/// </summary>
		public void WriteStqr(VirtualRegister rt, ObjectWithAddress address)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.stqr);
			inst.ObjectWithAddress = address;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		public void WriteLoad(VirtualRegister rt, ObjectWithAddress objectToLoad)
		{
			WriteLoad(rt, objectToLoad, 0);
		}

		public void WriteLoad(VirtualRegister rt, ObjectWithAddress owa, int quadWordOffset)
		{
			AssertRegisterNotNull(rt, "rt");

			SpuInstruction inst = new SpuInstruction(SpuOpCode.lqr);
			inst.ObjectWithAddress = owa;
			inst.Constant = quadWordOffset*4;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		public VirtualRegister WriteLoad(ObjectWithAddress owa, int quadWordOffset)
		{
			VirtualRegister rt = NextRegister();
			WriteLoad(rt, owa, quadWordOffset);
			return rt;
		}

		public VirtualRegister WriteLoad(ObjectWithAddress objectToLoad)
		{
			VirtualRegister rt = NextRegister();
			WriteLoad(rt, objectToLoad, 0);

			return rt;
		}

		/// <summary>
		/// Pseudo instruction to load the integer into the register.
		/// No other registers are used.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="i"></param>
		/// <returns></returns>
		public void WriteLoadI4(VirtualRegister rt, int i)
		{
			if (i >> 15 == 0)
			{
				WriteIl(rt, i);
			}
			else
			{
				WriteIlhu(rt, i >> 16);
				WriteIohl(rt, i);
			}
		}

		/// <summary>
		/// Pseudo instruction to load the integer into a register.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public VirtualRegister WriteLoadI4(int i)
		{
			VirtualRegister rt = NextRegister();
			WriteLoadI4(rt, i);

			return rt;
		}

		public void WriteLoadR8(VirtualRegister rt, double i)
		{
			long il = Utilities.ReinterpretAsLong(i);

			int high = (int) (il >> 32);

			WriteIlhu(rt, high >> 16);
			WriteIohl(rt, high);

			WriteShlqbyi(rt, 4);

			WriteIlhu(rt, (int)il >> 16);
			WriteIohl(rt, (int)il);
		}

		public VirtualRegister WriteLoadR8(double i)
		{
			VirtualRegister rt = NextRegister();
			WriteLoadR8(rt, i);

			return rt;
		}

		/// <summary>
		/// Pseudo instruction to load a main storage pointer into a register.
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		public VirtualRegister WriteLoadIntPtr(IntPtr i)
		{
			return WriteLoadI4(i.ToInt32());
		}

//		/// <summary>
//		/// Pseudo instruction to load the address of an object into a rgister.
//		/// No other registers are used.
//		/// </summary>
//		/// <param name="rt"></param>
//		/// <param name="owa"></param>
//		/// <returns></returns>
//		public void WriteLoadAddress(VirtualRegister rt, ObjectWithAddress owa)
//		{
//			SpuInstruction inst = new SpuInstruction(SpuOpCode.ila);
//			inst.Constant = 0;
//			inst.Rt = rt;
//			inst.ObjectWithAddress = owa;
//			AddInstruction(inst);
//		}

		/// <summary>
		/// Pseudo instruction to load the address of an object into a rgister.
		/// </summary>
		/// <param name="owa"></param>
		/// <returns></returns>
		[Obsolete("This one probably shouldn't be used. Try to use Load with an offset instead.")]
		public VirtualRegister WriteLoadAddress(ObjectWithAddress owa)
		{
			throw new InvalidOperationException();
//			VirtualRegister rt = NextRegister();
//			WriteLoadAddress(rt, owa);
//
//			return rt;
		}

		#endregion

		#region Channels

		/// <summary>
		/// Read channel.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="channel"></param>
		public void WriteRdch(VirtualRegister rt, SpuReadChannel channel)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.rdch);
			inst.Constant = (int) channel;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		/// <summary>
		/// Read channel.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public VirtualRegister WriteRdch(SpuReadChannel channel)
		{
			VirtualRegister rt = NextRegister();
			WriteRdch(rt, channel);
			return rt;
		}

		/// <summary>
		/// Read channel count.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="channel"></param>
		public void WriteRdchcnt(VirtualRegister rt, SpuWriteChannel channel)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.rchcnt);
			inst.Constant = (int)channel;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		/// <summary>
		/// Read channel count.
		/// </summary>
		/// <param name="channel"></param>
		/// <returns></returns>
		public VirtualRegister WriteRdchcnt(SpuWriteChannel channel)
		{
			VirtualRegister rt = NextRegister();
			WriteRdchcnt(rt, channel);
			return rt;
		}

		/// <summary>
		/// Write channel.
		/// </summary>
		/// <param name="rt"></param>
		/// <param name="channel"></param>
		public void WriteWrch(SpuWriteChannel channel, VirtualRegister rt)
		{
			SpuInstruction inst = new SpuInstruction(SpuOpCode.wrch);
			inst.Constant = (int)channel;
			inst.Rt = rt;
			AddInstruction(inst);
		}

		#endregion
		
		public void WriteDivU(VirtualRegister dividend, VirtualRegister divisor, VirtualRegister quotient, VirtualRegister remainder)
		{
			WriteIl(remainder, 0);
			WriteIl(quotient, 0);

			//if(divisor == 0) return;
			WriteBranch(SpuOpCode.brz);
			LastInstruction.Rt = divisor;
			SpuInstruction br1 = LastInstruction; // TODO Skal brance til slutningen af divisionen.

//			if (divisor > dividend) {remainder = dividend; return;}
			VirtualRegister rr1 = new VirtualRegister();
			WriteClgt(rr1, divisor, dividend);
			WriteBranch(SpuOpCode.brz);
			LastInstruction.Rt = rr1;
			SpuInstruction br2 = LastInstruction; // brancher til efter if sætningen.
			WriteMove(dividend, remainder);
			WriteBranch(SpuOpCode.br);
			SpuInstruction br3 = LastInstruction; // TODO brancher til slutningen af divsion

			BeginNewBasicBlock();

			br2.JumpTarget = CurrentBlock;

			// if (divisor == dividend) {quotient = 1; return;}
			VirtualRegister rr2 = new VirtualRegister();
			WriteCeq(rr1, divisor, dividend);
			WriteBranch(SpuOpCode.brz);
			LastInstruction.Rt = rr1;
			SpuInstruction br4 = LastInstruction; // TODO brancher til efter if sætningen.
			WriteIl(quotient, 1);
			WriteBranch(SpuOpCode.br);
			SpuInstruction br5 = LastInstruction; // TODO brancher til slutningen af divsion

			BeginNewBasicBlock();

			br4.JumpTarget = CurrentBlock;

			VirtualRegister numBit = new VirtualRegister();
			WriteIl(numBit, 32);

			BeginNewBasicBlock();

			VirtualRegister bit = new VirtualRegister();
			VirtualRegister d = new VirtualRegister();


			//while (divisor > remainder){bit = (dividend & 0x80000000) >> 31;remainder = (remainder << 1) | bit;d = dividend;dividend = dividend << 1;num_bits--;}
			VirtualRegister rr3 = new VirtualRegister();
			WriteClgt(rr3, divisor, remainder);
			WriteBranch(SpuOpCode.brz);
			LastInstruction.Rt = rr3;
			SpuInstruction br6 = LastInstruction; // TODO Branch til slutningen af while
			// while body
			//bit = (dividend & 0x80000000)

			int tmp;
			unchecked
			{
				tmp = (int) 0x80000000;
			}
			WriteLoadI4(bit, tmp);

			WriteAnd(bit, dividend, bit);

			//bit = bit >> 31
			WriteRotmi(bit, bit, 33);

			// remainder = (remainder << 1) | bit
			WriteShli(remainder, remainder, 1);
			WriteOr(remainder, remainder, bit);

			// d = dividend
			WriteMove(dividend, d);

			//dividend = dividend << 1
			WriteShli(dividend, dividend, 1);

			//num_bits--
			WriteAi(numBit, numBit, -1);

			WriteBranch(SpuOpCode.br);
			LastInstruction.JumpTarget = CurrentBlock;
			//End of while loop

			BeginNewBasicBlock();
			br6.JumpTarget = CurrentBlock;

			//  dividend = d;remainder = remainder >> 1;num_bits++;
			WriteMove(d, dividend);
			WriteRotmi(remainder, remainder, -63);
			WriteAi(numBit, numBit, 1);

			//for (i = 0; i < num_bits; i++){
			//	bit = (dividend & 0x80000000) >> 31;remainder = (remainder << 1) | bit;
			//	t = remainder - divisor;q = !((t & 0x80000000) >> 31);
			//	dividend = dividend << 1;quotient = (quotient << 1) | q;
			//	if (q){remainder = t;}}

			VirtualRegister i = new VirtualRegister();
			VirtualRegister rr6 = new VirtualRegister();
			WriteCgt(rr6, numBit, i);
			WriteBranch(SpuOpCode.brz);
			SpuInstruction br7 = LastInstruction; // TODO branche til egter for løkken.
			br7.Rt = rr6;
			//bit = (dividend & 0x80000000)
			unchecked
			{
				tmp = (int)0x80000000;
			}
			WriteLoadI4(bit, tmp);
			WriteAnd(bit, dividend, bit);

			//bit = bit >> 31
			WriteRotmi(bit, bit, 33);
			
			//remainder = (remainder << 1) | bit
			WriteShli(remainder, remainder, 1);
			WriteOr(remainder, remainder, bit);

			// t = remainder - divisor 
			VirtualRegister t = new VirtualRegister();
			WriteSf(t, divisor, remainder);

			//q = !((t & 0x80000000) >> 31)
			VirtualRegister q = new VirtualRegister();
			unchecked
			{
				tmp = (int)0x80000000;
			}
			WriteLoadI4(q, tmp);

			WriteAnd(q, t, q);

			WriteRotmi(q, q, 33);

			WriteCeqi(q, q, 0);

			// TODO FIXME q er 0 efter Ceqi selvom q var 0 før.

//			VirtualRegister tmpvr1 = new VirtualRegister();
//			WriteIl(tmpvr1, 0);
//			VirtualRegister tmpvr2 = new VirtualRegister();
//			WriteMove(q, tmpvr2);
//			WriteCeq(q, tmpvr1, tmpvr2);

			WriteAndi(q, q, 1);

			//dividend = dividend << 1
			WriteShli(dividend, dividend, 1);

			//quotient = (quotient << 1) | q
			WriteShli(quotient, quotient, 1);
			WriteOr(quotient, quotient, q);

			//if (q) { remainder = t; }
			WriteBranch(SpuOpCode.brz);
			SpuInstruction br8 = LastInstruction; // TODO brancher til slutningen af divisionen.
			br8.Rt = q;
			br8.JumpTarget = CurrentBlock;
			WriteMove(t, remainder);

			WriteBranch(SpuOpCode.br);
			LastInstruction.JumpTarget = CurrentBlock;
			// End for

			BeginNewBasicBlock();

			br1.JumpTarget = CurrentBlock;
			br3.JumpTarget = CurrentBlock;
			br5.JumpTarget = CurrentBlock;
			br7.JumpTarget = CurrentBlock;

//			//			VirtualRegister tempreg = new VirtualRegister();
//			//			WriteIl(tempreg, 13);
//			WriteMove(q, quotient); //DEBUG
//			WriteBranch(SpuOpCode.br); //DEBUG
//			brreturn = LastInstruction; //DEBUG
//
//
//
//			
//
//			brreturn.JumpTarget = CurrentBlock;
		}

		/// <summary>
		/// Returns the instructions that are currently in the writer as assembly code.
		/// </summary>
		/// <returns></returns>
		public string Disassemble()
		{
			StringWriter tw = new StringWriter();

			Disassemble(tw);

			return tw.GetStringBuilder().ToString();
		}

		private void Disassemble(TextWriter tw)
		{
			int offset = 0;
			foreach (SpuBasicBlock bb in _basicBlocks)
			{
				SpuInstruction inst = bb.Head;
				if (inst == null)
					continue;

				Utilities.AssertNotNull(inst, "inst");

				offset = Disassembler.DisassembleInstructions(inst.GetEnumerable(), offset, tw);
			}
		}

		public void AssertNoPseudoInstructions()
		{
			int bbindex = -1;
			foreach (SpuBasicBlock bb in _basicBlocks)
			{
				bbindex++;
				if (bb.Head == null)
					continue;

				int instnum = 0;
				foreach (SpuInstruction inst in bb.Head.GetEnumerable())
				{
					if ((inst.OpCode.SpecialFeatures & SpuOpCodeSpecialFeatures.Pseudo) != SpuOpCodeSpecialFeatures.None)
						throw new BadSpuInstructionException("Error at basic block " + bbindex + ", instruction " + instnum + ": Pseudo instruction \"" + inst.OpCode.Name + "\" found.");

					instnum++;
				}
			}
		}
	}
}
