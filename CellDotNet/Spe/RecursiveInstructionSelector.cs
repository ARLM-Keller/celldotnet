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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CellDotNet.Intermediate;
using JetBrains.Annotations;

namespace CellDotNet.Spe
{
	/// <summary>
	/// This class writes IL trees as SPU instructions.
	/// </summary>
	class RecursiveInstructionSelector
	{
		private SpuInstructionWriter _writer;
		private List<IRBasicBlock> _basicBlocks;

		private ReadOnlyCollection<MethodParameter> _parameters;

		private List<KeyValuePair<SpuInstruction, IRBasicBlock>> _branchInstructions;
		private Dictionary<IRBasicBlock, SpuBasicBlock> _spubasicblocks;

		private List<IROpCode> _unimplementedOpCodes;

		private readonly SpecialSpeObjects _specialSpeObjects;
		private readonly StackSpaceAllocator _stackAllocate;
		private InstructionWritingHelper _helper;


		public RecursiveInstructionSelector()
		{
		}

		public RecursiveInstructionSelector([NotNull]SpecialSpeObjects specialSpeObjects, StackSpaceAllocator spaceAllocator)
		{
			Utilities.AssertArgumentNotNull(specialSpeObjects, "specialSpeObjects");
			_specialSpeObjects = specialSpeObjects;
			_stackAllocate = spaceAllocator;
		}

		public void GenerateCode(List<IRBasicBlock> basicBlocks, ReadOnlyCollection<MethodParameter> parameters, SpuInstructionWriter writer)
		{
			_writer = writer;
			_helper = new InstructionWritingHelper(_writer, _specialSpeObjects, _stackAllocate);
			_basicBlocks = basicBlocks;

			_parameters = parameters;

			_unimplementedOpCodes = new List<IROpCode>();

			// These two are used to patch up branch instructions after instruction selection.
			_branchInstructions = new List<KeyValuePair<SpuInstruction, IRBasicBlock>>();
			_spubasicblocks = new Dictionary<IRBasicBlock, SpuBasicBlock>();

			// The moves are currently performed by the linear register allocator.
			WriteFirstBasicBlock();

			foreach (IRBasicBlock bb in _basicBlocks)
			{
				_writer.BeginNewBasicBlock();

//				_writer.WriteNop();

				_spubasicblocks.Add(bb, _writer.CurrentBlock);
				foreach (TreeInstruction root in bb.Roots)
				{
					GenerateCode(root);
				}
			}

			if (_unimplementedOpCodes.Count > 0)
			{
				string msg = string.Format(
					"Instruction selection encountered {0} IR instructions " + 
					"which are not currently supported, or their operand types are not supported.\r\n" + 
					"The instructions opcodes are:\r\n", _unimplementedOpCodes.Count);

				var ocnames = Utilities.RemoveDuplicates(_unimplementedOpCodes).Select(input => input.Name).ToArray();
				msg += string.Join(", ", ocnames.ToArray()) + ".";

					throw new NotImplementedException(msg);
			}

			// Patch generated branch instructions with their target spu basic blocks.
			foreach (KeyValuePair<SpuInstruction, IRBasicBlock> pair in _branchInstructions)
			{
				SpuBasicBlock target = _spubasicblocks[pair.Value];
				pair.Key.JumpTarget = target;
			}
		}


		/// <summary>
		/// Creates the first basic block of the method, which moves arguments from physical
		/// registers to virtual registers.
		/// </summary>
		private void WriteFirstBasicBlock()
		{
			if (_parameters.Count > 72)
				throw new NotSupportedException("More than 72 arguments is not supported.");
			if (_parameters.Count == 0)
				return;

			_writer.BeginNewBasicBlock();
			for (int i = 0; i < _parameters.Count; i++)
			{
				MethodParameter parameter = _parameters[i];

				VirtualRegister src = HardwareRegister.GetHardwareArgumentRegister(i);
				if (!parameter.StackType.IsStackValueType)
				{
					_writer.WriteMove(src, parameter.VirtualRegister);
				}
				else
				{
					// Copy struct to stack.
					_helper.WriteMoveQuadWords(src, 0, HardwareRegister.SP, parameter.StackLocation, parameter.StackType.ComplexType.QuadWordCount);
				}
			}
		}

