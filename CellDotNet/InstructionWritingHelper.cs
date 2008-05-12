using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using CellDotNet.Intermediate;
using CellDotNet.Spe;

namespace CellDotNet
{
	class InstructionWritingHelper
	{
		private readonly SpuInstructionWriter _writer;
		private readonly SpecialSpeObjects _specialSpeObjects;
		private readonly StackSpaceAllocator _stackAllocate;


		public InstructionWritingHelper(SpuInstructionWriter writer, SpecialSpeObjects specialSpeObjects, StackSpaceAllocator stackAllocate)
		{
			_specialSpeObjects = specialSpeObjects;
			_stackAllocate = stackAllocate;
			_writer = writer;
		}

		public VirtualRegister WriteCflts(SpuInstructionWriter writer, VirtualRegister vrleft)
		{
			return writer.WriteCflts(vrleft, 173);
		}

		/// <summary>
		/// Writes loads and stores to move the requested number of quadwords from <paramref name="sourceAddress"/> 
		/// to <paramref name="destinationAddress"/>.
		/// </summary>
		public void WriteMoveQuadWords(VirtualRegister sourceAddress, int sourceQwOffset, VirtualRegister destinationAddress, int destinationQwOffset, int quadWordCount)
		{
			for (int i = 0; i < quadWordCount; i++)
			{
				VirtualRegister val = _writer.WriteLqd(sourceAddress, sourceQwOffset + i);
				_writer.WriteStqd(val, destinationAddress, destinationQwOffset + i);
			}
		}

		public VirtualRegister WritePpeMethodCall(TreeInstruction inst, List<VirtualRegister> childregs)
		{
			int areaSlot = 0;

			// Write method to be called.
			MethodInfo method = inst.OperandAsPpeMethod.Method;
			IntPtr mptr = method.MethodHandle.Value;
			VirtualRegister handlereg = _writer.WriteLoadIntPtr(mptr);

			ObjectWithAddress argaddress = _specialSpeObjects.PpeCallDataArea;
			_writer.WriteStore(handlereg, argaddress, areaSlot++);

			// Write parameters to the ppe call area.
			MethodCallInstruction mci = (MethodCallInstruction)inst;
			for (int paramidx = 0; paramidx < mci.Parameters.Count; paramidx++)
			{
				TreeInstruction param = mci.Parameters[paramidx];
				StackTypeDescription type = param.StackType;
				if (type.IndirectionLevel == 0)
				{
					// Byval value types.
					Utilities.Assert(type.CliType != CliType.ObjectType, "type.CliType != CliType.ObjectType");
					if (type.IsStackValueType)
					{
						// It's on the stack.
						for (int qnum = 0; qnum < type.ComplexType.QuadWordCount; qnum++)
						{
							VirtualRegister val = _writer.WriteLqd(childregs[paramidx], qnum);
							_writer.WriteStore(val, argaddress, areaSlot++);
						}
					}
					else
					{
						// The value is in a single register.
						_writer.WriteStore(childregs[paramidx], argaddress, areaSlot++);
					}
				}
				else if (type.IndirectionLevel == 1)
				{
					StackTypeDescription etype = type.Dereference();
					if (etype.CliType != CliType.ObjectType)
						throw new NotSupportedException(
							"Argument type '" + type + "' is not supported. Unmanaged pointers are not supported.");

					// Should be a PPE reference type handle.
					_writer.WriteStore(childregs[paramidx], argaddress, areaSlot++);
				}
				else
					throw new NotSupportedException("Argument type '" + type + "' is not supported.");

				Utilities.Assert(areaSlot * 16 <= _specialSpeObjects.PpeCallDataArea.Size,
								 "areaSlot * 16 < _specialSpeObjects.PpeCallDataArea.Size");
			}

			// Perform the call.
			_writer.WriteStop(SpuStopCode.PpeCall);


			// Move return value back.
			StackTypeDescription rettype = mci.StackType;
			if (rettype != StackTypeDescription.None)
			{
				VirtualRegister retval;
				if (rettype.DereferencedCliType == CliType.ObjectType)
				{
					Utilities.Assert(rettype.IndirectionLevel == 1, "mci.StackType.IndirectionLevel == 1");
					retval = _writer.WriteLoad(argaddress, 0);
				}
				else
				{
					Utilities.Assert(rettype.IndirectionLevel == 0, "rettype.IndirectionLevel == 0");
					if (!rettype.IsStackValueType)
						retval = _writer.WriteLoad(argaddress, 0);
					else
					{
						// Save the struct to a new stack position.
						int varStackPos = _stackAllocate(rettype.ComplexType.QuadWordCount);
						retval = _writer.WriteAi(HardwareRegister.SP, varStackPos * 16);
						for (int qnum = 0; qnum < rettype.ComplexType.QuadWordCount; qnum++)
						{
							VirtualRegister val = _writer.WriteLoad(argaddress, qnum);
							int stackPos = varStackPos + qnum;
							_writer.WriteStqd(val, HardwareRegister.SP, stackPos);
						}
					}
				}

				return retval;
			}

			return null;
		}

