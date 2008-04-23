// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using JetBrains.Annotations;

namespace CellDotNet.Intermediate
{

	#region IlReaderWrapper

	/// <summary>
	/// Wraps an ILReader and expands some IL instructions to a sequence of instructions.
	/// </summary>
	class IlReaderWrapper
	{

		private int _variableCount;

		private readonly ILReader _ilreader;
		public ILReader ILReader
		{
			get { return _ilreader; }
		}

		struct ILRecord
		{
			public readonly OpCode OpCode;
			public readonly object Operand;
			public int InstructionSize;

			public bool IsEmpty
			{
				get { return OpCode == null; }
			}

			public ILRecord(OpCode opCode, object operand, int instructionSize)
			{
				OpCode = opCode;
				Operand = operand;
				InstructionSize = instructionSize;
			}
		}

		private ILRecord _currentRecord;

		private readonly Queue<ILRecord> _instQueue = new Queue<ILRecord>();

		private MethodVariable _lastCreatedMethodVariable;
		public MethodVariable LastCreatedMethodVariable
		{
			get { return _lastCreatedMethodVariable; }
		}

		public IlReaderWrapper(ILReader reader)
		{
			_ilreader = reader;	
		}

		public void Reset()
		{
			_ilreader.Reset();
			_variableCount = 0;
		}

		public int InstructionSize
		{
			get { return _currentRecord.InstructionSize; }
		}

		public bool Read(StackTypeDescription type)
		{
			_lastCreatedMethodVariable = null;
			if (_instQueue.Count > 0)
			{
				_currentRecord = _instQueue.Dequeue();
				return true;
			}

			if (!_ilreader.Read())
				return false;

			OpCode opcode = _ilreader.OpCode;
			object operand = _ilreader.Operand;

			if (opcode == OpCodes.Dup)
			{
				if (type == StackTypeDescription.None)
					throw new ArgumentException("Incompatible type.");

				MethodVariable mv = new MethodVariable(_variableCount + 2000, type);
				_currentRecord = new ILRecord(OpCodes.Stloc, mv, 0);
				_instQueue.Enqueue(new ILRecord(OpCodes.Ldloc, mv, 0));
				_instQueue.Enqueue(new ILRecord(OpCodes.Ldloc, mv, _ilreader.InstructionSize));

				_lastCreatedMethodVariable = mv;
			}
			else
			{
				_currentRecord = new ILRecord(opcode, operand, _ilreader.InstructionSize);
			}

			return true;
		}

		public int Offset
		{
			get { return _ilreader.Offset; }
		}

		public OpCode OpCode
		{
			get
			{
				Utilities.Assert(!_currentRecord.IsEmpty, "!_currentRecord.IsEmpty");
				return _currentRecord.OpCode;
			}
		}

		public object Operand
		{
			get
			{
				Utilities.Assert(!_currentRecord.IsEmpty, "!_currentRecord.IsEmpty");
				return _currentRecord.Operand;
			}
		}
	}

	#endregion

	[DebuggerDisplay("{DebuggerDisplay}")]
	class ILReader
	{
		private string DebuggerDisplay
		{
			get { return string.Format("{0} {1} ({2})", OpCode.Name, Operand, Offset); }
		}

		static readonly object s_lock = new object();
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
						Utilities.Assert(field.FieldType == typeof (OpCode), 
							"Unexpected field type: " + field.FieldType.FullName);