		private VirtualRegister GenerateCode(TreeInstruction inst)
		{
			VirtualRegister vrleft = null, vrright = null;

			// Subtree instructions.
			List<VirtualRegister> childregs = new List<VirtualRegister>();
			foreach (TreeInstruction child in inst.GetChildInstructions())
			{
				VirtualRegister reg = GenerateCode(child);
				Utilities.AssertNotNull(reg, "GenerateCode childreg is null.");
				childregs.Add(reg);
			}
			if (childregs.Count >= 1)
			{
				vrleft = childregs[0];
				if (childregs.Count >= 2)
					vrright = childregs[1];
			}


			// Assert that vrleft and vrright are not null if the opcode requires them not to be.
			if (inst.Opcode.ReflectionOpCode != null)
			{
				switch (IROpCode.GetPopBehavior(inst.Opcode.ReflectionOpCode.Value.StackBehaviourPop))
				{
					case PopBehavior.Pop0:
						Utilities.Assert(vrleft == null && vrright == null, "vrleft == null && vrright == null");
						break;
					case PopBehavior.Pop1:
						Utilities.Assert(vrleft != null && vrright == null, "vrleft != null && vrright == null");
						break;
					case PopBehavior.Pop2:
						Utilities.Assert(vrleft != null && vrright != null, "vrleft == null && vrright == null");
						break;
					case PopBehavior.Pop3:
						throw new InvalidIRTreeException("PopBehavior.Pop3");
				}
			}

			IRCode ilcode = inst.Opcode.IRCode;
			StackTypeDescription lefttype = inst.Left != null ? inst.Left.StackType : StackTypeDescription.None;
			StackTypeDescription righttype = inst.Right != null ? inst.Right.StackType : StackTypeDescription.None;
			switch (ilcode)
			{
				case IRCode.Nop:
					return null;
				case IRCode.Break:
					break;
				case IRCode.Ldnull:
					return _writer.WriteLoadI4(0);
				case IRCode.Ldc_I4:
					return _writer.WriteLoadI4((int) inst.Operand);
				case IRCode.Ldc_I8:
					break;
				case IRCode.Ldc_R4:
					{
						uint val = Utilities.ReinterpretAsUInt((float) inst.Operand);
						return _writer.WriteLoadI4((int) val);

//						VirtualRegister reg = _writer.WriteIlhu((int) ((val >> 16) & 0xffff));
//						_writer.WriteIohl(reg, (int) (val & 0xffff));
//						return reg;
					}
				case IRCode.Ldc_R8:
					return _writer.WriteLoadR8((double)inst.Operand);
				case IRCode.Dup:
					// This is rewritten to stloc, ldloc, ldloc.
					break;
				case IRCode.Pop:
					// Does it make sense that this are IR instructions?
					return null;
				case IRCode.Jmp:
					break;
				case IRCode.SpuInstructionMethodCall:
					{
						MethodCallInstruction callInst = (MethodCallInstruction) inst;
						return _helper.GenerateSpuInstructionMethod(_writer, callInst, childregs);
					}
				case IRCode.IntrinsicNewObj:
				case IRCode.IntrinsicCall:
					{
						MethodCallInstruction callInst = (MethodCallInstruction) inst;
						return _helper.GenerateIntrinsicMethod(_writer, (SpuIntrinsicMethod)callInst.Operand, childregs);
					}
				case IRCode.PpeCall:
					return _helper.WritePpeMethodCall(inst, childregs);
				case IRCode.Newobj:
					{
						Type cls = inst.OperandAsMethodCompiler.MethodBase.DeclaringType;
						StackTypeDescription std = new TypeDeriver().GetStackTypeDescription(cls);
						int byteSize = std.ComplexType.QuadWordCount*16;
						if (byteSize == 0)
						{
							// even though there are no fields, the object should take up just a little space.
							byteSize = 16;
						}
						VirtualRegister sizereg = _writer.WriteLoadI4(byteSize);
						VirtualRegister mem = _helper.WriteAllocateMemory(sizereg);
						_helper.WriteZeroMemory(mem, std.ComplexType.QuadWordCount);

						childregs.Insert(0, mem);
						_helper.WriteMethodCall((MethodCallInstruction)inst, childregs);

						return mem;
					}
				case IRCode.Call:
					return _helper.WriteMethodCall((MethodCallInstruction)inst, childregs);
				case IRCode.Callvirt:
					break;
				case IRCode.Calli:
					break;
				case IRCode.Ret:
					if (inst.StackType != StackTypeDescription.None)
						_writer.WriteMove(vrleft, HardwareRegister.GetHardwareRegister((int) CellRegister.REG_3));

					_writer.WriteReturn();
					return null;
				case IRCode.Br:
					WriteUnconditionalBranch(SpuOpCode.br, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Brfalse:
					WriteConditionalBranch(SpuOpCode.brz, vrleft, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Brtrue:
					WriteConditionalBranch(SpuOpCode.brnz, vrleft, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Beq:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							VirtualRegister vr1 = _writer.WriteCeq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr1, (IRBasicBlock) inst.Operand);
							return null;
						case CliType.Float32:
							VirtualRegister vr2 = _writer.WriteFceq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr2, (IRBasicBlock) inst.Operand);
							return null;
						case CliType.Int64:
						case CliType.Float64:
						case CliType.ObjectType:
						case CliType.ManagedPointer:
							break;
					}
					break;
				case IRCode.Bge:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A >= B) == !(B > A)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteCgt(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Bgt:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteCgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Ble:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A <= B) == !(A > B)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteCgt(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
								break;
							case CliType.Float64:
//								throw new Exception();
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Blt:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A < B) == (B > A)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteCgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Bne_Un:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteCeq(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Bge_Un:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A >= B) == !(B > A)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteClgt(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Bgt_Un:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteClgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Ble_Un:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A <= B) == !(A > B)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteClgt(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Blt_Un:
					{
						// NOTE: Asumes left and right operand is compatible
						// TODO: Not implemented for all valid types.
						// (A < B) == (B > A)
						VirtualRegister vr;
						switch (lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								vr = _writer.WriteClgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFcgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
							case CliType.ObjectType:
							case CliType.ManagedPointer:
								break;
						}
						break;
					}
				case IRCode.Switch:
					break;
				case IRCode.Ldind_I1:
				case IRCode.Ldind_U1:
					break;
				case IRCode.Ldind_I2:
				case IRCode.Ldind_U2:
					break;
				case IRCode.Ldind_I:
				case IRCode.Ldind_I4:
				case IRCode.Ldind_U4:
					{
						VirtualRegister ptr = vrleft;

						// Asssume (at least for now - 20070815) that the address is qw-aligned.
						return _writer.WriteLqd(ptr, 0);
					}
				case IRCode.Ldind_I8:
					break;
				case IRCode.Ldind_R4:
					break;
				case IRCode.Ldind_R8:
					break;
				case IRCode.Ldind_Ref:
					return _writer.WriteLqd(vrleft, 0);
				case IRCode.Stind_Ref:
					_writer.WriteStqd(vrleft, vrright, 0);
					return null;
				case IRCode.Stind_I1:
					break;
				case IRCode.Stind_I2:
					break;
				case IRCode.Stind_I4:
					{
						if (lefttype.IndirectionLevel != 1 && lefttype != StackTypeDescription.NativeInt)
							throw new InvalidIRTreeException("Invalid level of indirection for stind. Stack type: " + lefttype);
						VirtualRegister ptr = vrleft;

						_writer.WriteStoreWord(ptr, 0, 0, vrright);
						return null;
					}
				case IRCode.Stind_I8:
//					{
//						if (lefttype.IndirectionLevel != 1 && lefttype != StackTypeDescription.NativeInt)
//							throw new InvalidIRTreeException("Invalid level of indirection for stind. Stack type: " + lefttype);
//						VirtualRegister ptr = vrleft;
//
//						_writer.WriteStoreDoubleWord(ptr, 0, 0, vrright);
//						return null;
//					}
					break;
				case IRCode.Stind_R4:
					{
						if (lefttype.IndirectionLevel != 1 && lefttype != StackTypeDescription.NativeInt)
							throw new InvalidIRTreeException("Invalid level of indirection for stind. Stack type: " + lefttype);
						VirtualRegister ptr = vrleft;

						_writer.WriteStoreWord(ptr, 0, 0, vrright);
						return null;
					}
				case IRCode.Stind_R8:
//					{
//						if (lefttype.IndirectionLevel != 1 && lefttype != StackTypeDescription.NativeInt)
//							throw new InvalidIRTreeException("Invalid level of indirection for stind. Stack type: " + lefttype);
//						VirtualRegister ptr = vrleft;
//
//						_writer.WriteStoreDoubleWord(ptr, 0, 0, vrright);
//						return null;
//					}
					break;
				case IRCode.Add:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteA(vrleft, vrright);
						case CliType.Float32:
							return _writer.WriteFa(vrleft, vrright);
						case CliType.Float64:
							return _writer.WriteDfa(vrleft, vrright);
					}
					break;
				case IRCode.Sub:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteSf(vrright, vrleft);
						case CliType.Float32:
							return _writer.WriteFs(vrleft, vrright);
						case CliType.Float64:
							return _writer.WriteDfs(vrleft, vrright);
					}
					break;
				case IRCode.Mul:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							// "A 32-bit multiply instruction, mpy32 rt,ra,rb, can be 
							// emulated with the following instruction sequence:
							// mpyh t1,ra,rb
							// mpyh t2,rb,ra
							// mpyu t3,ra,rb
							// a    rt,t1,t2
							// a    rt,rt,t3"
							VirtualRegister t1 = _writer.WriteMpyh(vrleft, vrright);
							VirtualRegister t2 = _writer.WriteMpyh(vrright, vrleft);
							VirtualRegister t3 = _writer.WriteMpyu(vrleft, vrright);
							VirtualRegister rt = _writer.WriteA(t1, t2);
							_writer.WriteA(rt, rt, t3);
							return rt;
						case CliType.Float32:
							return _writer.WriteFm(vrleft, vrright);
						case CliType.Float64:
							return _writer.WriteDfm(vrleft, vrright);
					}
					break;
				case IRCode.Div:
				case IRCode.Div_Un:
					// Integer division is handled during IL reading, by replacing with a call to SpuMath.Div and SpuMath.Div_Un.
					switch (lefttype.CliType)
					{
//						case CliType.Int32: // Should have been replaced by a method call at this point.

						case CliType.Float32:
							{
								VirtualRegister r2 = vrright;
								VirtualRegister r3 = vrleft;
								VirtualRegister r4 = new VirtualRegister();
								VirtualRegister r5 = new VirtualRegister();
								VirtualRegister r6 = new VirtualRegister();

								_writer.WriteFrest(r5, r2);
								_writer.WriteFi(r5, r2, r5);

								_writer.WriteFm(r6, r3, r5);

								_writer.WriteFnms(r4, r6, r2, r3);
								_writer.WriteFma(r5, r4, r5, r6);

								_writer.WriteAi(r6, r5, 1);
								_writer.WriteFnms(r4, r2, r6, r3);
								_writer.WriteCgti(r4, r4, -1);
								_writer.WriteSelb(r4, r5, r6, r4);

								return r4;
							}
//						case CliType.Float64:
//							{
//								// TODO temporary implementation.
//								VirtualRegister r2 = _writer.WriteFrds(vrright); ;
//								VirtualRegister r3 = _writer.WriteFrds(vrleft); ;
//
//								VirtualRegister r4 = new VirtualRegister();
//								VirtualRegister r5 = new VirtualRegister();
//								VirtualRegister r6 = new VirtualRegister();
//
//								_writer.WriteFrest(r5, r2);
//								_writer.WriteFi(r5, r2, r5);
//
//								_writer.WriteFm(r6, r3, r5);
//
//								_writer.WriteFnms(r4, r6, r2, r3);
//								_writer.WriteFma(r5, r4, r5, r6);
//
//								_writer.WriteAi(r6, r5, 1);
//								_writer.WriteFnms(r4, r2, r6, r3);
//								_writer.WriteCgti(r4, r4, -1);
//								_writer.WriteSelb(r4, r5, r6, r4);
//
//								return r4;
//							}
					}

