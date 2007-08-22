using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;

namespace CellDotNet
{
	[DebuggerDisplay("{DebuggerDisplay}")]
	class ILReader
	{
		private string DebuggerDisplay
		{
			get { return string.Format("{0} {1} ({2})", OpCode.Name, Operand, Offset); }
		}

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

//		/// <summary>
//		/// Used to decompose branch macro instructions.
//		/// The value part is branch offset.
//		/// </summary>
//		Stack<KeyValuePair<IROpCode, int>> _opcodestack = new Stack<KeyValuePair<IROpCode, int>>();

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
			Initialize(method.GetMethodBody().GetILAsByteArray());
		}

		/// <summary>
		/// For unit testing.
		/// </summary>
		/// <param name="il"></param>
		public ILReader(byte[] il)
		{
			Initialize(il);
		}

		private void Initialize(byte[] il)
		{
			_il = il;
			_state = ReadState.Initial;
		}

		public bool Read()
		{
			if (_state == ReadState.EOF)
				return false;
			_state = ReadState.Reading;

			_instructionsRead++;
			_offset = _readoffset;
			_operand = null;

//			if (_opcodestack.Count > 0)
//			{
//				KeyValuePair<IROpCode, int> pair = _opcodestack.Pop();
//				_opcode = pair.Key;
//				_operand = pair.Value;
//
//				// Wouldn't be good if both instructions got the same offset.
//				// We give the offset to the first of the decomposed instructions,
//				// and since we're at this place, we're not at the first one.
//				_offset = -1;
//				return true;
//			}

			ushort ocval;
			if (_il[_readoffset] == 0xfe)
			{
				Utilities.Assert(_readoffset + 1 < _il.Length, "_readoffset + 1 < _il.Length");
				ocval = (ushort) ((_il[_readoffset] << 8 ) | _il[_readoffset + 1]);
				_readoffset += 2;
			}
			else
			{
				ocval = _il[_readoffset];
				_readoffset += 1;
			}
			
			OpCode srOpcode;
			try
			{
				srOpcode = _ocmap[(short)ocval];
			}
			catch (KeyNotFoundException)
			{
				throw new ILParseException(
					string.Format("Opcode bytes {0:x4} from IL offset {1:x6} does not exist in the opcode map. This indicates invalid IL or a bug in this class.", ocval, Offset));
			}
			if (srOpcode.OpCodeType == OpCodeType.Prefix)
				throw new NotImplementedException("Prefix opcodes are not implemented. Used prefix is: " + srOpcode.Name + ".");

			try
			{
				ReadInstructionArguments(srOpcode);
				RemoveMacro(ref srOpcode);
			}
			catch (Exception e)
			{
				throw new ILParseException(string.Format("An error occurred while reading IL starting at offset {0:x4}.", Offset), e);
			}

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
		/// Finishes reading the specified opcode from the il and prepares the opcode operand.
		/// </summary>
		/// <param name="srOpcode"></param>
		private void ReadInstructionArguments(OpCode srOpcode)
		{
			switch (srOpcode.OperandType)
			{
				case OperandType.InlineBrTarget:
					int xtoken;
					uint xindex;
					_operand = ReadInt32();
					break;
				case OperandType.InlineField:
					xtoken = ReadInt32();
					_operand = _method.Module.ResolveField(xtoken);
					break;
				case OperandType.InlineI:
					_operand = ReadInt32();
					break;
				case OperandType.InlineI8:
					_operand = ReadInt64();
					break;
				case OperandType.InlineMethod:
					xtoken = ReadInt32();
					_operand = _method.Module.ResolveMethod(xtoken);
					break;
				case OperandType.InlineNone:
					break;
//				case OperandType.InlinePhi:
//					break;
				case OperandType.InlineR:
					_operand = ReadDouble();
					break;
				case OperandType.InlineSig:
					xtoken = ReadInt32();
					_operand = _method.Module.ResolveSignature(xtoken);
					break;
				case OperandType.InlineString:
					xtoken = ReadInt32();
					_operand = _method.Module.ResolveString(xtoken);
					break;
				case OperandType.InlineSwitch:
					throw new NotImplementedException("Switch is not implemented.");
				case OperandType.InlineTok:
					_operand = ReadInt32();
					break;
				case OperandType.InlineType:
					xtoken = ReadInt32();
					_operand = _method.Module.ResolveType(xtoken);
					break;
				case OperandType.ShortInlineVar:
					xindex = (uint) ReadInt8();
					if (srOpcode == OpCodes.Ldarg_S || srOpcode == OpCodes.Ldarga_S || srOpcode == OpCodes.Starg_S)
						_operand = _method.GetParameters()[xindex];
					else
					{
						Utilities.Assert(srOpcode == OpCodes.Ldloc_S || srOpcode == OpCodes.Ldloca_S || srOpcode == OpCodes.Stloc_S,
						                 "Not loc?!");
						_operand = _body.LocalVariables[(int) xindex];
					}
					break;
				case OperandType.InlineVar:
					xindex = (uint) ReadInt16();
					if (srOpcode == OpCodes.Ldarg || srOpcode == OpCodes.Ldarga || srOpcode == OpCodes.Starg)
						_operand = _method.GetParameters()[xindex];
					else
					{
						Utilities.Assert(srOpcode == OpCodes.Ldloc || srOpcode == OpCodes.Ldloca || srOpcode == OpCodes.Stloc, "Not loc??");
						_operand = _body.LocalVariables[(int) xindex];
					}
					break;
				case OperandType.ShortInlineBrTarget:
					_operand = ReadInt8();
					break;
				case OperandType.ShortInlineI:
					// ldc.i4.s and unaligned prefix. unaligned only uses small positive values, so 
					// we can pretend it's signed in both cases.
					_operand = ReadInt8();
					break;
				case OperandType.ShortInlineR:
					_operand = ReadSingle();
					break;
				default:
					throw new ILSemanticErrorException("Unknown operand type: " + srOpcode.OperandType);
			}
		}

		private unsafe float ReadSingle()
		{
			int i = ReadInt32();
			return *(((float*)&i));
		}

		private unsafe double ReadDouble()
		{
			long i = ReadInt64();
			return *(((double*)&i));
		}

		/// <summary>
		/// Gets rid of macro opcodes except for some of the branches.
		/// </summary>
		/// <param name="srOpcode"></param>
		private void RemoveMacro(ref OpCode srOpcode)
		{
			{
				// stloc.
				if (srOpcode == OpCodes.Stloc_S)
				{
					srOpcode = OpCodes.Stloc;
					return;
				}
				else if (srOpcode.Value >= OpCodes.Stloc_0.Value && srOpcode.Value <= OpCodes.Stloc_3.Value)
				{
					int varindex = srOpcode.Value - OpCodes.Stloc_0.Value;
					_operand = _body.LocalVariables[varindex];
					srOpcode = OpCodes.Stloc;
				}
			}

			{
				// ldloc.
				if (srOpcode == OpCodes.Ldloc_S)
				{
					srOpcode = OpCodes.Ldloc;
					return;
				}
				else if (srOpcode.Value >= OpCodes.Ldloc_0.Value && srOpcode.Value <= OpCodes.Ldloc_3.Value)
				{
					int varindex = srOpcode.Value - OpCodes.Ldloc_0.Value;
					_operand = _body.LocalVariables[varindex];
					srOpcode = OpCodes.Ldloc;
					return;
				}
			}

			{
				// ldloca.
				if (srOpcode == OpCodes.Ldloca_S)
				{
					srOpcode = OpCodes.Ldloca;
					return;
				}
			}

			{
				// starg.
				if (srOpcode == OpCodes.Starg_S)
				{
					srOpcode = OpCodes.Starg;
					return;
				}
			}

			{
				// ldarg(a)
				if (srOpcode == OpCodes.Ldarg_S)
				{
					srOpcode = OpCodes.Ldarg;
					return;
				}
				else if (srOpcode.Value >= OpCodes.Ldarg_0.Value && srOpcode.Value <= OpCodes.Ldarg_3.Value)
				{
					int index = srOpcode.Value - OpCodes.Ldarg_0.Value;
					_operand = _method.GetParameters()[index];
					srOpcode = OpCodes.Ldarg;
					return;
				}
				else if (srOpcode == OpCodes.Ldarga_S)
				{
					srOpcode = OpCodes.Ldarga;
					return;
				}
			}

			{
				// beq.
				if (srOpcode == OpCodes.Beq_S)
					srOpcode = OpCodes.Beq;

//				if (srOpcode == OpCodes.Beq)
//				{
//					srOpcode = OpCodes.Ceq;
//					_opcodestack.Push(new KeyValuePair<IROpCode, int>(IROpCodes.Brtrue, (int) _operand));
//					_operand = null;
//					return;
//				}
			}

			{
				// bge(.un)
				if (srOpcode == OpCodes.Bge_S)
				{
					srOpcode = OpCodes.Bge;
					return;
				}

				if (srOpcode == OpCodes.Bge_Un_S)
				{
					srOpcode = OpCodes.Bge_Un;
					return;
				}
			}

			{
				// bgt(.un)
				if (srOpcode == OpCodes.Bgt_S)
				{
					srOpcode = OpCodes.Bgt;
					return;
				}
				if (srOpcode == OpCodes.Bgt_Un_S)
				{
					srOpcode = OpCodes.Bgt_Un;
					return;
				}
			}

			{
				// ble(.un)
				if (srOpcode == OpCodes.Ble_S)
				{
					srOpcode = OpCodes.Ble;
					return;
				}
				if (srOpcode == OpCodes.Ble_Un_S)
				{
					srOpcode = OpCodes.Ble_Un;
					return;
				}
			}

			{
				// blt(.un)
				if (srOpcode == OpCodes.Blt_S)
				{
					srOpcode = OpCodes.Blt;
					return;
				}
				if (srOpcode == OpCodes.Blt_Un_S)
				{
					srOpcode = OpCodes.Blt_Un;
					return;
				}
			}

			{
				// bne.un
				if (srOpcode == OpCodes.Bne_Un_S)
				{
					srOpcode = OpCodes.Bne_Un;
					return;
				}
			}

			{
				// br
				if (srOpcode == OpCodes.Br_S)
				{
					srOpcode = OpCodes.Br;
					return;
				}
			}

			{
				// br(false/null/zero).
				if (srOpcode == OpCodes.Brfalse_S)
				{
					srOpcode = OpCodes.Brfalse;
					return;
				}
			}

			{
				// br(true/inst).
				if (srOpcode == OpCodes.Brtrue_S)
				{
					srOpcode = OpCodes.Brtrue;
					return;
				}
			}

			{
				// ldc.
				if (srOpcode.Value >= OpCodes.Ldc_I4_M1.Value && srOpcode.Value <= OpCodes.Ldc_I4_8.Value)
				{
					_operand = srOpcode.Value - OpCodes.Ldc_I4_0.Value;
					srOpcode = OpCodes.Ldc_I4;
					return;
				}
				else if (srOpcode == OpCodes.Ldc_I4_S)
				{
					srOpcode = OpCodes.Ldc_I4;
					return;
				}
			}

			{
				// leave.
				if (srOpcode == OpCodes.Leave_S)
				{
					srOpcode = OpCodes.Leave;
					return;
				}
			}

			if (srOpcode == OpCodes.Ldtoken)
			{
				int token = ReadInt32();
				_operand = token;
				return;
			}
		}

		/// <summary>
		/// Four-byte integers in the IL stream are little-endian.
		/// </summary>
		/// <returns></returns>
		private short ReadInt16()
		{
			short i = (short) (_il[_readoffset] | (_il[_readoffset + 1] << 8));

			_readoffset += 2;

			return i;
		}

		/// <summary>
		/// Two-byte integers in the IL stream are little-endian.
		/// </summary>
		/// <returns></returns>
		private int ReadInt32()
		{
			int i = ((_il[_readoffset] | (_il[_readoffset + 1] << 8)) |
					 (_il[_readoffset + 2] << 0x10)) | (_il[_readoffset + 3] << 0x18);

			_readoffset += 4;

			return i;
		}

		private long ReadInt64()
		{
			long num3 = (((_il[_readoffset + 0]) | (_il[_readoffset + 1] << 8)) | (_il[_readoffset + 2] << 0x10)) | (_il[_readoffset + 3] << 0x18);
			long num4 = (((_il[_readoffset + 4]) | (_il[_readoffset + 5] << 8)) | (_il[_readoffset + 6] << 0x10)) | (_il[_readoffset + 7] << 0x18);
			_readoffset += 8;
			return num3 | (num4 << 0x20);
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
