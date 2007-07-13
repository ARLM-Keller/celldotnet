using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// This class writes IL trees as SPU instructions.
	/// </summary>
	class ILTreeSpuWriter
	{
		private SpuInstructionWriter _writer;

		public void GenerateCode(MethodCompiler ci, SpuInstructionWriter writer)
		{
			_writer = writer;

			foreach (BasicBlock bb in ci.Blocks)
			{
				foreach (TreeInstruction root in bb.Roots)
				{
					GenerateCode(root);
				}
			}
		}

		private VirtualRegister GenerateCode(TreeInstruction inst)
		{
			VirtualRegister vrleft = null, vrright = null;

			if (inst.Left != null)
			{
				vrleft = GenerateCode(inst.Left);
				if (inst.Right != null)
					vrright = GenerateCode(inst.Right);
			}
			else if (inst.Right != null)
				throw new InvalidILTreeException("Right but no left??");

			IRCode ilcode = inst.Opcode.IRCode;
			switch (ilcode)
			{
				case IRCode.Nop:
					return null;
				case IRCode.Break:
					break;
				case IRCode.Ldnull:
					break;
				case IRCode.Ldc_I4:
					{
						VirtualRegister r;
						int i = (int)inst.Operand;
						if (i >> 16 == 0)
						{
							r = _writer.WriteIl(i);
						}
						else
						{
							r = _writer.WriteIlhu(i >> 16);
							_writer.WriteIohl(r, i);
						}
						return r;
					}
				case IRCode.Ldc_I8:
					break;
				case IRCode.Ldc_R4:
					break;
				case IRCode.Ldc_R8:
					break;
				case IRCode.Dup:
					break;
				case IRCode.Pop:
					break;
				case IRCode.Jmp:
					break;
				case IRCode.Call:
					break;
				case IRCode.Callvirt:
					break;
				case IRCode.Calli:
					break;
				case IRCode.Ret:
					if (inst.StackType != StackTypeDescription.None)
						throw new NotImplementedException("Cannot return values.");
					return null;
				case IRCode.Br:
					break;
				case IRCode.Brfalse:
					break;
				case IRCode.Brtrue:
					break;
				case IRCode.Beq:
					break;
				case IRCode.Bge:
					break;
				case IRCode.Bgt:
					break;
				case IRCode.Ble:
					break;
				case IRCode.Blt:
					break;
				case IRCode.Bne_Un:
					break;
				case IRCode.Bge_Un:
					break;
				case IRCode.Bgt_Un:
					break;
				case IRCode.Ble_Un:
					break;
				case IRCode.Blt_Un:
					break;
				case IRCode.Switch:
					break;
				case IRCode.Ldind_I1:
					break;
				case IRCode.Ldind_U1:
					break;
				case IRCode.Ldind_I2:
					break;
				case IRCode.Ldind_U2:
					break;
				case IRCode.Ldind_I4:
					break;
				case IRCode.Ldind_U4:
					break;
				case IRCode.Ldind_I8:
					break;
				case IRCode.Ldind_I:
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
						if (inst.Left.StackType.IndirectionLevel != 1) 
							throw new InvalidILTreeException("Invalid level of indirection for stind. Stack type: " + inst.Left.StackType);
						VirtualRegister ptr = GetVirtualRegister(inst.Left);

						VirtualRegister loadedvalue = _writer.WriteLqd(ptr, 0);
						VirtualRegister mask = _writer.WriteCwd(ptr, 0);
//						VirtualRegister combined = _writer.WriteShufb(loadedvalue, vrright, mask);
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
					break;
				case IRCode.Sub:
					break;
				case IRCode.Mul:
					break;
				case IRCode.Div:
					break;
				case IRCode.Div_Un:
					break;
				case IRCode.Rem:
					break;
				case IRCode.Rem_Un:
					break;
				case IRCode.And:
					break;
				case IRCode.Or:
					break;
				case IRCode.Xor:
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
					break;
				case IRCode.Cgt:
					break;
				case IRCode.Cgt_Un:
					break;
				case IRCode.Clt:
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
					return ((MethodVariable)inst.Operand).VirtualRegister;
				case IRCode.Ldloca:
					break;
				case IRCode.Stloc:
					VirtualRegister dest = ((MethodVariable)inst.Operand).VirtualRegister;
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
					throw new InvalidILTreeException("Invalid opcode: " + ilcode);
			}

			throw new ILNotImplementedException(inst);
		}

		private VirtualRegister GetVirtualRegister(TreeInstruction inst)
		{
			if (inst.Operand is MethodVariable)
			{
				MethodVariable var = (MethodVariable)inst.Operand;
				Utilities.AssertNotNull(var.VirtualRegister, "var.VirtualRegister");
				return var.VirtualRegister;
			}
			else
				throw new NotImplementedException();
		}
	}

}