					break;
				case IRCode.Rem:
					break;
				case IRCode.Rem_Un:
					break;
				case IRCode.And:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteAnd(vrleft, vrright);
						case CliType.Int64:
							break;
					}
					break;
				case IRCode.Or:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteOr(vrleft, vrright);
						case CliType.Int64:
							break;
					}
					break;
				case IRCode.Xor:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteXor(vrleft, vrright);
						case CliType.Int64:
							break;
					}
					break;
				case IRCode.Shl:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
							return _writer.WriteShl(vrleft, vrright);
					}
					break;
				case IRCode.Shr:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							{
								if (righttype.CliType == CliType.Int32 || righttype.CliType == CliType.NativeInt)
								{
									VirtualRegister r = _writer.WriteSfi(vrright, 0);
									return _writer.WriteRotma(vrleft, r);
								}
							}
							break;
					}
					break;
				case IRCode.Shr_Un:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							{
								if (righttype.CliType == CliType.Int32 || righttype.CliType == CliType.NativeInt)
								{
									VirtualRegister r = _writer.WriteSfi(vrright, 0);
									return _writer.WriteRotm(vrleft, r);
								}
							}
							break;
					}
					break;
				case IRCode.Neg:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteSfi(vrleft, 0);
						case CliType.Float32:
							VirtualRegister r1 = _writer.WriteIl(0);
							VirtualRegister r2 = _writer.WriteCsflt(r1, 155);
							return _writer.WriteFs(r2, vrleft);
					}
					break;
				case IRCode.Not:
					break;
				case IRCode.Conv_I1:
					break;
				case IRCode.Conv_I2:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							VirtualRegister r1 = _writer.WriteIla(0xffff); //Loads 18 bits without sign extend.	
							VirtualRegister r2 = _writer.WriteAnd(r1, vrleft);
							return _writer.WriteXshw(r2);
					}
					break;
				case IRCode.Conv_I4:
					{
						switch(lefttype.CliType)
						{
							case CliType.Int32:
							case CliType.NativeInt:
								return vrleft;
							case CliType.Float32:
								return _helper.WriteCflts(_writer, vrleft);
							case CliType.Float64:
								{
									VirtualRegister r = _writer.WriteFrds(vrleft);
									return _helper.WriteCflts(_writer, r);
								}
						}
					}
					break;
				case IRCode.Conv_I8:
					break;
				case IRCode.Conv_R4:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
							return _helper.WriteCsflt(_writer, vrleft);
						case CliType.Float64:
							return _writer.WriteFrds(vrleft);
						case CliType.Int64:
							break;
					}
					break;
				case IRCode.Conv_R8:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
							VirtualRegister r = _helper.WriteCsflt(_writer, vrleft);
							return _writer.WriteFesd(r);
						case CliType.Float32:
							return _writer.WriteFesd(vrleft);
					}
					break;
				case IRCode.Conv_U4:
					break;
				case IRCode.Conv_U8:
					break;
				case IRCode.Cpobj:
					break;
				case IRCode.Ldobj:
					if (lefttype.CliType.Equals(CliType.ManagedPointer))
					{
						StackTypeDescription type = lefttype.Dereference();
						switch (type.CliType)
						{
							case CliType.Int32Vector:
							case CliType.Float32Vector:
								return _writer.WriteLqd(vrleft, 0);
							case CliType.ValueType:
								if (!type.IsImmutableSingleRegisterType)
								{
									// Copy to new stack position.
									int stackpos = _stackAllocate(type.ComplexType.QuadWordCount);
									for (int i = 0; i < type.ComplexType.QuadWordCount; i++)
									{
										VirtualRegister val = _writer.WriteLqd(vrleft, i);
										_writer.WriteStqd(val, HardwareRegister.SP, stackpos + i);
									}

									return _writer.WriteAi(HardwareRegister.SP, stackpos * 16);
								}
								else 
									break;
						}
					}
					break;
				case IRCode.Ldstr:
					break;
				case IRCode.Castclass:
					break;
				case IRCode.Isinst:
					break;
				case IRCode.Conv_R_Un:
					break;
				case IRCode.Unbox:
					break;
				case IRCode.Throw:
					break;
				case IRCode.Ldflda:
					break;
				case IRCode.Ldfld:
					{
						FieldInfo field = inst.OperandAsField;
						Type vt = field.DeclaringType;

						int qwoffset;
						int byteoffset;
						int fieldsize;
						_helper.GetStructFieldData(lefttype, vt, field, out qwoffset, out byteoffset, out fieldsize);

						if (lefttype.CliType == CliType.ManagedPointer && lefttype.Dereference().CliType == CliType.ValueType)
						{
							if (byteoffset == 0)
								return _writer.WriteLqd(vrleft, qwoffset);
							else if (byteoffset + fieldsize <= 16)
							{
								VirtualRegister r = _writer.WriteLqd(vrleft, qwoffset);
								return _writer.WriteShlqbyi(r, byteoffset);
							}
							else
							{
								VirtualRegister rqw1 = _writer.WriteLqd(vrleft, qwoffset);
								VirtualRegister rqw2 = _writer.WriteLqd(vrleft, qwoffset + 1);
								VirtualRegister r1 = _writer.WriteShlqbyi(rqw1, byteoffset);
								VirtualRegister r2 = _writer.WriteRotqmbyi(rqw2, -(16 - byteoffset));

								return _writer.WriteOr(r1, r2);
							}
						}
						else
						{
							Utilities.Assert(byteoffset == 0, "wordoffset == 0");
							return _writer.WriteLqd(vrleft, qwoffset);
						}
					}
				case IRCode.Stfld:
					{
						FieldInfo field = inst.OperandAsField;
						Type vt = field.DeclaringType;

						int qwoffset;
//						int wordoffset;
						int byteoffset;
						int fieldsize;
						_helper.GetStructFieldData(lefttype, vt, field, out qwoffset, out byteoffset, out fieldsize);

						if (lefttype.CliType == CliType.ManagedPointer && lefttype.Dereference().CliType == CliType.ValueType)
						{
							// if the field do NOT cross a quard word boundary.
							if (byteoffset + fieldsize <= 16)
							{
								switch (fieldsize)
								{
									case 4:
										_writer.WriteStoreWord(vrleft, qwoffset, byteoffset / 4, vrright);
										break;
									case 16:
										_writer.WriteStqd(vrright, vrleft, qwoffset);
										break;
									default:
										throw new NotSupportedException();
								}
								return null;
							}
							else
							{
								Utilities.Assert(fieldsize == 16, "fieldsize == 16");

								VirtualRegister qw1 = _writer.WriteLqd(vrleft, qwoffset);
								VirtualRegister qw2 = _writer.WriteLqd(vrleft, qwoffset + 1);

								if (byteoffset == 8)
								{
									VirtualRegister rcdd1 = _writer.WriteCdd(vrleft, byteoffset);
									VirtualRegister rcdd2 = _writer.WriteCdd(vrleft, byteoffset+8);

									_writer.WriteShufb(qw1, vrright, qw1, rcdd1);

									VirtualRegister r = _writer.WriteShlqbyi(vrright, 8);

									_writer.WriteShufb(qw2, r, qw2, rcdd2);
								}
								else if (byteoffset == 12 || byteoffset == 4)
								{
									VirtualRegister rcwd1 = _writer.WriteCwd(vrleft, byteoffset);
									VirtualRegister rcdd1 = _writer.WriteCdd(vrleft, byteoffset + 4);
									VirtualRegister rcwd2 = _writer.WriteCwd(vrleft, byteoffset + 12);

									_writer.WriteShufb(qw1, vrright, qw1, rcwd1);

									VirtualRegister r = _writer.WriteShlqbyi(vrright, 4);

									if (byteoffset == 4)
										_writer.WriteShufb(qw1, r, qw1, rcdd1);
									else
										_writer.WriteShufb(qw2, r, qw2, rcdd1);

									_writer.WriteShlqbyi(r, r, 8);

									_writer.WriteShufb(qw2, r, qw2, rcwd2);

								}
								_writer.WriteStqd(qw1, vrleft, qwoffset);
								_writer.WriteStqd(qw2, vrleft, qwoffset + 1);
							}
						}
						else
						{
							Utilities.Assert(byteoffset == 0, "byteoffset == 0");
							if (byteoffset == 0)
								_writer.WriteStqd(vrright, vrleft, qwoffset);
						}
						return null;
					}
				case IRCode.Ldsfld:
					break;
				case IRCode.Ldsflda:
					break;
				case IRCode.Stsfld:
					break;
				case IRCode.Stobj:
					if (lefttype.CliType == CliType.ManagedPointer)
					{
						StackTypeDescription desttype = lefttype.Dereference();

						switch (desttype.CliType)
						{
							case CliType.Int32Vector:
							case CliType.Float32Vector:
								_writer.WriteStqd(vrright, vrleft, 0);
								return null;
							case CliType.ValueType:
								if (!desttype.IsImmutableSingleRegisterType)
								{
									for (int i = 0; i < desttype.ComplexType.QuadWordCount; i++)
									{
										VirtualRegister w = _writer.WriteLqd(vrright, i);
										_writer.WriteStqd(w, vrleft, i);
									}
								}
								else
								{
									_writer.WriteStqd(vrright, vrleft, 0);
								}
								return null;
						}
					}
					break;
				case IRCode.Conv_Ovf_I1_Un:
					break;
				case IRCode.Conv_Ovf_I2_Un:
					break;
				case IRCode.Conv_Ovf_I4_Un:
					break;
				case IRCode.Conv_Ovf_I8_Un:
					break;
				case IRCode.Conv_Ovf_U1_Un:
					break;
				case IRCode.Conv_Ovf_U2_Un:
					break;
				case IRCode.Conv_Ovf_U4_Un:
					break;
				case IRCode.Conv_Ovf_U8_Un:
					break;
				case IRCode.Conv_Ovf_I_Un:
					break;
				case IRCode.Conv_Ovf_U_Un:
					break;
				case IRCode.Box:
					break;
				case IRCode.Newarr:
					{
						StackTypeDescription elementtype = (StackTypeDescription) inst.Operand;
						VirtualRegister elementcount = vrleft;


						// Determine byte size.
						int elementByteSize = elementtype.GetSizeWithPadding();
						VirtualRegister bytesize;
						if (elementByteSize == 4)
							bytesize = _writer.WriteShli(elementcount, 2);
						else if (elementByteSize == 16)
							bytesize = _writer.WriteShli(elementcount, 4);
						else if (elementByteSize == 8)
							bytesize = _writer.WriteShli(elementcount, 3);
						else
							throw new NotSupportedException("Element size of " + elementByteSize + " is not supported.");

						_writer.WriteAi(bytesize, bytesize, 16); // makes room for the arraysize.

						// Sets the size to be 16 bytes aligned. Note if the size is 16 bytes aligned, there will be allocated 16 extra bytes.
						_writer.WriteAndi(bytesize, bytesize, 0xfff0); // Note the immediated field is no more than 16 bits wide.
						_writer.WriteAi(bytesize, bytesize, 16);

						VirtualRegister array = _helper.WriteAllocateMemory(bytesize);

						// make the arraypointer point to the first element.
						_writer.WriteAi(array, array, 16);

						// Initialize array length field.
						_writer.WriteStqd(elementcount, array, -1);

						return array;
					}
				case IRCode.Ldlen:
					{
						VirtualRegister arr = vrleft;
						// Array length is stored in the quadword just before the array.
						return _writer.WriteLqd(arr, -1);
					}
				case IRCode.Ldelema:
					{
						VirtualRegister arr = vrleft;
						VirtualRegister index = vrright;
						StackTypeDescription elementtype = (StackTypeDescription) inst.Operand;
						int elementsize = elementtype.GetSizeWithPadding();

						VirtualRegister byteoffset;
						if (elementsize == 4)
							byteoffset = _writer.WriteShli(index, 2);
						else if (elementsize == 8)
							byteoffset = _writer.WriteShli(index, 3);
						else if (elementsize == 16)
							byteoffset = _writer.WriteShli(index, 4);
						else
							throw new NotImplementedException();

						return _writer.WriteA(arr, byteoffset);
					}
				case IRCode.Ldelem_I1:
				case IRCode.Ldelem_U1:
					break;
				case IRCode.Ldelem_I2:
				case IRCode.Ldelem_U2:
					break;
				case IRCode.Ldelem_I4:
				case IRCode.Ldelem_U4:
				case IRCode.Ldelem_I:
				case IRCode.Ldelem_R4:
					{
						VirtualRegister array = vrleft;
						VirtualRegister index = vrright;

						// Load.
						VirtualRegister byteoffset = _writer.WriteShli(index, 2);
						VirtualRegister quad = _writer.WriteLqx(array, byteoffset);

						// Move word to preferred slot.
						// We're going to use shlqby (Shift Left Quadword by Bytes),
						// so we have to clear bit 27 from the byte offset.
						VirtualRegister addrMod16 = _writer.WriteAndi(byteoffset, 0xf);
						return _writer.WriteShlqby(quad, addrMod16);
					}
				case IRCode.Ldelem_I8:
				case IRCode.Ldelem_R8:
