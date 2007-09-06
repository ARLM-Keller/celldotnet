using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CellDotNet
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

		private SpecialSpeObjects _specialSpeObjects;


		public RecursiveInstructionSelector()
		{
		}

		public RecursiveInstructionSelector(SpecialSpeObjects specialSpeObjects)
		{
			Utilities.AssertArgumentNotNull(specialSpeObjects, "specialSpeObjects");
			_specialSpeObjects = specialSpeObjects;
		}

		public void GenerateCode(List<IRBasicBlock> basicBlocks, ReadOnlyCollection<MethodParameter> parameters, SpuInstructionWriter writer)
		{
			_writer = writer;
			_basicBlocks = basicBlocks;

			_parameters = parameters;

			_unimplementedOpCodes = new List<IROpCode>();
			
			// These two are used to patch up branch instructions after instruction selection.
			_branchInstructions = new List<KeyValuePair<SpuInstruction, IRBasicBlock>>();
			_spubasicblocks = new Dictionary<IRBasicBlock, SpuBasicBlock>();

			WriteFirstBasicBlock();

			foreach (IRBasicBlock bb in _basicBlocks)
			{
				_writer.BeginNewBasicBlock();
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
					"The instructions are:\r\n", _unimplementedOpCodes.Count);

				List<string> ocnames = _unimplementedOpCodes.ConvertAll<string>(
					delegate(IROpCode input) { return input.Name; });
				msg += string.Join(", ", ocnames.ToArray()) + ".";

				throw new NotImplementedException(msg);
			}

			foreach (KeyValuePair<SpuInstruction, IRBasicBlock> pair in _branchInstructions)
			{
				SpuBasicBlock target;

				target = _spubasicblocks[pair.Value];
				pair.Key.JumpTarget = target;
			}
		}


		/// <summary>
		/// Creates the first basic block of the method, which moves arguments from physical
		/// registers to virtual registers.
		/// </summary>
		private void WriteFirstBasicBlock()
		{
			const int FirstArgumentRegister = 3;

			if (_parameters.Count > 72)
				throw new NotSupportedException("More than 72 arguments is not supported.");
			if (_parameters.Count == 0)
				return;

			_writer.BeginNewBasicBlock();
			for (int i = 0; i < _parameters.Count; i++)
			{
				MethodParameter parameter = _parameters[i];

				VirtualRegister src = HardwareRegister.GetHardwareRegister(FirstArgumentRegister + i);

				_writer.WriteMove(src, parameter.VirtualRegister);
			}
		}

		static private unsafe uint ReinterpretAsUInt(float f)
		{
			return *((uint*) &f);
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
//					VirtualRegister dbgr = _writer.WriteLoadI4((int) inst.Operand);
//					_writer.WriteDebugStop(dbgr, _specialSpeObjects.DebugValueObject);
//					return dbgr;
				case IRCode.Ldc_I8:
					break;
				case IRCode.Ldc_R4:
					{
						uint val = ReinterpretAsUInt((float) inst.Operand);
						VirtualRegister reg = _writer.WriteIlhu((int) ((val >> 16) & 0xffff));
						_writer.WriteIohl(reg, (int) (val & 0xffff));
						return reg;
					}
				case IRCode.Ldc_R8:
					break;
				case IRCode.Dup:
				case IRCode.Pop:
					// Does it make sense that these two are IR instructions?
					break;
				case IRCode.Jmp:
					break;
				case IRCode.SpuInstructionMethodCall:
					{
						MethodCallInstruction callInst = (MethodCallInstruction)inst;
						return IntrinsicsWriter.GenerateSpuInstructionMethod(_writer, callInst, childregs);
					}
				case IRCode.IntrinsicMethodCall:
					{
						MethodCallInstruction callInst = (MethodCallInstruction)inst;
						return IntrinsicsWriter.GenerateIntrinsicMethod(_writer, (SpuIntrinsicMethod)callInst.Operand, childregs);
					}
				case IRCode.Call:
					{
						MethodCallInstruction callInst = (MethodCallInstruction) inst;
						MethodCompiler target = callInst.TargetMethodCompiler;

						if (target.MethodBase.IsConstructor)
							throw new NotImplementedException("Constructors are not implemented.");
						if (!target.MethodBase.IsStatic)
							throw new NotImplementedException("Only static methods are implemented.");
						if(target.Parameters.Count > HardwareRegister.CallerSavesVirtualRegisters.Length)
							throw new NotImplementedException("No support for more than " + HardwareRegister.CallerSavesVirtualRegisters.Length + "parameters.");

						// Move parameters into hardware registers.
						for (int i = 0; i < target.Parameters.Count; i++)
							_writer.WriteMove(childregs[i], HardwareRegister.GetHardwareArgumentRegister(i));

//						VirtualRegister r = _writer.WriteLoadAddress(target);

						_writer.WriteBrsl(target);

						if (inst.StackType != StackTypeDescription.None)
							return HardwareRegister.GetHardwareRegister(3);
						else
							return null;
					}
				case IRCode.Callvirt:
					break;
				case IRCode.Calli:
					break;
				case IRCode.Ret:
					if (inst.StackType != StackTypeDescription.None)
					{
						_writer.WriteMove(vrleft, HardwareRegister.GetHardwareRegister((int) CellRegister.REG_3));
						_writer.WriteReturn();
					}
					return null;
				case IRCode.Br:
					WriteUnconditionalBranch(SpuOpCode.br, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Brfalse:
					WriteConditionalBranch(SpuOpCode.brz, vrleft, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Brtrue:
					WriteConditionalBranch(SpuOpCode.brnz, vrleft, (IRBasicBlock)inst.Operand);
					return null;
				case IRCode.Beq:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							VirtualRegister vr1 = _writer.WriteCeq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr1, (IRBasicBlock)inst.Operand);
							return null;
						case CliType.Float32:
							VirtualRegister vr2 = _writer.WriteFceq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr2, (IRBasicBlock)inst.Operand);
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
								vr = _writer.WriteFceq(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								vr = _writer.WriteFceq(vrleft, vrright);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.Float64:
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrright, vrleft);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								_writer.WriteXori(vr, vr, 0xfffffff);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
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
					break;
				case IRCode.Stind_Ref:
					break;
				case IRCode.Stind_I1:
					break;
				case IRCode.Stind_I2:
					break;
				case IRCode.Stind_I4:
					{
						if (lefttype.IndirectionLevel != 1 && lefttype != StackTypeDescription.NativeInt)
							throw new InvalidIRTreeException("Invalid level of indirection for stind. Stack type: " + lefttype);
						VirtualRegister ptr = vrleft;

						VirtualRegister loadedvalue = _writer.WriteLqd(ptr, 0);
						VirtualRegister mask = _writer.WriteCwd(ptr, 0);
						VirtualRegister combined = _writer.WriteShufb(vrright, loadedvalue, mask);
						_writer.WriteStqd(combined, ptr, 0);
						return null;
					}
				case IRCode.Stind_I8:
					break;
				case IRCode.Stind_R4:
					break;
				case IRCode.Stind_R8:
					break;
				case IRCode.Add:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteA(vrleft, vrright);
					}
					break;
				case IRCode.Sub:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteSf(vrright, vrleft);
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
					}
					break;
				case IRCode.Div:
					break;
				case IRCode.Div_Un:
// TODO FIXME
//					VirtualRegister result = new VirtualRegister();
//					_writer.WriteDivU(vrleft, vrright, result, new VirtualRegister());
//					return result;
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
					break;
				case IRCode.Shr:
					break;
				case IRCode.Shr_Un:
					break;
				case IRCode.Neg:
					break;
				case IRCode.Not:
					break;
				case IRCode.Conv_I1:
					break;
				case IRCode.Conv_I2:
					break;
				case IRCode.Conv_I4:
					{
						StackTypeDescription srctype = lefttype;
						if (srctype.CliBasicType == CliBasicType.NativeInt)
							return vrleft;

						throw new NotSupportedException();
					}
				case IRCode.Conv_I8:
					break;
				case IRCode.Conv_R4:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.NativeInt:
							return _writer.WriteCsflt(vrleft, 0);
						case CliType.Int64:
							break;
					}
					break;
				case IRCode.Conv_R8:
					break;
				case IRCode.Conv_U4:
					break;
				case IRCode.Conv_U8:
					break;
				case IRCode.Cpobj:
					break;
				case IRCode.Ldobj:
					break;
				case IRCode.Ldstr:
					break;
				case IRCode.Newobj:
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
				case IRCode.Ldfld:
					break;
				case IRCode.Ldflda:
					break;
				case IRCode.Stfld:
					break;
				case IRCode.Ldsfld:
					break;
				case IRCode.Ldsflda:
					break;
				case IRCode.Stsfld:
					break;
				case IRCode.Stobj:
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
						VirtualRegister bytesize;
						
						// 1: Determine number of required bytes.
						// 2: Verify that we've got space for the allocation.
						// 3: Update available space counter.
						// 4: Get address for the array.
						// 5: Initialize array length field.
						// 6: Update pointer for next allocation.

						if (_specialSpeObjects == null)
							throw new InvalidOperationException("_specialSpeObjects == null");


						// Determine byte size.
						int elementByteSize = elementtype.GetSizeWithPadding();
						if (elementByteSize == 4)
							bytesize = _writer.WriteShli(elementcount, 2);
						else
							throw new NotSupportedException("Element size of " + elementByteSize + " is not supported.");

						_writer.WriteAi(bytesize, bytesize, 16); // makes room for the arraysize.

						// Sets the size to be 16 bytes aligned. Note if the size is 16 bytes aligned, there will be allocated 16 extra bytes.
						_writer.WriteAndi(bytesize, bytesize, 0xfff0);//Note the immediated field is no more than 16 bits wide.
						_writer.WriteAi(bytesize, bytesize, 16);

						// Subtract from available byte count.
						{
							VirtualRegister allocatableByteCount = _writer.WriteLoad(_specialSpeObjects.AllocatableByteCountObject);
							_writer.WriteSf(allocatableByteCount, bytesize, allocatableByteCount);

							// If there isn't enough space, then halt by branching to the OOM routine.
							VirtualRegister isFreeSpacePositive = _writer.WriteCgti(allocatableByteCount, 0);
							_writer.WriteConditionalBranch(SpuOpCode.brz, isFreeSpacePositive, _specialSpeObjects.OutOfMemory);

							// Store new allocatable byte count.
							_writer.WriteStore(allocatableByteCount, _specialSpeObjects.AllocatableByteCountObject);
						}

						VirtualRegister nextAllocAddress = _writer.WriteLoad(_specialSpeObjects.NextAllocationStartObject);
						VirtualRegister array = nextAllocAddress;

						// Increment the pointer for the next allocation.
						{
							VirtualRegister newNextAllocAddress = new VirtualRegister();
							_writer.WriteA(newNextAllocAddress, nextAllocAddress, bytesize);
							_writer.WriteStore(newNextAllocAddress, _specialSpeObjects.NextAllocationStartObject);
						}
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
							case CliType.Int32:
								val = _writer.WriteCeq(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Float32:
							case CliType.Float64:
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
							VirtualRegister val = _writer.WriteCgt(vrleft, vrright);
							return _writer.WriteAndi(val, 1);
					}
					break;
				case IRCode.Cgt_Un:
					break;
				case IRCode.Clt:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.Int32:
							return _writer.WriteCgt(vrright, vrleft);
					}
					break;
				case IRCode.Clt_Un:
					break;
				case IRCode.Ldftn:
					break;
				case IRCode.Ldvirtftn:
					break;
				case IRCode.Ldarg:
					{
						MethodVariable var = ((MethodVariable)inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
						{
							_writer.WriteLqd(var.VirtualRegister, HardwareRegister.SP, var.StackLocation);
							return var.VirtualRegister;
						}
						else
							return var.VirtualRegister;
						// Do nothing.
//						return GetMethodVariableRegister(inst);
					}
				case IRCode.Ldarga:
					{
						MethodVariable var = ((MethodVariable)inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
						{
							Utilities.Assert(var.StackLocation * 4 < 32*1024, "Immediated overflow");
							VirtualRegister r = _writer.WriteIl(var.StackLocation * 16);
							_writer.WriteA(r, HardwareRegister.SP, r);
							return r;
						}
						else
							throw new Exception("Escaping variable with no stack location.");
					}
				case IRCode.Starg:
					{
						MethodVariable var = ((MethodVariable)inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
							_writer.WriteStqd(vrleft, HardwareRegister.SP, var.StackLocation);
						else
							_writer.WriteMove(vrleft, var.VirtualRegister);

						return null;
					}
				case IRCode.Ldloc:
					{
						MethodVariable var = ((MethodVariable) inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
						{
							_writer.WriteLqd(var.VirtualRegister, HardwareRegister.SP, var.StackLocation);
							return var.VirtualRegister;
						}
						else
							return var.VirtualRegister;
					}
				case IRCode.Ldloca:
					{
						MethodVariable var = ((MethodVariable) inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
						{
							Utilities.Assert(var.StackLocation * 4 < 32 * 1024, "Immediated overflow");
							VirtualRegister r = _writer.WriteIl(var.StackLocation * 16);
							_writer.WriteA(r, HardwareRegister.SP, r);
							return r;
						}
						else
							throw new Exception("Escaping variable with no stack location.");
					}
				case IRCode.Stloc:
					{
						MethodVariable var = ((MethodVariable) inst.Operand);
						if (var.Escapes != null && var.Escapes.Value)
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



		private enum MfcDmaCommand
		{
			Put = 0x0020,
//			PutS = 0x0028, /*  PU Only */
			PutR = 0x0030,
			PutF = 0x0022,
			PutB = 0x0021,
//			PutFS = 0x002A, /*  PU Only */
//			PutBS = 0x0029, /*  PU Only */
			PutRF = 0x0032,
			PutRB = 0x0031,
			PutL = 0x0024, /* SPU Only */
			PutRL = 0x0034, /* SPU Only */
			PutLF = 0x0026, /* SPU Only */
			PutLB = 0x0025, /* SPU Only */
			PutRLF = 0x0036, /* SPU Only */
			PutRLB = 0x0035, /* SPU Only */

			Get = 0x0040,
//			GetS = 0x0048, /*  PU Only */
			GetF = 0x0042,
			GetB = 0x0041,
//			GetFS = 0x004A, /*  PU Only */
//			GetBS = 0x0049, /*  PU Only */
			GetL = 0x0044, /* SPU Only */
			GetLF = 0x0046, /* SPU Only */
			GetLB = 0x0045, /* SPU Only */
		}


		class IntrinsicsWriter
		{
			public static VirtualRegister GenerateSpuInstructionMethod(
				SpuInstructionWriter writer, MethodCallInstruction inst, List<VirtualRegister> childregs)
			{
				MethodBase method = inst.IntrinsicMethod;
				ParameterInfo[] parr = method.GetParameters();
				SpuInstructionPart partsSoFar = SpuInstructionPart.None;
				SpuInstruction spuinst = new SpuInstruction(inst.OperandSpuOpCode);

				MethodInfo mi = method as MethodInfo;
				if (mi == null)
				{
					// Constructor.
					throw new NotSupportedException();
				}

				SpuInstructionPart necessaryParts = spuinst.OpCode.Parts;

				// Assign parameters.
				for (int i = 0; i < parr.Length; i++)
				{
					object[] atts = parr[i].GetCustomAttributes(typeof (SpuInstructionPartAttribute), false);
					if (atts.Length == 0)
						throw new InvalidInstructionParametersException("Method: " + method.Name);
					SpuInstructionPartAttribute att = (SpuInstructionPartAttribute) atts[0];

					// Make sure that it's not a reassignment.
					if ((partsSoFar | att.Part) == partsSoFar)
						throw new InvalidInstructionParametersException("Same instruction part applied to multiple parameters.");
					partsSoFar |= att.Part;

					// TODO: Check that the opcode actually uses the parts that we give it.
					// TODO: There should be a way for the tree builder to fix any immediate part so that we can assign it here.
					// TODO: Should probably move some of this logic to the tree builder.
					switch (att.Part)
					{
						case SpuInstructionPart.Rt:
							spuinst.Rt = childregs[i];
							break;
						case SpuInstructionPart.Ra:
							spuinst.Ra = childregs[i];
							break;
						case SpuInstructionPart.Rb:
							spuinst.Rb = childregs[i];
							break;
						case SpuInstructionPart.Rc:
							spuinst.Rc = childregs[i];
							break;
						case SpuInstructionPart.Sa:
						case SpuInstructionPart.Ca:
						case SpuInstructionPart.Immediate:
							int hasThisExtraParam = ((int)(method.CallingConvention & CallingConventions.HasThis) != 0) ? 1 : 0;
							if (inst.Parameters[hasThisExtraParam + i].Opcode == IROpCodes.Ldc_I4)
								spuinst.Constant = (int)inst.Parameters[hasThisExtraParam + i].Operand;
							else
								throw new InvalidInstructionParametersException("Spu instruction method requeries a constant.");
							break;
						default:
							throw new InvalidInstructionParametersException();
					}
				}

				VirtualRegister returnRegister;

				// Assign optional return register.
				{
					writer.AddInstructionManually(spuinst);

					SpuInstructionPartAttribute att;
					if (mi.ReturnType == typeof (void))
						returnRegister = null;
					else
					{
						object[] retAtts = mi.ReturnParameter.GetCustomAttributes(typeof (SpuInstructionPartAttribute), false);
						if (retAtts.Length != 1)
							throw new InvalidInstructionParametersException();

						att = (SpuInstructionPartAttribute) retAtts[0];
						switch (att.Part)
						{
							case SpuInstructionPart.Rt:
								spuinst.Rt = writer.NextRegister();
								returnRegister = spuinst.Rt;
								partsSoFar |= SpuInstructionPart.Rt;
								break;
							default:
								throw new NotSupportedException();
						}
					}
				}

				if (partsSoFar != necessaryParts)
					throw new InvalidInstructionParametersException(
						"Not all necessary instruction parts are mapped. Missing parts: " + (necessaryParts & ~partsSoFar) + ".");

				return returnRegister;
			}

			public static VirtualRegister GenerateIntrinsicMethod(SpuInstructionWriter writer, SpuIntrinsicMethod method, List<VirtualRegister> childregs)
			{
				switch (method)
				{
					case SpuIntrinsicMethod.Runtime_Stop:
						writer.WriteStop();
						return null;
					case SpuIntrinsicMethod.Mfc_GetAvailableQueueEntries:
						return writer.WriteRdchcnt(SpuWriteChannel.MFC_CmdAndClassID);
					case SpuIntrinsicMethod.Mfc_Get:
						WriteMfcDmaCommand(writer, MfcDmaCommand.Get, childregs);
						return null;
					case SpuIntrinsicMethod.Mfc_Put:
						WriteMfcDmaCommand(writer, MfcDmaCommand.Put, childregs);
						return null;
					case SpuIntrinsicMethod.MainStorageArea_get_EffectiveAddress:
						// The address is the only component.
						return childregs[0];
					default:
						throw new ArgumentException();
				}
			}

			private static void WriteMfcDmaCommand(SpuInstructionWriter writer, MfcDmaCommand cmd, List<VirtualRegister> arguments)
			{
				// These must match the order of the mfc dma method arguments.
				VirtualRegister ls = arguments[0];
				VirtualRegister ea = arguments[1];
				VirtualRegister size = arguments[2];
				VirtualRegister tag = arguments[3];
				VirtualRegister tid = arguments[4];
				VirtualRegister rid = arguments[5];

				writer.WriteWrch(SpuWriteChannel.MFC_LSA, ls);
				writer.WriteWrch(SpuWriteChannel.MFC_EAL, ea);
				writer.WriteWrch(SpuWriteChannel.MFC_Size, size);
				writer.WriteWrch(SpuWriteChannel.MFC_TagID, tag);

				// Combine tid, rid and cmd into a cmd-and-class-id-word.
				// Formula: (tid << 24) | (rid << 16) | cmd)

				VirtualRegister cmdReg = writer.WriteLoadI4((int) cmd);
				VirtualRegister tid2 = writer.WriteShli(tid, 24);
				VirtualRegister rid2 = writer.WriteShli(rid, 16);

				VirtualRegister or1 = writer.WriteOr(cmdReg, tid2);
				VirtualRegister finalCmd = writer.WriteOr(or1, rid2);

				writer.WriteWrch(SpuWriteChannel.MFC_CmdAndClassID, finalCmd);
			}
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

		private static VirtualRegister GetMethodVariableRegister(TreeInstruction inst)
		{
			if (!(inst.Operand is MethodVariable))
				throw new InvalidOperationException();

			MethodVariable var = (MethodVariable) inst.Operand;
			Utilities.AssertNotNull(var.VirtualRegister, "var.VirtualRegister");
			return var.VirtualRegister;
		}
	}
}