		public void WriteMethodCall(SpuRoutine routine, IList<VirtualRegister> arguments)
		{
			// Move parameters into hardware registers.
			for (int i = 0; i < arguments.Count; i++)
				_writer.WriteMove(arguments[i], HardwareRegister.GetHardwareArgumentRegister(i));

			_writer.WriteBrsl(routine);
		}

		public VirtualRegister WriteMethodCall(MethodCallInstruction callInst, List<VirtualRegister> childregs)
		{
			SpuRoutine target = callInst.TargetRoutine;

			MethodCompiler mc = callInst.TargetRoutine as MethodCompiler;
			if (mc != null)
			{
				if (mc.MethodBase.IsConstructor && mc.MethodBase == typeof(object).GetConstructor(Type.EmptyTypes))
				{
					// The System.Object ctor does nothing for us, so don't even bother calling it.
					return null;
				}
			}
			if (target.Parameters.Count > HardwareRegister.CallerSavesRegisters.Count)
				throw new NotImplementedException("No support for more than " +
												  HardwareRegister.CallerSavesRegisters.Count + "parameters.");

			// Move parameters into hardware registers.
			for (int i = 0; i < target.Parameters.Count; i++)
				_writer.WriteMove(childregs[i], HardwareRegister.GetHardwareArgumentRegister(i));

			_writer.WriteBrsl(target);

			if (callInst.StackType != StackTypeDescription.None)
				return HardwareRegister.GetHardwareRegister(CellRegister.REG_3);
			else
				return null;
		}

		public VirtualRegister WriteAllocateMemory(VirtualRegister bytesize)
		{
			// 1: Determine number of required bytes.
			// 2: Verify that we've got space for the allocation.
			// 3: Update available space counter.
			// 4: Get address for the array.
			// 5: Initialize array length field.
			// 6: Update pointer for next allocation.

			if (_specialSpeObjects == null)
				throw new InvalidOperationException("_specialSpeObjects == null");

			// Subtract from available byte count.
			{
				VirtualRegister allocatableByteCount = _writer.WriteLoad(_specialSpeObjects.AllocatableByteCountObject);
				_writer.WriteSf(allocatableByteCount, bytesize, allocatableByteCount);

				// If there isn't enough space, then halt by branching to the OOM routine.
				VirtualRegister isFreeSpacePositive = _writer.WriteCgti(allocatableByteCount, 0);
				_writer.WriteConditionalBranch(SpuOpCode.brz, isFreeSpacePositive, _specialSpeObjects.OutOfMemory);

				_writer.BeginNewBasicBlock();

				// Store new allocatable byte count.
				_writer.WriteStore(allocatableByteCount, _specialSpeObjects.AllocatableByteCountObject);
			}

			VirtualRegister nextAllocAddress = _writer.WriteLoad(_specialSpeObjects.NextAllocationStartObject);
			VirtualRegister array = nextAllocAddress;

			// Increment the pointer for the next allocation.
			{
				VirtualRegister newNextAllocAddress = _writer.NextRegister();
				_writer.WriteA(newNextAllocAddress, nextAllocAddress, bytesize);
				_writer.WriteStore(newNextAllocAddress, _specialSpeObjects.NextAllocationStartObject);
			}
			return array;
		}

