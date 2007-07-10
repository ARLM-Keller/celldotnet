using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	class ILReader
	{
		static object s_lock = new object();
		static Dictionary<short, OpCode> s_reflectionmap;
		static Dictionary<short, OpCode> GetReflectionOpCodeMap()
		{
			lock (s_lock)
			{
				if (s_reflectionmap == null)
				{
					s_reflectionmap = new Dictionary<short, OpCode>();
					FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Static | BindingFlags.Public);
					foreach (FieldInfo field in fields)
					{
						if (field.FieldType != typeof(OpCode))
							throw new Exception("Unexpected field type: " + field.FieldType.FullName);

						OpCode oc = (OpCode)field.GetValue(null);
						s_reflectionmap.Add(oc.Value, oc);
					}
				}
			}

			return s_reflectionmap;
		}

		private static Dictionary<short, IROpCode> s_iropcodemap;
		/// <summary>
		/// Returns a map from the IL opcode subset that maps directly to the IR opcodes.
		/// </summary>
		/// <returns></returns>
		static Dictionary<short, IROpCode> GetIROpCodeMap()
		{
			lock (s_lock)
			{
				if (s_iropcodemap == null)
				{
					s_iropcodemap = new Dictionary<short, IROpCode>();

					FieldInfo[] fields = typeof(IROpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
					foreach (FieldInfo field in fields)
					{
						IROpCode oc = (IROpCode)field.GetValue(null);
						s_iropcodemap.Add(oc.ReflectionOpCode.Value, oc);
					}
				}
			}

			return s_iropcodemap;
		}

		private enum ReadState
		{
			None,
			Initial,
			Reading,
			EOF
		}

		private ReadState _state;
		private byte[] _il;
		private int _readoffset;
		private Dictionary<short, OpCode> _ocmap = GetReflectionOpCodeMap();
		private Dictionary<short, IROpCode> _irmap = GetIROpCodeMap();
		private MethodBase _method;
		private MethodBody _body;

		private int _instructionsRead;
		public int InstructionsRead
		{
			get { return _instructionsRead; }
		}

		public ILReader(MethodBase method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			_method = method;
			_body = method.GetMethodBody();
			_il = method.GetMethodBody().GetILAsByteArray();
			_state = ReadState.Initial;
		}

		public bool Read()
		{
			if (_state == ReadState.EOF)
				return false;
			_state = ReadState.Reading;

			_offset = _readoffset;
			_instructionsRead++;
			_operand = null;

			byte b = _il[_readoffset];
			_readoffset++;
			short ocval;
			if (b == 0xFE)
			{
				ocval = (short)((b << 8) | _il[_readoffset]);
				_readoffset++;

				unchecked
				{
					const short tail = (short)0xFE14;
					const short unaligned = (short)0xFE12;
					const short @volatile = (short)0xFE13;

					if (ocval == tail || ocval == unaligned || ocval == @volatile)
						throw new NotImplementedException("tail, unaligned and volatile prefixes are not implemented.");
				}
			}
			else
				ocval = b;

			OpCode srOpcode = _ocmap[ocval];
			ReadInstructionArguments(ref srOpcode);
			IROpCode ircode;
			if (!_irmap.TryGetValue(srOpcode.Value, out ircode))
			{
				throw new Exception("Can't find IR opcode for reflection opcode " + srOpcode.Name +
									". The parsing or simplification probably wasn't performed correcly.");
			}
			_opcode = ircode;

			_instructionSize = _readoffset - Offset;

			// ReadInstructionArguments will have determined a relative offset.
			if (OpCode.FlowControl == FlowControl.Branch || OpCode.FlowControl == FlowControl.Cond_Branch)
			{
				_operand = (Offset + InstructionSize) + (int) _operand;
			}


			if (_readoffset == _il.Length)
				_state = ReadState.EOF;

			return true;
		}

		private int _offset;
		/// <summary>
		/// The offset, in bytes, of the current instruction.
		/// </summary>
		public int Offset
		{
			get { return _offset; }
		}

		private int _instructionSize;
		/// <summary>
		/// The size of the current instruction in bytes, including prefixes and tokens etc.
		/// </summary>
		public int InstructionSize
		{
			get { return _instructionSize; }
		}


		/// <summary>
		/// Four-byte integers in the IL stream are little-endian.
		/// </summary>
		/// <returns></returns>
		private int ReadInt32()
		{
			int i = ((_il[_readoffset] | (_il[_readoffset + 1] << 8)) |
				(_il[_readoffset + 2] << 0x10)) | (_il[_readoffset + 3] << 0x18);

			_readoffset += 4;

			return i;
		}

		/// <summary>
		/// Finishes reading the specified opcode from the il.
		/// Also simplifies the code by converting most macro instructions.
		/// </summary>
		/// <param name="srOpcode"></param>
		private void ReadInstructionArguments(ref OpCode srOpcode)
		{
			{
				// stloc.
				int varindex = -1;
				if (srOpcode == OpCodes.Stloc)
					varindex = ReadInt32();
				else if (srOpcode == OpCodes.Stloc_S)
					varindex = _il[_readoffset++];
				else if (srOpcode.Value >= OpCodes.Stloc_0.Value && srOpcode.Value <= OpCodes.Stloc_3.Value)
					varindex = srOpcode.Value - OpCodes.Stloc_0.Value;

				if (varindex != -1)
				{
					_operand = _body.LocalVariables[varindex];
					srOpcode = OpCodes.Stloc;
					return;
				}
			}

			{
				// ldloc.
				int varindex = -1;
				if (srOpcode == OpCodes.Ldloc)
					varindex = ReadInt32();
				else if (srOpcode == OpCodes.Ldloc_S)
					varindex = _il[_readoffset++];
				else if (srOpcode.Value >= OpCodes.Ldloc_0.Value && srOpcode.Value <= OpCodes.Ldloc_3.Value)
					varindex = srOpcode.Value - OpCodes.Ldloc_0.Value;

				if (varindex != -1)
				{
					_operand = _body.LocalVariables[varindex];
					srOpcode = OpCodes.Ldloc;
					return;
				}
			}

			{
				// ldloca.
				int index = -1;
				if (srOpcode == OpCodes.Ldloca)
					index = ReadInt32();
				else if (srOpcode == OpCodes.Ldloca_S)
					index = _il[_readoffset++];

				if (index != -1)
				{
					_operand = _body.LocalVariables[index];
					srOpcode = OpCodes.Ldloca;
					return;
				}
			}

			{
				// starg.
				int argnum = -1;
				if (srOpcode == OpCodes.Starg)
					argnum = ReadInt32();
				else if (srOpcode == OpCodes.Starg_S)
					argnum = _il[_readoffset++];

				if (argnum != -1)
				{
					srOpcode = OpCodes.Starg;
					_operand = _method.GetParameters()[argnum];
					return;
				}
			}

			{
				// ldarg(a)
				int index = -1;
				if (srOpcode == OpCodes.Ldarg)
					index = ReadInt32();
				else if (srOpcode == OpCodes.Ldarg_S)
				{
					index = _il[_readoffset++];
					srOpcode = OpCodes.Ldarg;
				}
				else if (srOpcode.Value >= OpCodes.Ldarg_0.Value && srOpcode.Value <= OpCodes.Ldarg_3.Value)
				{
					index = srOpcode.Value - OpCodes.Ldarg_0.Value;
					srOpcode = OpCodes.Ldarg;
				}
				else if (srOpcode == OpCodes.Ldarga)
					index = ReadInt32();
				else if (srOpcode == OpCodes.Ldarga_S)
				{
					index = _il[_readoffset++];
					srOpcode = OpCodes.Ldarga;
				}

				if (index != -1)
				{
					_operand = _method.GetParameters()[index];
					return;
				}
			}

			{
				// switch
				if (srOpcode == OpCodes.Switch)
					throw new NotImplementedException("switch");
			}

			{
				// beq.
				if (srOpcode == OpCodes.Beq)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Beq_S)
				{
					srOpcode = OpCodes.Beq;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// bge(.un)
				if (srOpcode == OpCodes.Bge)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Bge_S)
				{
					srOpcode = OpCodes.Bge;
					_operand = ReadInt8();
					return;
				}
				else if (srOpcode == OpCodes.Bge_Un)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Bge_Un_S)
				{
					srOpcode = OpCodes.Bge_Un;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// bgt(.un)
				if (srOpcode == OpCodes.Bgt)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Bgt_S)
				{
					srOpcode = OpCodes.Bgt;
					_operand = ReadInt8();
					return;
				}
				else if (srOpcode == OpCodes.Bgt_Un)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Bgt_Un_S)
				{
					srOpcode = OpCodes.Bgt_Un;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// ble(.un)
				if (srOpcode == OpCodes.Ble)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Ble_S)
				{
					srOpcode = OpCodes.Ble;
					_operand = ReadInt8();
					return;
				}
				else if (srOpcode == OpCodes.Ble_Un)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Ble_Un_S)
				{
					srOpcode = OpCodes.Ble_Un;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// blt(.un)
				if (srOpcode == OpCodes.Blt)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Blt_S)
				{
					srOpcode = OpCodes.Blt;
					_operand = ReadInt8();
					return;
				}
				else if (srOpcode == OpCodes.Blt_Un)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Blt_Un_S)
				{
					srOpcode = OpCodes.Blt_Un;
					_operand = ReadInt8();
					return;
				}
			}


			{
				// bne.un
				if (srOpcode == OpCodes.Bne_Un)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Bne_Un_S)
				{
					srOpcode = OpCodes.Bne_Un;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// br
				if (srOpcode == OpCodes.Br)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Br_S)
				{
					srOpcode = OpCodes.Br;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// br(false/null/zero).
				if (srOpcode == OpCodes.Brfalse)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Brfalse_S)
				{
					srOpcode = OpCodes.Brfalse;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// br(true/inst).
				if (srOpcode == OpCodes.Brtrue)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Brtrue_S)
				{
					srOpcode = OpCodes.Brtrue;
					_operand = ReadInt8();
					return;
				}
			}

			{
				// call/calli.
				if (srOpcode == OpCodes.Call || srOpcode == OpCodes.Calli)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveMethod(token);
					return;
				}
			}

			{
				// jmp.
				if (srOpcode == OpCodes.Jmp)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveMethod(token);
				}
			}

			{
				// ldc.
				if (srOpcode == OpCodes.Ldc_I4)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode.Value >= OpCodes.Ldc_I4_0.Value && srOpcode.Value <= OpCodes.Ldc_I4_8.Value)
				{
					_operand = srOpcode.Value - OpCodes.Ldc_I4_0.Value;
					srOpcode = OpCodes.Ldc_I4;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_I4_M1)
				{
					_operand = -1;
					srOpcode = OpCodes.Ldc_I4;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_I4_S)
				{
					_operand = ReadInt8();
					srOpcode = OpCodes.Ldc_I4;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_I8)
				{
					int num3 = (((_il[_readoffset + 0] << 0x18) | (_il[_readoffset + 1] << 0x10)) | (_il[_readoffset + 2] << 8)) | _il[_readoffset + 3];
					int num4 = (((_il[_readoffset + 4] << 0x18) | (_il[_readoffset + 5] << 0x10)) | (_il[_readoffset + 6] << 8)) | _il[_readoffset + 7];
					_operand = ((long)((ulong)num4)) | (num3 << 0x20);

					_readoffset += 8;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_R4)
				{
					_operand = BitConverter.ToSingle(_il, _readoffset);
					_readoffset += 4;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_R8)
				{
					_operand = BitConverter.ToDouble(_il, _readoffset);
					_readoffset += 8;
					return;
				}
			}

			{
				// ldftn.
				if (srOpcode == OpCodes.Ldftn)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveMethod(token);
					return;
				}
			}

			{
				// leave.
				if (srOpcode == OpCodes.Leave)
				{
					_operand = ReadInt32();
					return;
				}
				else if (srOpcode == OpCodes.Leave_S)
				{
					_operand = ReadInt8();
					srOpcode = OpCodes.Leave;
					return;
				}
			}

			{
				if (srOpcode == OpCodes.Callvirt || srOpcode == OpCodes.Ldvirtftn)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveMethod(token);
					return;
				}
			}

			{
				if (srOpcode == OpCodes.Castclass || srOpcode == OpCodes.Cpobj || srOpcode == OpCodes.Initobj ||
					srOpcode == OpCodes.Isinst || srOpcode == OpCodes.Box || srOpcode == OpCodes.Ldelema ||
					srOpcode == OpCodes.Ldobj || srOpcode == OpCodes.Mkrefany || srOpcode == OpCodes.Newarr ||
					srOpcode == OpCodes.Refanyval || srOpcode == OpCodes.Sizeof ||
					srOpcode == OpCodes.Stobj || srOpcode == OpCodes.Unbox)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveType(token);
					return;
				}
				if (srOpcode == OpCodes.Newobj)
				{
					int token = ReadInt32();
					_operand = _method.Module.ResolveMethod(token);
					return;
				}
			}

			if (srOpcode == OpCodes.Ldfld || srOpcode == OpCodes.Ldflda ||
				srOpcode == OpCodes.Ldsfld || srOpcode == OpCodes.Ldsflda ||
				srOpcode == OpCodes.Stfld || srOpcode == OpCodes.Stsfld)
			{
				int token = ReadInt32();
				_operand = _method.Module.ResolveField(token);
				return;
			}

			if (srOpcode == OpCodes.Ldstr)
			{
				int token = ReadInt32();
				_operand = _method.Module.ResolveString(token);
				return;
			}

			if (srOpcode == OpCodes.Ldtoken)
			{
				int token = ReadInt32();
				_operand = token;
				return;
			}
		}

		/// <summary>
		/// Reads a single signed byte from the IL and returns it as an int.
		/// </summary>
		/// <returns></returns>
		private int ReadInt8()
		{
			return (sbyte) _il[_readoffset++];
		}

		private object _operand;
		public object Operand
		{
			get { return _operand; }
		}

		private IROpCode _opcode;
		public IROpCode OpCode
		{
			get { return _opcode; }
		}
	}
}