//					{
//						VirtualRegister array = vrleft;
//						VirtualRegister index = vrright;
//
//						// Load.
//						VirtualRegister byteoffset = _writer.WriteShli(index, 3);
//						VirtualRegister quad = _writer.WriteLqx(array, byteoffset);
//
//						// Move word to preferred slot.
//						// We're going to use shlqby (Shift Left Quadword by Bytes),
//						// so we have to clear bit 27 from the byte offset.
//						VirtualRegister addrMod16 = _writer.WriteAndi(byteoffset, 0xf);
//						return _writer.WriteShlqby(quad, addrMod16);
//					}
					break;
				case IRCode.Ldelem_Ref:
					break;
				case IRCode.Stelem_I:
				case IRCode.Stelem_I1:
				case IRCode.Stelem_I2:
				case IRCode.Stelem_I4:
				case IRCode.Stelem_I8:
				case IRCode.Stelem_R4:
				case IRCode.Stelem_R8:
				case IRCode.Stelem_Ref:
					throw new InvalidIRTreeException("stelem instruction encountered.");
//				case IRCode.Ldelem_Any:
//					break;
//				case IRCode.Stelem_Any:
//					break;
				case IRCode.Unbox_Any:
					break;
				case IRCode.Conv_Ovf_I1:
					break;
				case IRCode.Conv_Ovf_U1:
					break;
				case IRCode.Conv_Ovf_I2:
					break;
				case IRCode.Conv_Ovf_U2:
					break;
				case IRCode.Conv_Ovf_I4:
					break;
				case IRCode.Conv_Ovf_U4:
					break;
				case IRCode.Conv_Ovf_I8:
					break;
				case IRCode.Conv_Ovf_U8:
					break;
				case IRCode.Refanyval:
					break;
				case IRCode.Ckfinite:
					break;
				case IRCode.Mkrefany:
					break;
				case IRCode.Ldtoken:
					break;
				case IRCode.Conv_U2:
					break;
				case IRCode.Conv_U1:
					break;
				case IRCode.Conv_I:
					return vrleft;
				case IRCode.Conv_Ovf_I:
					break;
				case IRCode.Conv_Ovf_U:
					break;
				case IRCode.Add_Ovf:
					break;
				case IRCode.Add_Ovf_Un:
					break;
				case IRCode.Mul_Ovf:
					break;
				case IRCode.Mul_Ovf_Un:
					break;
				case IRCode.Sub_Ovf:
					break;
				case IRCode.Sub_Ovf_Un:
					break;
				case IRCode.Endfinally:
					break;
				case IRCode.Leave:
					break;
				case IRCode.Leave_S:
					break;
				case IRCode.Stind_I:
					break;
				case IRCode.Conv_U:
					break;
				case IRCode.Arglist:
					break;
				case IRCode.Ceq:
					{
						VirtualRegister val;
						switch (lefttype.CliType)
						{
//								val = _writer.WriteCeqb(vrleft, vrright);
//								return _writer.WriteAndi(val, 1);
//								val = _writer.WriteCeqh(vrleft, vrright);
//								return _writer.WriteAndi(val, 1);
							case CliType.ObjectType:
							case CliType.Int32:
								val = _writer.WriteCeq(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Float32:
								val = _writer.WriteFceq(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Float64:
								return _helper.WriteCeqFloat64(vrleft, vrright);
							case CliType.Int64:
								break;
						}
						break;
					}
				case IRCode.Cgt:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.Int32:
							if (righttype.CliType == CliType.NativeInt || righttype.CliType == CliType.Int32)
							{
								VirtualRegister val = _writer.WriteCgt(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							}
							break;
						case CliType.Float32:
							if (righttype.CliType == CliType.Float32)
							{
								VirtualRegister val = _writer.WriteFcgt(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							}
							break;
//						case CliType.Float64:
//							return WriteCltFloat64(vrright, vrleft);
					}
					break;
				case IRCode.Cgt_Un:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.Int32:
							if (righttype.CliType == CliType.NativeInt || righttype.CliType == CliType.Int32)
							{
								VirtualRegister val = _writer.WriteClgt(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							}
							break;
						case CliType.Float32:
							if (righttype.CliType == CliType.Float32)
							{
								VirtualRegister val = _writer.WriteFcgt(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							}
							break;
//						case CliType.Float64:
//							return WriteCltFloat64(vrright, vrleft);
					}
					break;
				case IRCode.Clt:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.Int32:
							{
								VirtualRegister val = _writer.WriteCgt(vrright, vrleft);
								return _writer.WriteAndi(val, 1);
							}
						case CliType.Float32:
							if (righttype.CliType == CliType.Float32)
							{
								VirtualRegister val = _writer.WriteFcgt(vrright, vrleft);
								return _writer.WriteAndi(val, 1);
							}
							break;
						case CliType.Float64:
							return _helper.WriteCltFloat64(vrleft, vrright);
					}
					break;
				case IRCode.Clt_Un:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.Int32:
							if (righttype.CliType == CliType.NativeInt || righttype.CliType == CliType.Int32)
							{
								VirtualRegister val = _writer.WriteClgt(vrright, vrleft);
								return _writer.WriteAndi(val, 1);
							}
							break;
						case CliType.Float32:
							if (righttype.CliType == CliType.Float32)
							{
								VirtualRegister val = _writer.WriteFcgt(vrright, vrleft);
								return _writer.WriteAndi(val, 1);
							}
							break;
//						case CliType.Float64:
//							return WriteCltFloat64(vrleft, vrright);
					}
					break;
				case IRCode.Ldftn:
					break;
				case IRCode.Ldvirtftn:
					break;
				case IRCode.Ldarg:
				case IRCode.Ldloc:
					{
						MethodVariable var = ((MethodVariable)inst.Operand);
						if (var.StackType.IsStackValueType)
						{
							// Save to new stack location.
							int count = var.StackType.ComplexType.QuadWordCount;
							int newstackloc = _stackAllocate(count);
							VirtualRegister stackptr = _writer.WriteAi(HardwareRegister.SP, newstackloc * 16);
							_helper.WriteMoveQuadWords(HardwareRegister.SP, var.StackLocation, HardwareRegister.SP, newstackloc, count);
							return stackptr;
						}
						else if (var.Escapes.GetValueOrDefault(false))
						{
							_writer.WriteLqd(var.VirtualRegister, HardwareRegister.SP, var.StackLocation);
							return var.VirtualRegister;
						}
						else
							return var.VirtualRegister;
					}
				case IRCode.Ldarga:
				case IRCode.Ldloca:
					{
						MethodVariable var = ((MethodVariable) inst.Operand);
						if (var.StackType.IsStackValueType || var.Escapes.GetValueOrDefault(true))
							return _writer.WriteAi(HardwareRegister.SP, var.StackLocation*16);
						else
							throw new InvalidIRTreeException("Escaping variable with no stack location.");
					}
				case IRCode.Starg:
				case IRCode.Stloc:
					{
						MethodVariable var = ((MethodVariable) inst.Operand);
						if (var.StackType.IsStackValueType)
							_helper.WriteMoveQuadWords(vrleft, 0, HardwareRegister.SP, var.StackLocation, var.StackType.ComplexType.QuadWordCount);
						else if (var.Escapes.GetValueOrDefault(false))
							_writer.WriteStqd(vrleft, HardwareRegister.SP, var.StackLocation);
						else
							_writer.WriteMove(vrleft, var.VirtualRegister);

						return null;
					}
				case IRCode.Localloc:
				case IRCode.Endfilter:
				case IRCode.Unaligned:
				case IRCode.Volatile:
//				case IRCode.Tail:
				case IRCode.Initobj:
					{
						StackTypeDescription t = (StackTypeDescription) inst.Operand;

						int slotcount = t.ComplexType.QuadWordCount;
						_helper.WriteZeroMemory(vrleft, slotcount);
						return null;
					}
				case IRCode.Constrained:
				case IRCode.Cpblk:
				case IRCode.Initblk:
//				case IRCode.No:
				case IRCode.Rethrow:
				case IRCode.Sizeof:
				case IRCode.Refanytype:
				case IRCode.Readonly:
					break;
				default:
					throw new InvalidIRTreeException("Invalid opcode: " + ilcode);
			}

			_unimplementedOpCodes.Add(inst.Opcode);
			return new VirtualRegister(-1);
		}

		private void WriteUnconditionalBranch(SpuOpCode branchopcode, IRBasicBlock target)
		{
			_writer.WriteBranch(branchopcode);
			_branchInstructions.Add(new KeyValuePair<SpuInstruction, IRBasicBlock>(_writer.LastInstruction, target));
		}

		private void WriteConditionalBranch(SpuOpCode branchopcode, VirtualRegister conditionregister, IRBasicBlock target)
		{
			_writer.WriteBranch(branchopcode);
			_writer.LastInstruction.Rt = conditionregister;
			_branchInstructions.Add(new KeyValuePair<SpuInstruction, IRBasicBlock>(_writer.LastInstruction, target));
		}

		private void WriteConditionalBranch(SpuOpCode branchopcode, VirtualRegister conditionregister, SpuBasicBlock target)
		{
			_writer.WriteBranch(branchopcode);
			_writer.LastInstruction.Rt = conditionregister;
			_writer.LastInstruction.JumpTarget = target;
			//			_branchInstructions.Add(new KeyValuePair<SpuInstruction, IRBasicBlock>(_writer.LastInstruction, target));
		}
	}
}