		public void WriteZeroMemory(VirtualRegister address, int quadwordCount)
		{
			VirtualRegister zero = _writer.WriteIl(0);
			for (int i = 0; i < quadwordCount; i++)
			{
				_writer.WriteStqd(zero, address, i);
			}
		}

		public void GetStructFieldData(StackTypeDescription refType, Type complexType, FieldInfo field, out int qwoffset, out int byteoffset, out int valuesize)
		{
			Utilities.AssertArgument(refType.CliType == CliType.ValueType || refType.IndirectionLevel == 1, "refType.IndirectionLevel != 1");

			switch (refType.Dereference().CliType)
			{
				case CliType.ValueType:
					byteoffset = (int)Marshal.OffsetOf(complexType, field.Name);
					break;
				case CliType.ObjectType:
					byteoffset = refType.ComplexType.OffsetOf(field);
					break;
				default:
					throw new ArgumentException();
			}

			StackTypeDescription fieldtype = new TypeDeriver().GetStackTypeDescription(field.FieldType);
			if (fieldtype == StackTypeDescription.None ||
				fieldtype.CliType == CliType.ValueType)
				throw new NotSupportedException("Only simple field types are supported.");

			if (!fieldtype.IsImmutableSingleRegisterType)
			{
				if (fieldtype.IndirectionLevel != 1 && fieldtype.NumericSize != CliNumericSize.FourBytes && fieldtype.NumericSize != CliNumericSize.EightBytes)
					throw new NotSupportedException("Only four- and eight-byte fields are supported.");
			}

			if (fieldtype.CliType == CliType.ObjectType)
				valuesize = 4;
			else
				valuesize = (int)fieldtype.NumericSize;

			if (fieldtype.IsImmutableSingleRegisterType)
			{
				if (byteoffset % 4 != 0)
					throw new NotSupportedException();
			}
			else
			{
				// We don't do unaligned.
				if (byteoffset % valuesize != 0)
					throw new NotSupportedException();
			}

			qwoffset = byteoffset / 16;
			byteoffset = (byteoffset % 16);
		}

		public VirtualRegister GenerateSpuInstructionMethod(SpuInstructionWriter writer, MethodCallInstruction inst, List<VirtualRegister> childregs)
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
				object[] atts = parr[i].GetCustomAttributes(typeof(SpuInstructionPartAttribute), false);
				if (atts.Length == 0)
					throw new InvalidInstructionParametersException("Method: " + method.Name);
				SpuInstructionPartAttribute att = (SpuInstructionPartAttribute)atts[0];

