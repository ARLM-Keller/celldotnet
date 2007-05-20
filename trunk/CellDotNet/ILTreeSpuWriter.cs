using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// This class writes IL trees as SPU instructions.
	/// </summary>
	class ILTreeSpuWriter
	{
		private SpuInstructionWriter _writer;

		public void GenerateCode(CompileInfo ci, SpuInstructionWriter writer)
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

			Code ilcode = inst.Opcode.Code;
			int oldInstCount = _writer.Instructions.Count;
			switch (ilcode)
			{
				case Code.Nop:
					return null;
				case Code.Break:
					break;
				case Code.Ldnull:
					break;
				case Code.Ldc_I4:
					break;
				case Code.Ldc_I8:
					break;
				case Code.Ldc_R4:
					break;
				case Code.Ldc_R8:
					break;
				case Code.Dup:
					break;
				case Code.Pop:
					break;
				case Code.Jmp:
					break;
				case Code.Call:
					break;
				case Code.Calli:
					break;
				case Code.Ret:
					break;
				case Code.Bne_Un_S:
					break;
				case Code.Bge_Un_S:
					break;
				case Code.Bgt_Un_S:
					break;
				case Code.Ble_Un_S:
					break;
				case Code.Blt_Un_S:
					break;
				case Code.Br:
					break;
				case Code.Brfalse:
					break;
				case Code.Brtrue:
					break;
				case Code.Beq:
					break;
				case Code.Bge:
					break;
				case Code.Bgt:
					break;
				case Code.Ble:
					break;
				case Code.Blt:
					break;
				case Code.Bne_Un:
					break;
				case Code.Bge_Un:
					break;
				case Code.Bgt_Un:
					break;
				case Code.Ble_Un:
					break;
				case Code.Blt_Un:
					break;
				case Code.Switch:
					break;
				case Code.Ldind_I1:
					break;
				case Code.Ldind_U1:
					break;
				case Code.Ldind_I2:
					break;
				case Code.Ldind_U2:
					break;
				case Code.Ldind_I4:
					break;
				case Code.Ldind_U4:
					break;
				case Code.Ldind_I8:
					break;
				case Code.Ldind_I:
					break;
				case Code.Ldind_R4:
					break;
				case Code.Ldind_R8:
					break;
				case Code.Ldind_Ref:
					break;
				case Code.Stind_Ref:
					break;
				case Code.Stind_I1:
					break;
				case Code.Stind_I2:
					break;
				case Code.Stind_I4:
					break;
				case Code.Stind_I8:
					break;
				case Code.Stind_R4:
					break;
				case Code.Stind_R8:
					break;
				case Code.Add:
/*
					switch (inst.Left.CliType)
					{
						case CliType.None:
							break;
						case CliType.Int8:
						case CliType.UInt8:
							return _writer.WriteAh(vrleft, vrright);
						case CliType.Int16:
						case CliType.UInt16:
							return _writer.WriteAh(vrleft, vrright);
						case CliType.Int32:
						case CliType.UInt32:
							return _writer.WriteA(vrleft, vrright);
						case CliType.Int64:
						case CliType.UInt64:
							break;
						case CliType.NativeInt:
						case CliType.NativeUInt:
							break;
						case CliType.Float32:
							return _writer.WriteFa(vrleft, vrright);
						case CliType.Float64:
							return _writer.WriteDfa(vrleft, vrright);
						case CliType.ManagedPointer:
						case CliType.ValueType:
						case CliType.ObjectType:
						default:
							break;
					}
*/
					break;
				case Code.Sub:
					break;
				case Code.Mul:
					break;
				case Code.Div:
					break;
				case Code.Div_Un:
					break;
				case Code.Rem:
					break;
				case Code.Rem_Un:
					break;
				case Code.And:
					break;
				case Code.Or:
					break;
				case Code.Xor:
					break;
				case Code.Shl:
					break;
				case Code.Shr:
					break;
				case Code.Shr_Un:
					break;
				case Code.Neg:
					break;
				case Code.Not:
					break;
				case Code.Conv_I1:
					break;
				case Code.Conv_I2:
					break;
				case Code.Conv_I4:
					break;
				case Code.Conv_I8:
					break;
				case Code.Conv_R4:
					break;
				case Code.Conv_R8:
					break;
				case Code.Conv_U4:
					break;
				case Code.Conv_U8:
					break;
				case Code.Callvirt:
					break;
				case Code.Cpobj:
					break;
				case Code.Ldobj:
					break;
				case Code.Ldstr:
					break;
				case Code.Newobj:
					break;
				case Code.Castclass:
					break;
				case Code.Isinst:
					break;
				case Code.Conv_R_Un:
					break;
				case Code.Unbox:
					break;
				case Code.Throw:
					break;
				case Code.Ldfld:
					break;
				case Code.Ldflda:
					break;
				case Code.Stfld:
					break;
				case Code.Ldsfld:
					break;
				case Code.Ldsflda:
					break;
				case Code.Stsfld:
					break;
				case Code.Stobj:
					break;
				case Code.Conv_Ovf_I1_Un:
					break;
				case Code.Conv_Ovf_I2_Un:
					break;
				case Code.Conv_Ovf_I4_Un:
					break;
				case Code.Conv_Ovf_I8_Un:
					break;
				case Code.Conv_Ovf_U1_Un:
					break;
				case Code.Conv_Ovf_U2_Un:
					break;
				case Code.Conv_Ovf_U4_Un:
					break;
				case Code.Conv_Ovf_U8_Un:
					break;
				case Code.Conv_Ovf_I_Un:
					break;
				case Code.Conv_Ovf_U_Un:
					break;
				case Code.Box:
					break;
				case Code.Newarr:
					break;
				case Code.Ldlen:
					break;
				case Code.Ldelema:
					break;
				case Code.Ldelem_I1:
					break;
				case Code.Ldelem_U1:
					break;
				case Code.Ldelem_I2:
					break;
				case Code.Ldelem_U2:
					break;
				case Code.Ldelem_I4:
					break;
				case Code.Ldelem_U4:
					break;
				case Code.Ldelem_I8:
					break;
				case Code.Ldelem_I:
					break;
				case Code.Ldelem_R4:
					break;
				case Code.Ldelem_R8:
					break;
				case Code.Ldelem_Ref:
					break;
				case Code.Stelem_I:
					break;
				case Code.Stelem_I1:
					break;
				case Code.Stelem_I2:
					break;
				case Code.Stelem_I4:
					break;
				case Code.Stelem_I8:
					break;
				case Code.Stelem_R4:
					break;
				case Code.Stelem_R8:
					break;
				case Code.Stelem_Ref:
					break;
				case Code.Ldelem_Any:
					break;
				case Code.Stelem_Any:
					break;
				case Code.Unbox_Any:
					break;
				case Code.Conv_Ovf_I1:
					break;
				case Code.Conv_Ovf_U1:
					break;
				case Code.Conv_Ovf_I2:
					break;
				case Code.Conv_Ovf_U2:
					break;
				case Code.Conv_Ovf_I4:
					break;
				case Code.Conv_Ovf_U4:
					break;
				case Code.Conv_Ovf_I8:
					break;
				case Code.Conv_Ovf_U8:
					break;
				case Code.Refanyval:
					break;
				case Code.Ckfinite:
					break;
				case Code.Mkrefany:
					break;
				case Code.Ldtoken:
					break;
				case Code.Conv_U2:
					break;
				case Code.Conv_U1:
					break;
				case Code.Conv_I:
					break;
				case Code.Conv_Ovf_I:
					break;
				case Code.Conv_Ovf_U:
					break;
				case Code.Add_Ovf:
					break;
				case Code.Add_Ovf_Un:
					break;
				case Code.Mul_Ovf:
					break;
				case Code.Mul_Ovf_Un:
					break;
				case Code.Sub_Ovf:
					break;
				case Code.Sub_Ovf_Un:
					break;
				case Code.Endfinally:
					break;
				case Code.Leave:
					break;
				case Code.Leave_S:
					break;
				case Code.Stind_I:
					break;
				case Code.Conv_U:
					break;
				case Code.Arglist:
					break;
				case Code.Ceq:
					break;
				case Code.Cgt:
					break;
				case Code.Cgt_Un:
					break;
				case Code.Clt:
					break;
				case Code.Clt_Un:
					break;
				case Code.Ldftn:
					break;
				case Code.Ldvirtftn:
					break;
				case Code.Ldarg:
					break;
				case Code.Ldarga:
					break;
				case Code.Starg:
					break;
				case Code.Ldloc:
					break;
				case Code.Ldloca:
					break;
				case Code.Stloc:
					break;
				case Code.Localloc:
				case Code.Endfilter:
				case Code.Unaligned:
				case Code.Volatile:
				case Code.Tail:
				case Code.Initobj:
				case Code.Constrained:
				case Code.Cpblk:
				case Code.Initblk:
				case Code.No:
				case Code.Rethrow:
				case Code.Sizeof:
				case Code.Refanytype:
				case Code.Readonly:
					break;
				default:
					throw new Exception("Invalid opcode: " + ilcode);
			}

			throw new ILNotImplementedException(inst);
		}
	}

	#region ILNotImplementedException


	[Serializable]
	class ILNotImplementedException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public ILNotImplementedException() { }
		public ILNotImplementedException(string message) : base(message) { }
		public ILNotImplementedException(string message, Exception inner) : base(message, inner) { }

		public ILNotImplementedException(TreeInstruction inst) : this(inst.Opcode.Code.ToString()) {  }

		public ILNotImplementedException(Code ilcode) : this(ilcode.ToString()) { }

		protected ILNotImplementedException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}

	#endregion
}
