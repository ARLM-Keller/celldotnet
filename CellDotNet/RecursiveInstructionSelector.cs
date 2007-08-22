using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CellDotNet
{
	/// <summary>
	/// This class writes IL trees as SPU instructions.
	/// </summary>
	class RecursiveInstructionSelector
	{
		private SpuInstructionWriter _writer;
//		private MethodCompiler _method;
		private List<IRBasicBlock> _basicBlocks;

		private ReadOnlyCollection<MethodParameter> _parameters;

		private List<KeyValuePair<SpuInstruction, IRBasicBlock>> _branchInstructions;
		private Dictionary<IRBasicBlock, SpuBasicBlock> _spubasicblocks;

		private List<IROpCode> _unimplementedOpCodes;


		//  public void GenerateCode(MethodCompiler mc, SpuInstructionWriter writer)
		public void GenerateCode(List<IRBasicBlock> basicBlocks, ReadOnlyCollection<MethodParameter> parameters, SpuInstructionWriter writer)
		{
			_writer = writer;
//			_method = mc;
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

				VirtualRegister dest = HardwareRegister.GetHardwareRegister(FirstArgumentRegister + i);

				_writer.WriteMove(parameter.VirtualRegister, dest);
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
				childregs.Add(reg);
			}
			if (childregs.Count >= 1)
			{
				vrleft = childregs[0];
				if (childregs.Count >= 2)
					vrright = childregs[1];
			}

//			// For the compare instructions.
//			bool orderedCompare = true;

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
				case IRCode.Call:
					{
						MethodCompiler target = (MethodCompiler) inst.Operand;

						if (target.MethodBase.IsConstructor)
							throw new NotImplementedException("Constructors are not implemented.");
						if (!target.MethodBase.IsStatic)
							throw new NotImplementedException("Only static methods are implemented.");

						// Move parameters into hardware registers.
						for (int i = 0; i < target.Parameters.Count; i++)
							_writer.WriteMove(childregs[i], HardwareRegister.GetHardwareArgumentRegister(i));


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
					WriteConditionalBranch(SpuOpCode.brnz, vrleft, (IRBasicBlock) inst.Operand);
					return null;
				case IRCode.Beq:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							VirtualRegister vr1 = _writer.WriteCeq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr1, (IRBasicBlock)inst.Operand);
							return null;
						case CliType.Float32:
							VirtualRegister vr2 = _writer.WriteFceq(vrleft, vrright);
							WriteConditionalBranch(SpuOpCode.brnz, vr2, (IRBasicBlock)inst.Operand);
							return null;
						case CliType.Int64:
						case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
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
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
								vr = _writer.WriteCgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock) inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
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
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
								vr = _writer.WriteCgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
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
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
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
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
								vr = _writer.WriteClgt(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrleft, vrright);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
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
							case CliType.UInt64:
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
							case CliType.UInt32:
							case CliType.NativeInt:
							case CliType.NativeUInt:
								vr = _writer.WriteClgt(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Float32:
								vr = _writer.WriteFceq(vrright, vrleft);
								WriteConditionalBranch(SpuOpCode.brnz, vr, (IRBasicBlock)inst.Operand);
								return null;
							case CliType.Int64:
							case CliType.UInt64:
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
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							return _writer.WriteA(vrleft, vrright);
					}
					break;
				case IRCode.Sub:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							return _writer.WriteSf(vrright, vrleft);
					}
					break;
				case IRCode.Mul:
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
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
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							return _writer.WriteAnd(vrleft, vrright);
						case CliType.Int64:
						case CliType.UInt64:
							break;
					}
					break;
				case IRCode.Or:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							return _writer.WriteOr(vrleft, vrright);
						case CliType.Int64:
						case CliType.UInt64:
							break;
					}
					break;
				case IRCode.Xor:
					// NOTE: Asumes left and right operand is compatible
					// TODO: Not implemented for all valid types.
					switch (lefttype.CliType)
					{
						case CliType.Int32:
						case CliType.UInt32:
						case CliType.NativeInt:
						case CliType.NativeUInt:
							return _writer.WriteXor(vrleft, vrright);
						case CliType.Int64:
						case CliType.UInt64:
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
					break;
				case IRCode.Conv_I8:
					break;
				case IRCode.Conv_R4:
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
					break;
				case IRCode.Ldlen:
					break;
				case IRCode.Ldelema:
					break;
				case IRCode.Ldelem_I1:
					break;
				case IRCode.Ldelem_U1:
					break;
				case IRCode.Ldelem_I2:
					break;
				case IRCode.Ldelem_U2:
					break;
				case IRCode.Ldelem_I4:
					break;
				case IRCode.Ldelem_U4:
					break;
				case IRCode.Ldelem_I8:
					break;
				case IRCode.Ldelem_I:
					break;
				case IRCode.Ldelem_R4:
					break;
				case IRCode.Ldelem_R8:
					break;
				case IRCode.Ldelem_Ref:
					break;
				case IRCode.Stelem_I:
					break;
				case IRCode.Stelem_I1:
					break;
				case IRCode.Stelem_I2:
					break;
				case IRCode.Stelem_I4:
					break;
				case IRCode.Stelem_I8:
					break;
				case IRCode.Stelem_R4:
					break;
				case IRCode.Stelem_R8:
					break;
				case IRCode.Stelem_Ref:
					break;
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
							case CliType.Int8:
							case CliType.UInt8:
								val = _writer.WriteCeqb(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Int32:
							case CliType.UInt32:
								val = _writer.WriteCeq(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Int16:
							case CliType.UInt16:
								val = _writer.WriteCeqh(vrleft, vrright);
								return _writer.WriteAndi(val, 1);
							case CliType.Float32:
							case CliType.Float64:
							case CliType.Int64:
							case CliType.UInt64:
								break;
						}
						break;
					}
				case IRCode.Cgt:
					switch (lefttype.CliType)
					{
						case CliType.NativeInt:
						case CliType.NativeUInt:
						case CliType.Int32:
						case CliType.UInt32:
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
						case CliType.NativeUInt:
						case CliType.Int32:
						case CliType.UInt32:
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
						// Do nothing.
						return null;
					}
				case IRCode.Ldarga:
					break;
				case IRCode.Starg:
					break;
				case IRCode.Ldloc:
					return ((MethodVariable) inst.Operand).VirtualRegister;
				case IRCode.Ldloca:
					break;
				case IRCode.Stloc:
					VirtualRegister dest = ((MethodVariable) inst.Operand).VirtualRegister;
					_writer.WriteMove(vrleft, dest);
					return null;
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