				if ((necessaryParts & att.Part) == 0)
					throw new InvalidInstructionParametersException(
						"Instruction '" + inst.OperandSpuOpCode + "' does not use instruction part " + att.Part + ".");

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
				if (mi.ReturnType == typeof(void))
					returnRegister = null;
				else
				{
					object[] retAtts = mi.ReturnParameter.GetCustomAttributes(typeof(SpuInstructionPartAttribute), false);
					if (retAtts.Length != 1)
						throw new InvalidInstructionParametersException();

					att = (SpuInstructionPartAttribute)retAtts[0];
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

		public VirtualRegister GenerateIntrinsicMethod(SpuInstructionWriter writer, SpuIntrinsicMethod method, List<VirtualRegister> childregs)
		{
			//TODO make asertion for the number of element ind childregs, compare to the required number of regs.

			switch (method)
			{
				case SpuIntrinsicMethod.Runtime_Stop:
					writer.WriteStop();
					return null;
				case SpuIntrinsicMethod.Mfc_GetAvailableQueueEntries:
					return writer.WriteRdchcnt(SpuWriteChannel.MFC_CmdAndClassID);
				//					case SpuIntrinsicMethod.Mfc_Get:
				//						WriteMfcDmaCommand(writer, Mfc.MfcDmaCommand.Get, childregs);
				//						return null;
				//					case SpuIntrinsicMethod.Mfc_Put:
				//						WriteMfcDmaCommand(writer, Mfc.MfcDmaCommand.Put, childregs);
				//						return null;
				case SpuIntrinsicMethod.Vector_GetWord0:
					return childregs[0];
				case SpuIntrinsicMethod.Vector_GetWord1:
					return writer.WriteRotqbyi(childregs[0], 1 * 4);
				case SpuIntrinsicMethod.Vector_GetWord2:
					return writer.WriteRotqbyi(childregs[0], 2 * 4);
				case SpuIntrinsicMethod.Vector_GetWord3:
					return writer.WriteRotqbyi(childregs[0], 3 * 4);
				case SpuIntrinsicMethod.Vector_PutWord0:
					{
						VirtualRegister index = writer.WriteIl(0);
						VirtualRegister cwdreg = writer.WriteCwd(index, 0);
						return writer.WriteShufb(childregs[0], childregs[1], cwdreg);
					}
				case SpuIntrinsicMethod.Vector_PutWord1:
					{
						VirtualRegister index = writer.WriteIl(1 * 4);
						VirtualRegister cwdreg = writer.WriteCwd(index, 0);
						return writer.WriteShufb(childregs[0], childregs[1], cwdreg);
					}
				case SpuIntrinsicMethod.Vector_PutWord2:
					{
						VirtualRegister index = writer.WriteIl(2 * 4);
						VirtualRegister cwdreg = writer.WriteCwd(index, 0);
						return writer.WriteShufb(childregs[0], childregs[1], cwdreg);
					}
				case SpuIntrinsicMethod.Vector_PutWord3:
					{
						VirtualRegister index = writer.WriteIl(3 * 4);
						VirtualRegister cwdreg = writer.WriteCwd(index, 0);
						return writer.WriteShufb(childregs[0], childregs[1], cwdreg);
					}
				case SpuIntrinsicMethod.Vector_GetDWord0:
					return childregs[0];
				case SpuIntrinsicMethod.Vector_GetDWord1:
					return writer.WriteRotqbyi(childregs[0], 8);
				case SpuIntrinsicMethod.Int_Equals:
					{
						VirtualRegister r1 = writer.WriteCeq(childregs[0], childregs[1]);
						VirtualRegister r2 = writer.WriteGb(r1);
						VirtualRegister r3 = writer.WriteCeqi(r2, 0x0f);
						return writer.WriteAndi(r3, 1);
					}
				case SpuIntrinsicMethod.Int_NotEquals:
					{
						VirtualRegister r1 = writer.WriteCeq(childregs[0], childregs[1]);
						VirtualRegister r2 = writer.WriteGb(r1);
						VirtualRegister r3 = writer.WriteCeqi(r2, 0x0f);
						VirtualRegister r4 = writer.WriteAndi(r3, 1);
						return writer.WriteXori(r4, 0x01);
					}
				case SpuIntrinsicMethod.Float_Equals:
					{
						VirtualRegister r1 = writer.WriteFceq(childregs[0], childregs[1]);
						VirtualRegister r2 = writer.WriteGb(r1);
						VirtualRegister r3 = writer.WriteCeqi(r2, 0x0f);
						return writer.WriteAndi(r3, 1);
					}
				case SpuIntrinsicMethod.Float_NotEquals:
					{
						VirtualRegister r1 = writer.WriteFceq(childregs[0], childregs[1]);
						VirtualRegister r2 = writer.WriteGb(r1);
						VirtualRegister r3 = writer.WriteCeqi(r2, 0x0f);
						VirtualRegister r4 = writer.WriteAndi(r3, 1);
						return writer.WriteXori(r4, 0x01);
					}
				case SpuIntrinsicMethod.ReturnArgument1:
					return childregs[0];
				case SpuIntrinsicMethod.CombineFourWords:
					{
						// Could probably save some instructions here...
						VirtualRegister w0 = childregs[0];
						VirtualRegister w1 = writer.WriteRotqbyi(childregs[1], 12);
						VirtualRegister w2 = writer.WriteRotqbyi(childregs[2], 8);
						VirtualRegister w3 = writer.WriteRotqbyi(childregs[3], 4);

						VirtualRegister merge0_1_mask = writer.WriteFsmbi(0x0f00);
						VirtualRegister v0_1 = writer.WriteSelb(w0, w1, merge0_1_mask);

						VirtualRegister merge01_2_mask = writer.WriteFsmbi(0x00f0);
						VirtualRegister v01_2 = writer.WriteSelb(v0_1, w2, merge01_2_mask);

						VirtualRegister merge012_3_mask = writer.WriteFsmbi(0x000f);
						VirtualRegister v012_3 = writer.WriteSelb(v01_2, w3, merge012_3_mask);

						return v012_3;
					}
				case SpuIntrinsicMethod.CombineTwoDWords:
					{
						VirtualRegister w0 = childregs[0];
						VirtualRegister w1 = writer.WriteRotqbyi(childregs[1], 8);

						VirtualRegister merge0_1_mask = writer.WriteFsmbi(0x00ff);
						VirtualRegister v0_1 = writer.WriteSelb(w0, w1, merge0_1_mask);

						return v0_1;
					}
				case SpuIntrinsicMethod.SplatWord:
					{
						VirtualRegister pattern = writer.WriteIla(0x00010203);
						return writer.WriteShufb(childregs[0], childregs[0], pattern);
					}
				case SpuIntrinsicMethod.SplatDWord:
					{
						VirtualRegister w0 = childregs[0];
						VirtualRegister w1 = writer.WriteRotqbyi(w0, 8);
						VirtualRegister maskinput = writer.WriteIl(0x3);
						var mask = writer.WriteFsm(maskinput);
						return writer.WriteSelb(w0, w1, mask);
					}
				case SpuIntrinsicMethod.CompareGreaterThanIntAndSelect:
					{
						if (childregs.Count < 4)
							throw new ArgumentException("Too few argument register to intrinsic CompareGreaterThanIntAndSelect.");

						VirtualRegister r1 = writer.WriteCgt(childregs[0], childregs[1]);
						return writer.WriteSelb(childregs[3], childregs[2], r1);
					}
				case SpuIntrinsicMethod.CompareGreaterThanFloatAndSelect:
					{
						if (childregs.Count < 4)
							throw new ArgumentException("Too few argument register to intrinsic CompareGreaterThanFloatAndSelect.");

						VirtualRegister r1 = writer.WriteFcgt(childregs[0], childregs[1]);
						return writer.WriteSelb(childregs[3], childregs[2], r1);
					}
				case SpuIntrinsicMethod.CompareEqualsIntAndSelect:
					{
						if (childregs.Count < 4)
							throw new ArgumentException("Too few argument register to intrinsic CompareEqualsIntAndSelect.");

						VirtualRegister r1 = writer.WriteCeq(childregs[0], childregs[1]);
						return writer.WriteSelb(childregs[3], childregs[2], r1);
					}
				case SpuIntrinsicMethod.ConditionalSelectWord:
					{
						if (childregs.Count < 3)
							throw new ArgumentException("Too few argument register to intrinsic ConditionalSelectWord.");

						VirtualRegister r1 = writer.WriteFsm(childregs[0]);
						return writer.WriteSelb(childregs[2], childregs[1], r1);
					}
				case SpuIntrinsicMethod.ConditionalSelectVector:
					{
						if (childregs.Count < 3)
							throw new ArgumentException("Too few argument register to intrinsic ConditionalSelectWord.");

						VirtualRegister r1 = writer.WriteFsm(childregs[0]);
						VirtualRegister r2 = writer.WriteFsm(r1);
						return writer.WriteSelb(childregs[2], childregs[1], r2);
					}
				case SpuIntrinsicMethod.ConvertFloatToInteger:
					{
						if (childregs.Count < 1)
							throw new ArgumentException("Too few argument register to intrinsic ConvertFloatToInteger.");

						return writer.WriteCflts(childregs[0], 173);
					}
				case SpuIntrinsicMethod.ConvertIntToFloat:
					{
						if (childregs.Count < 1)
							throw new ArgumentException("Too few argument register to intrinsic ConvertIntToFloat.");

						return writer.WriteCsflt(childregs[0], 155);
					}
				default:
					throw new ArgumentException(method.ToString());
			}
		}

		private static void WriteMfcDmaCommand(SpuInstructionWriter writer, Mfc.MfcDmaCommand cmd, List<VirtualRegister> arguments)
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

			VirtualRegister cmdReg = writer.WriteIlhu((int)cmd);
			VirtualRegister tid2 = writer.WriteShli(tid, 24);
			VirtualRegister rid2 = writer.WriteShli(rid, 16);

			VirtualRegister or1 = writer.WriteOr(cmdReg, tid2);
			VirtualRegister finalCmd = writer.WriteOr(or1, rid2);

			writer.WriteWrch(SpuWriteChannel.MFC_CmdAndClassID, finalCmd);
		}

