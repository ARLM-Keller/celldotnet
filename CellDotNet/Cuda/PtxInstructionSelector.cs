﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	class PtxInstructionSelector
	{
		public ICollection<BasicBlock> Select(List<BasicBlock> inputblocks)
		{
			// construct all output blocks up front, so we can reference them for branches.
			var blockmap = inputblocks.ToDictionary(ib => ib, ib => new BasicBlock());

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

		void Select(ListInstruction inputinst, BasicBlock ob, Dictionary<BasicBlock, BasicBlock> blockmap)
		{
			ListInstruction new1, new2;

			switch (inputinst.IRCode)
			{
				case IRCode.Add:
//					new1 = new ListInstruction(PtxCode.None);
					break;
				case IRCode.Add_Ovf:
				case IRCode.Add_Ovf_Un:
				case IRCode.And:
				case IRCode.Arglist:
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
				case IRCode.Br:
				case IRCode.Break:
				case IRCode.Brfalse:
				case IRCode.Brtrue:
				case IRCode.Call:
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
				case IRCode.Ldarg:
				case IRCode.Ldarga:
				case IRCode.Ldc_I4:
				case IRCode.Ldc_I8:
				case IRCode.Ldc_R4:
				case IRCode.Ldc_R8:
				case IRCode.Ldelem:
				case IRCode.Ldelem_I:
				case IRCode.Ldelem_I1:
				case IRCode.Ldelem_I2:
				case IRCode.Ldelem_I4:
				case IRCode.Ldelem_I8:
				case IRCode.Ldelem_R4:
				case IRCode.Ldelem_R8:
				case IRCode.Ldelem_Ref:
				case IRCode.Ldelem_U1:
				case IRCode.Ldelem_U2:
				case IRCode.Ldelem_U4:
				case IRCode.Ldelema:
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
				case IRCode.Ldnull:
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
				case IRCode.Ret:
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
				case IRCode.Stind_I4:
				case IRCode.Stind_I8:
				case IRCode.Stind_R4:
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
				case IRCode.Unaligned:
				case IRCode.Unbox:
				case IRCode.Unbox_Any:
				case IRCode.Volatile:
				case IRCode.Xor:
					break;
			}
			
		}
	}
}
