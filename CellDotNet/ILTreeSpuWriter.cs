using System;
using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace CellDotNet
{
	/// <summary>
	/// This class writes IL trees as SPU instructions.
	/// </summary>
	class ILTreeSpuWriter
	{
		private SpuInstructionWriter _writer;
		private Dictionary<ParameterReference, VirtualRegister> _parameters;
		private Dictionary<VariableReference, VirtualRegister> _variables;

		public void GenerateCode(CompileInfo ci, SpuInstructionWriter writer)
		{
			_writer = writer;

			// Create registers for parameters and variables so that they're 
			// accessible during code generation.
			// TODO: The parameter registers must end up matching the calling convention.
			_parameters = new Dictionary<ParameterReference, VirtualRegister>();
			foreach (ParameterDefinition parameter in ci.MethodDefinition.Parameters)
				_parameters.Add(parameter, _writer.NextRegister());

			_variables = new Dictionary<VariableReference, VirtualRegister>();
			foreach (VariableDefinition variable in ci.MethodDefinition.Body.Variables)
				_variables.Add(variable, _writer.NextRegister());

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
			switch (ilcode)
			{
				case Code.Nop:
					return null;
				case Code.Break:
					break;
				case Code.Ldnull:
					break;
				case Code.Ldc_I4:
					{
						int i = (int) inst.Operand;
						VirtualRegister l = _writer.WriteIlh(i);
						if (i >> 16 == 0)
							return l;
						VirtualRegister u = _writer.WriteIlhu(i >> 16);
						return _writer.WriteOr(l, u);
					}
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
					if (inst.StackType != StackTypeDescription.None)
						throw new NotImplementedException("Cannot return values.");
					return null;
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
					{
						if (inst.Left.StackType.IndirectionLevel != 1) throw new InvalidILTreeException();
						VirtualRegister ptr = GetRegisterForReference(inst.Left);

						VirtualRegister loadedvalue = _writer.WriteLqd(ptr, 0);
						VirtualRegister mask = _writer.WriteCwd(ptr, 0);
						VirtualRegister combined = _writer.WriteShufb(loadedvalue, vrleft, mask);
						_writer.WriteStqd(ptr, combined, 0);
						return null;
					}
				case Code.Stind_I8:
					break;
				case Code.Stind_R4:
					break;
				case Code.Stind_R8:
					break;
				case Code.Add:
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
					return vrleft;
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
					{
						// Do nothing.
						return null;
					}
				case Code.Ldarga:
					break;
				case Code.Starg:
					break;
				case Code.Ldloc:
					return _variables[(VariableReference)inst.Operand];
				case Code.Ldloca:
					break;
				case Code.Stloc:
					VirtualRegister dest = _variables[(VariableReference) inst.Operand];
					_writer.WriteMove(vrleft, dest);
					return null;
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
					throw new InvalidILTreeException("Invalid opcode: " + ilcode);
			}

			throw new ILNotImplementedException(inst);
		}

		private VirtualRegister GetRegisterForReference(TreeInstruction inst)
		{
			if (inst.Operand is ParameterReference)
				return _parameters[(ParameterReference)inst.Operand];
			else if (inst.Operand is VariableReference)
				return _variables[(VariableReference)inst.Operand];
			else
				throw new NotImplementedException();
		}
	}

	#region ILNotImplementedException


	[Serializable]
	public class InvalidILTreeException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public InvalidILTreeException() { }
		public InvalidILTreeException(string message) : base(message) { }
		public InvalidILTreeException(string message, Exception inner) : base(message, inner) { }
		protected InvalidILTreeException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}


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