		public VirtualRegister WriteCltFloat64(VirtualRegister ra, VirtualRegister rb)
		{
			VirtualRegister r4 = rb;
			VirtualRegister r3 = ra;

			VirtualRegister r34 = _writer.WriteIl(0);
			VirtualRegister r40 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleSignFilter);
			VirtualRegister r36 = _writer.WriteRotmai(r4, -31);
			_writer.WriteNop();
			_writer.WriteNop();
			VirtualRegister r38 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleCompareDataArea, 2);
			VirtualRegister r35 = _writer.WriteRotmai(r3, -31);
			VirtualRegister r13 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleCompareDataArea, 3);
			VirtualRegister r25 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleExponentFilter);
			_writer.WriteNop();
			VirtualRegister r26 = _writer.WriteAnd(r3, r40);
			VirtualRegister r33 = _writer.WriteAnd(r4, r40);
			VirtualRegister r37 = _writer.WriteBg(r26, r34);
			VirtualRegister r32 = _writer.WriteShufb(r36, r36, r13);
			VirtualRegister r39 = _writer.WriteBg(r33, r34);
			VirtualRegister r30 = _writer.WriteShufb(r35, r35, r13);
			VirtualRegister r23 = _writer.WriteClgt(r33, r25);
			_writer.WriteNop();
			_writer.WriteNop();
			VirtualRegister r29 = _writer.WriteShufb(r37, r37, r38);
			VirtualRegister r17 = _writer.WriteClgt(r26, r25);
			VirtualRegister r31 = _writer.WriteShufb(r39, r39, r38);
			VirtualRegister r28 = _writer.WriteCeq(r33, r25);
			VirtualRegister r27 = _writer.WriteRotqbyi(r23, 4);
			VirtualRegister r22 = _writer.WriteCeq(r26, r25);
			VirtualRegister r21 = _writer.WriteRotqbyi(r17, 4);
			_writer.WriteSfx(r29, r26, r34);
			_writer.WriteSfx(r31, r33, r34);
			VirtualRegister r15 = _writer.WriteSelb(r26, r29, r30);
			VirtualRegister r16 = _writer.WriteSelb(r33, r31, r32);
			VirtualRegister r24 = _writer.WriteAnd(r28, r27);
			VirtualRegister r20 = _writer.WriteClgt(r16, r15);
			VirtualRegister r18 = _writer.WriteAnd(r22, r21);
			VirtualRegister r11 = _writer.WriteCeq(r16, r15);
			VirtualRegister r12 = _writer.WriteRotqbyi(r20, 4);
			VirtualRegister r19 = _writer.WriteOr(r23, r24);
			r4 = _writer.WriteCgt(r16, r15);
			VirtualRegister r14 = _writer.WriteOr(r17, r18);
			VirtualRegister r9 = _writer.WriteShufb(r19, r19, r13);
			VirtualRegister r10 = _writer.WriteAnd(r11, r12);
			VirtualRegister r8 = _writer.WriteShufb(r14, r14, r13);
			VirtualRegister r5 = _writer.WriteOr(r4, r10);
			VirtualRegister r2 = _writer.WriteShufb(r5, r5, r13);
			VirtualRegister r7 = _writer.WriteOr(r9, r8);
			VirtualRegister r6 = _writer.WriteAndc(r2, r7);
			return _writer.WriteSfi(r6, 0);
		}

		public VirtualRegister WriteCeqFloat64(VirtualRegister vrleft, VirtualRegister vrright)
		{
			var r3 = vrleft;
			var r4 = vrright;
			var r11 = _writer.WriteOr(r4, r3);
			var r10 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleSignFilter);
			var r12 = _writer.WriteCeq(r3, r4);
			r4 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleCeqMagic1);
			var r13 = _writer.WriteRotqbyi(r12, 4);
			var r9 = _writer.WriteAnd(r11, r10);
			var r5 = _writer.WriteAnd(r12, r13);
			var r8 = _writer.WriteCeqi(r9, 0);
			var r6 = _writer.WriteRotqbyi(r8, 4);
			var r7 = _writer.WriteAnd(r8, r6);
			var r2 = _writer.WriteOr(r5, r7);
			r3 = _writer.WriteShufb(r2, r2, r4);
			r3 = _writer.WriteSfi(r3, 0);
			return r3;
		}

		public VirtualRegister WriteCeqFloat64_old(VirtualRegister vrleft, VirtualRegister vrright)
		{
			var r3 = vrleft;
			var r4 = vrright;
			var r20 = _writer.WriteCeq(r3, r4);
			var r19 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleSignFilter);
			var r18 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleExponentFilter);
			var r21 = _writer.WriteRotqbyi(r20, 4);
			var r13 = _writer.WriteAnd(vrleft, r19);
			var r12 = _writer.WriteAnd(vrright, r19);
			r4 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleCeqMagic1);
			var r14 = _writer.WriteClgt(r13, r18);
			var r11 = _writer.WriteOr(r13, r12);
			var r16 = _writer.WriteCeq(r13, r18);
			var r17 = _writer.WriteRotqbyi(r14, 4);
			var r9 = _writer.WriteCeqi(r11, 0);
			var r6 = _writer.WriteAnd(r20, r21);
			var r10 = _writer.WriteRotqbyi(r9, 4);
			var r15 = _writer.WriteAnd(r16, r17);
			var r5 = _writer.WriteOr(r14, r15);
			var r8 = _writer.WriteAnd(r9, r10);
			var r7 = _writer.WriteOr(r6, r8);
			var r2 = _writer.WriteAndc(r7, r5);
			r3 = _writer.WriteShufb(r2, r2, r4);
			r3 = _writer.WriteSfi(r3, 0); // ??
			return r3;
		}

		public VirtualRegister WriteCgtFloat64(VirtualRegister vrleft, VirtualRegister vrright)
		{
			var r3 = vrleft;
			var r4 = vrright;
			var r18 = _writer.WriteIl(0);
			var r24 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleSignFilter);
			var r20 = _writer.WriteRotmai(r3, -31);
			var r22 = _writer.WriteLoad(_specialSpeObjects.MathObjects.Double04050607_c);
			var r19 = _writer.WriteRotmai(r4, -31);
			var r10 = _writer.WriteLoad(_specialSpeObjects.MathObjects.DoubleCeqShuffleMask2);
			var r9 = _writer.WriteAnd(r4, r24);
			var r17 = _writer.WriteAnd(r3, r24);
			var r21 = _writer.WriteBg(r9, r18);
			var r16 = _writer.WriteShufb(r20, r20, r10);
			var r23 = _writer.WriteBg(r17, r18);
			var r14 = _writer.WriteShufb(r19, r19, r10);
			var r13 = _writer.WriteShufb(r21, r21, r22);
			var r15 = _writer.WriteShufb(r23, r23, r22);
			r13 = _writer.WriteSfx(r9, r18);
			r15 = _writer.WriteSfx(r17, r18);
			var r12 = _writer.WriteSelb(r9, r13, r14);
			var r11 = _writer.WriteSelb(r17, r15, r16);
			var r8 = _writer.WriteClgt(r11, r12);
			var r7 = _writer.WriteCeq(r11, r12);
			var r5 = _writer.WriteCgt(r11, r12);
			r4 = _writer.WriteRotqbyi(r8, 2);
			var r6 = _writer.WriteAnd(r7, r4);
			var r2 = _writer.WriteOr(r5, r6);
			r3 = _writer.WriteShufb(r2, r2, r10);
			r3 = _writer.WriteSfi(r3, 0);
			return r3;
		}
	}
}