						OpCode oc = (OpCode)field.GetValue(null);
						s_reflectionmap.Add(oc.Value, oc);
					}
				}
			}

			return s_reflectionmap;
		}

		public enum ReadState
		{
			None,
			Initial,
			Reading,
			EOF
		}

		private static readonly FieldInfo s_Ilstream;
		private static readonly FieldInfo s_Lenghtfield;

		static ILReader()
		{
			if (Environment.OSVersion.Platform == PlatformID.Unix) // mono
			{
				s_Ilstream = typeof(ILGenerator).GetField("code", BindingFlags.NonPublic | BindingFlags.Instance);
				s_Lenghtfield = typeof(ILGenerator).GetField("code_len", BindingFlags.NonPublic | BindingFlags.Instance);
			}
			else
			{
				s_Ilstream = typeof(ILGenerator).GetField("m_ILStream", BindingFlags.NonPublic | BindingFlags.Instance);
				s_Lenghtfield = typeof(ILGenerator).GetField("m_length", BindingFlags.NonPublic | BindingFlags.Instance);
			}
			Debug.Assert(s_Ilstream != null && s_Lenghtfield != null);
		}

		public void GetILBuffer(MethodBase method, out byte[] il, out int ilLength)
		{
			if (_method is DynamicMethod)
			{
				ILGenerator gen = ((DynamicMethod)_method).GetILGenerator();
				il = (byte[])s_Ilstream.GetValue(gen);
				ilLength = (int)s_Lenghtfield.GetValue(gen);
			}
			else
			{
				MethodBody body = _method.GetMethodBody();
				il = body.GetILAsByteArray();
				ilLength = il.Length;
			}
		}


		private ReadState _state;
		private byte[] _il;
		private int _readoffset;
		private readonly Dictionary<short, OpCode> _ocmap = GetReflectionOpCodeMap();
		private readonly MethodBase _method;
		private readonly MethodBody _body;

		public ReadState State
		{
			get { return _state; }
		}

		private int _instructionsRead;
		public int InstructionsRead
		{
			get { return _instructionsRead; }
		}

		private LocalVariableInfo GetLocalVariable(int index)
		{
			if (_body == null)
			{
				if (_method is DynamicMethod)
					throw new NotSupportedException("Use of local variables in DynamicMethod is not supported.");
				Debug.Fail("DynamicMethod");
			}
			return _body.LocalVariables[index];
		}

		public ILReader(MethodBase method)
		{
			if (method == null)
				throw new ArgumentNullException("method");

			Utilities.PretendVariableIsUsed(DebuggerDisplay);

			_method = method;
			if (!(method is DynamicMethod))
				_body = method.GetMethodBody();

			int illength;
			byte[] il;
			GetILBuffer(method, out il, out illength);
			if (il.Length != illength)
			{
				byte[] orig = il;
				il = new byte[illength];
				Buffer.BlockCopy(orig, 0, il, 0, illength);
			}

			Initialize(il);
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

		public void Reset()
		{
			_state = ReadState.Initial;
			_readoffset = default(int);
			_operand = default(object);
			_opcode = default(OpCode);
			_instructionsRead = default(int);
		}

		public bool Read()
		{
			if (_readoffset == _il.Length)
			{
				_state = ReadState.EOF;
				return false;
			}

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

			_opcode = srOpcode;

			_instructionSize = _readoffset - Offset;

			// ReadInstructionArguments will have determined a relative offset.
			if (OpCode.FlowControl == FlowControl.Branch || OpCode.FlowControl == FlowControl.Cond_Branch)
			{
				_operand = (Offset + InstructionSize) + (int) _operand;
			}


//			if (_readoffset == _il.Length)
//				_state = ReadState.EOF;

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
					{
						if ((_method.CallingConvention & CallingConventions.HasThis) != 0 && srOpcode != OpCodes.Newobj)
						{
							_operand = _method.GetParameters()[xindex-1];
						} else
						{
							_operand = _method.GetParameters()[xindex];
						}
					}
					else
					{
						Utilities.Assert(srOpcode == OpCodes.Ldloc_S || srOpcode == OpCodes.Ldloca_S || srOpcode == OpCodes.Stloc_S,
										 "Not loc?!");
						_operand = GetLocalVariable((int)xindex);
					}
					break;
				case OperandType.InlineVar:
					xindex = (uint) ReadInt16();
					if (srOpcode == OpCodes.Ldarg || srOpcode == OpCodes.Ldarga || srOpcode == OpCodes.Starg)
						if ((_method.CallingConvention & CallingConventions.HasThis) != 0 && srOpcode != OpCodes.Newobj)
						{
							_operand = _method.GetParameters()[xindex - 1];
						}
						else
						{
							_operand = _method.GetParameters()[xindex];
						}
					else
					{
						Utilities.Assert(srOpcode == OpCodes.Ldloc || srOpcode == OpCodes.Ldloca || srOpcode == OpCodes.Stloc, "Not loc??");
						_operand = GetLocalVariable((int) xindex);
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

		private float ReadSingle()
		{
			int i = ReadInt32();
			return Utilities.ReinterpretAsSingle(i);
		}

		private double ReadDouble()
		{
			long l = ReadInt64();
			return Utilities.ReinterpretAsDouble(l);
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
					_operand = GetLocalVariable(varindex);
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
					_operand = GetLocalVariable(varindex);
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

				// Q: What should _operand be when it's a 'this' argument?

				if (srOpcode == OpCodes.Ldarg_S)
				{
					srOpcode = OpCodes.Ldarg;
					return;
				}
				else if (srOpcode.Value >= OpCodes.Ldarg_0.Value && srOpcode.Value <= OpCodes.Ldarg_3.Value)
				{
//						if (!_method.IsStatic)
//						throw new NotSupportedException("Instances are not supported.");
					int index = srOpcode.Value - OpCodes.Ldarg_0.Value;

					if ((_method.CallingConvention & CallingConventions.HasThis) != 0 && srOpcode != OpCodes.Newobj)
					{
						if (index != 0)
							_operand = _method.GetParameters()[index-1];
						else
							_operand = null; //There is no ParameterInfo to represent the this parameter.
					}
					else
					{
						_operand = _method.GetParameters()[index];
					}
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
			long num3 = (((_il[_readoffset + 0]) | (_il[_readoffset + 1] << 8)) | (_il[_readoffset + 2] << 0x10)) | (uint)(_il[_readoffset + 3] << 0x18);
			long num4 = (uint)(((_il[_readoffset + 4]) | (_il[_readoffset + 5] << 8)) | (_il[_readoffset + 6] << 0x10)) | (uint)(_il[_readoffset + 7] << 0x18);
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

		private object	_operand;
		public object Operand
		{
			get { return _operand; }
		}

		private OpCode _opcode;
		public OpCode OpCode
		{
			get { return _opcode; }
		}
	}
}
