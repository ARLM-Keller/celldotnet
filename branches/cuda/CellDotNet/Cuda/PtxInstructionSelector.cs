﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	class PtxInstructionSelector
	{
		public List<BasicBlock> Select(List<BasicBlock> inputblocks)
		{
			// construct all output blocks up front, so we can reference them for branches.
			var blockmap = inputblocks.ToDictionary(ib => ib, ib => new BasicBlock(ib.Name));

			foreach (BasicBlock ib in inputblocks)
			{
				var ob = blockmap[ib];

				foreach (ListInstruction inputinst in ib.Instructions)
				{
					Select(inputinst, ob, blockmap);
				}
			}

			return inputblocks.Select(ib => blockmap[ib]).ToList();
		}

		void Select(ListInstruction inst, BasicBlock ob, Dictionary<BasicBlock, BasicBlock> blockmap)
		{
			ListInstruction new1, new2;
			PtxCode opcode;

			switch (inst.IRCode)
			{
				case IRCode.Add:
					switch (inst.Destination.StackType)
					{
						case StackType.I4:
							opcode = PtxCode.Add_S32;
							break;
						case StackType.R4:
							opcode = PtxCode.Add_F32;
							break;
						default:
							throw new InvalidIRException();
					}
					new1 = new ListInstruction(opcode, inst);
					ob.Append(new1);
					return;
				case IRCode.Add_Ovf:
				case IRCode.Add_Ovf_Un:
				case IRCode.And:
				case IRCode.Arglist:
					break;
				case IRCode.Beq:
				case IRCode.Bge:
				case IRCode.Bge_Un:
				case IRCode.Bgt:
				case IRCode.Bgt_Un:
				case IRCode.Ble:
				case IRCode.Ble_Un:
				case IRCode.Blt:
				case IRCode.Blt_Un:
				case IRCode.Bne_Un:
				case IRCode.Box:
				case IRCode.Brfalse:
				case IRCode.Brtrue:
					throw new InvalidIRException("Conditional branch code " + inst.IRCode + " encountered.");
				case IRCode.Br:
					new1 = new ListInstruction(PtxCode.Bra, inst) {Operand = blockmap[(BasicBlock) inst.Operand]};
					ob.Append(new1);
					return;
				case IRCode.Break:
				case IRCode.Call:
//					ob.Append(new MethodCallListInstruction(PtxCode.Call, inst));
//					break;
				case IRCode.Calli:
				case IRCode.Callvirt:
				case IRCode.Castclass:
				case IRCode.Ceq:
				case IRCode.Cgt:
				case IRCode.Cgt_Un:
				case IRCode.Ckfinite:
				case IRCode.Clt:
				case IRCode.Clt_Un:
				case IRCode.Constrained:
				case IRCode.Conv_I:
				case IRCode.Conv_I1:
				case IRCode.Conv_I2:
				case IRCode.Conv_I4:
				case IRCode.Conv_I8:
				case IRCode.Conv_Ovf_I:
				case IRCode.Conv_Ovf_I_Un:
				case IRCode.Conv_Ovf_I1:
				case IRCode.Conv_Ovf_I1_Un:
				case IRCode.Conv_Ovf_I2:
				case IRCode.Conv_Ovf_I2_Un:
				case IRCode.Conv_Ovf_I4:
				case IRCode.Conv_Ovf_I4_Un:
				case IRCode.Conv_Ovf_I8:
				case IRCode.Conv_Ovf_I8_Un:
				case IRCode.Conv_Ovf_U:
				case IRCode.Conv_Ovf_U_Un:
				case IRCode.Conv_Ovf_U1:
				case IRCode.Conv_Ovf_U1_Un:
				case IRCode.Conv_Ovf_U2:
				case IRCode.Conv_Ovf_U2_Un:
				case IRCode.Conv_Ovf_U4:
				case IRCode.Conv_Ovf_U4_Un:
				case IRCode.Conv_Ovf_U8:
				case IRCode.Conv_Ovf_U8_Un:
				case IRCode.Conv_R_Un:
				case IRCode.Conv_R4:
				case IRCode.Conv_R8:
				case IRCode.Conv_U:
				case IRCode.Conv_U1:
				case IRCode.Conv_U2:
				case IRCode.Conv_U4:
				case IRCode.Conv_U8:
				case IRCode.Cpblk:
				case IRCode.Cpobj:
				case IRCode.Div:
				case IRCode.Div_Un:
				case IRCode.Dup:
				case IRCode.Endfilter:
				case IRCode.Endfinally:
				case IRCode.Initblk:
				case IRCode.Initobj:
				case IRCode.IntrinsicCall:
				case IRCode.IntrinsicNewObj:
				case IRCode.Isinst:
				case IRCode.Jmp:
					break;
				case IRCode.Ldarg:
					switch (inst.Destination.StackType)
					{
						case StackType.Object:
							opcode = PtxCode.Ld_Param_S32;
							break;
						case StackType.I4:
							opcode = PtxCode.Ld_Param_S32;
							break;
						case StackType.R4:
							opcode = PtxCode.Ld_Param_F32;
							break;
						default:
							throw new InvalidIRException();
					}
					new1 = new ListInstruction(opcode, inst);
					ob.Append(new1);
					return;
				case IRCode.Ldarga:
				case IRCode.Ldc_I4:
					ob.Append(new ListInstruction(PtxCode.Mov_S32, inst) { Source1 = GlobalVReg.FromImmediate(inst.Operand, StackType.I4), Operand = null });
					return;
				case IRCode.Ldc_I8:
					break;
				case IRCode.Ldc_R4:
					ob.Append(new ListInstruction(PtxCode.Mov_F32, inst) { Source1 = GlobalVReg.FromImmediate(inst.Operand, StackType.R4), Operand = null });
					return;
				case IRCode.Ldc_R8:
					break;
				case IRCode.Ldelem:
				case IRCode.Ldelem_I:
				case IRCode.Ldelem_I1:
				case IRCode.Ldelem_I2:
				case IRCode.Ldelem_I8:
					break;
				case IRCode.Ldelem_R4: SelectLdElem(inst, ob, PtxCode.Ld_Global_F32, 4); return;
				case IRCode.Ldelem_R8:
				case IRCode.Ldelem_Ref:
				case IRCode.Ldelem_U1:
				case IRCode.Ldelem_U2:
					break;
				case IRCode.Ldelem_I4:
				case IRCode.Ldelem_U4: SelectLdElem(inst, ob, PtxCode.Ld_Global_S32, 4); return;
				case IRCode.Ldelema:
					{
						var offset = GlobalVReg.FromNumericType(StackType.I4, VRegStorage.Register);
						var elementsize = inst.OperandAsGlobalVRegNonNull.GetElementSize();
						// Determine byte offset.
						ob.Append(new ListInstruction(PtxCode.Mul_Lo_S32, inst)
						          	{
						          		Destination = offset,
						          		Source1 = inst.Source2,
						          		// index
						          		Source2 = GlobalVReg.FromImmediate(elementsize, StackType.I4)
						          	});
						// Determine element address.
						ob.Append(new ListInstruction(PtxCode.Add_S32, inst)
						          	{
						          		Destination = inst.Destination,
						          		Source1 = inst.Source1,
						          		Source2 = offset
						          	});
						return;
					}
				case IRCode.Ldfld:
				case IRCode.Ldflda:
				case IRCode.Ldftn:
				case IRCode.Ldind_I:
				case IRCode.Ldind_I1:
				case IRCode.Ldind_I2:
				case IRCode.Ldind_I4:
				case IRCode.Ldind_I8:
				case IRCode.Ldind_R4:
				case IRCode.Ldind_R8:
				case IRCode.Ldind_Ref:
				case IRCode.Ldind_U1:
				case IRCode.Ldind_U2:
				case IRCode.Ldind_U4:
				case IRCode.Ldlen:
				case IRCode.Ldloc:
				case IRCode.Ldloca:
					break;
				case IRCode.Ldnull:
					// RH 20080816: Using i4 for now.
					ob.Append(new ListInstruction(PtxCode.Mov_S32, inst) {Source1 = GlobalVReg.FromImmediate(0, StackType.I4)});
					break;
				case IRCode.Ldobj:
				case IRCode.Ldsfld:
				case IRCode.Ldsflda:
				case IRCode.Ldstr:
				case IRCode.Ldtoken:
				case IRCode.Ldvirtftn:
				case IRCode.Leave:
				case IRCode.Leave_S:
				case IRCode.Localloc:
				case IRCode.Mkrefany:
				case IRCode.Mul:
				case IRCode.Mul_Ovf:
				case IRCode.Mul_Ovf_Un:
				case IRCode.Neg:
				case IRCode.Newarr:
				case IRCode.Newobj:
				case IRCode.Nop:
				case IRCode.Not:
				case IRCode.Or:
				case IRCode.Pop:
				case IRCode.PpeCall:
				case IRCode.Prefix1:
				case IRCode.Prefix2:
				case IRCode.Prefix3:
				case IRCode.Prefix4:
				case IRCode.Prefix5:
				case IRCode.Prefix6:
				case IRCode.Prefix7:
				case IRCode.Prefixref:
				case IRCode.Readonly:
				case IRCode.Refanytype:
				case IRCode.Refanyval:
				case IRCode.Rem:
				case IRCode.Rem_Un:
					break;
				case IRCode.Ret:
					new1 = new ListInstruction(PtxCode.Ret);
					ob.Append(new1);
					return;
				case IRCode.Rethrow:
				case IRCode.Shl:
				case IRCode.Shr:
				case IRCode.Shr_Un:
				case IRCode.Sizeof:
				case IRCode.SpuInstructionMethodCall:
				case IRCode.Starg:
				case IRCode.Stelem:
				case IRCode.Stelem_I:
				case IRCode.Stelem_I1:
				case IRCode.Stelem_I2:
				case IRCode.Stelem_I4:
				case IRCode.Stelem_I8:
				case IRCode.Stelem_R4:
				case IRCode.Stelem_R8:
				case IRCode.Stelem_Ref:
				case IRCode.Stfld:
				case IRCode.Stind_I:
				case IRCode.Stind_I1:
				case IRCode.Stind_I2:
					break;
				case IRCode.Stind_I4:
					ob.Append(new ListInstruction(PtxCode.St_Global_S32, inst));
					return;
				case IRCode.Stind_I8:
					break;
				case IRCode.Stind_R4:
					ob.Append(new ListInstruction(PtxCode.St_Global_F32, inst));
					return;
				case IRCode.Stind_R8:
				case IRCode.Stind_Ref:
				case IRCode.Stloc:
				case IRCode.Stobj:
				case IRCode.Stsfld:
				case IRCode.Sub:
				case IRCode.Sub_Ovf:
				case IRCode.Sub_Ovf_Un:
				case IRCode.Switch:
				case IRCode.Tailcall:
				case IRCode.Throw:
					// kind of a hack, but convenient at least for initial testing.
					ob.Append(new ListInstruction(PtxCode.Exit));
					break;
				case IRCode.Unaligned:
				case IRCode.Unbox:
				case IRCode.Unbox_Any:
				case IRCode.Volatile:
				case IRCode.Xor:
					break;
			}

			throw new NotImplementedException("Opcode not implemented in instruction selector: " + inst.IRCode);
		}

		private void SelectLdElem(ListInstruction ldElemInst, BasicBlock ob, PtxCode ptxLoadCode, int elementsize)
		{
			var offset = GlobalVReg.FromNumericType(StackType.I4, VRegStorage.Register);
			// Determine byte offset.
			ob.Append(new ListInstruction(PtxCode.Mul_Lo_S32, ldElemInst)
			          	{
			          		Destination = offset,
			          		Source1 = ldElemInst.Source2, // index
			          		Source2 = GlobalVReg.FromImmediate(elementsize, StackType.I4)
			          	});
			// Determine element address.
			var address = GlobalVReg.FromNumericType(StackType.I4, VRegStorage.Register);
			ob.Append(new ListInstruction(PtxCode.Add_S32, ldElemInst)
			          	{
			          		Destination = address,
			          		Source1 = ldElemInst.Source1,
			          		Source2 = offset
			          	});
			ob.Append(new ListInstruction(ptxLoadCode, ldElemInst)
			          	{
			          		Destination = ldElemInst.Destination,
			          		Source1 = address,
			          		Source2 = null
			          	});
		}
	}
}
