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

namespace CellDotNet.Spe
{
	/// <summary>
	/// Bit-layout of an instruction.
	/// </summary>
	public enum SpuInstructionFormat
	{
		None,
		/// <summary>
		/// Register rb, ra, rt.
		/// Assembler format: "rt,ra,rb".
		/// 11 bit instruction code.
		/// </summary>
		RR,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra".
		/// 11 bit instruction code.
		/// The RR variant where the bits for the first register (b, bits 11-17) are not used.
		/// </summary>
		RR2,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt".
		/// 11 bit instruction code.
		/// </summary>
		RR1,
		/// <summary>
		/// Register rt, rb, ra, rc.
		/// Assembler format: "rt,ra,rb,rc".
		/// 4 bit instruction code.
		/// </summary>
		Rrr,
		/// <summary>   
		/// Register ra, rt.
		/// Assembler format: "rt,ra,value".
		/// 11 bit instruction code.
		/// 7 bit immediate.
		/// </summary>
		RI7,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra,scale".
		/// 10 bit instruction code.
		/// 8 bit immediate.
		/// </summary>
		RI8,
		/// <summary>
		/// Register ra, rt.
		/// Assembler format: "rt,ra,value".
		/// 8 bit instruction code.
		/// 8 bit immediate.
		/// </summary>
		RI10,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt,symbol".
		/// 9 bit instruction code.
		/// 10 bit immediate.
		/// </summary>
		RI16,
		/// <summary>
		/// No registers.
		/// Assembler format: "symbol".
		/// 9 bit instruction code.
		/// 16 bit immediate.
		/// An RI16 variant where the last register bits are ignored.
		/// </summary>
		RI16NoRegs,
		/// <summary>
		/// Register rt.
		/// Assembler format: "rt,symbol".
		/// 7 bit instruction code.
		/// 18 bit immediate.
		/// </summary>
		RI18,
		/// <summary>
		/// For the channel instructions.
		/// </summary>
		Channel,
		Weird,
		/// <summary>
		/// For pseudo-instructions.
		/// </summary>
		Custom,
		/// <summary>
		/// Used for the stop instruction.
		/// </summary>
		RI14
	}

	[Flags]
	enum SpuInstructionPart
	{
		None = 0,
		Rt = 1 << 0,
		Ra = 1 << 1,
		Rb = 1 << 2,
		Rc = 1 << 3,
		Sa = 1 << 4,
		Ca = 1 << 5,
		Immediate = 1 << 6,
	}

	/// <summary>
	/// Special features that an opcode can have, like D and E bits or branch hint offset.
	/// </summary>
	[Flags]
	public enum SpuOpCodeSpecialFeatures
	{
		None = 0,
		/// <summary>
		/// Sync opcode.
		/// <para>This is bit 11.</para>
		/// <para>
		/// The C feature bit causes channel synchronization to occur before instruction synchronization occurs. 
		/// Channel synchronization allows an SPU state modified through channel instructions to affect execution.
		/// </para>
		/// </summary>
		BitC = 1 << 0,

		/// <summary>
		/// These are always bits 12 and 13.
		/// </summary>
		BitDE = (1 << 1) | (1 << 2),

		/// <summary>
		/// Used for hbr to trigger inline prefetching. 
		/// Both offset and register is ignored when this is set.
		/// <para>This is bit 11.</para>
		/// <para>When the P feature bit is set, the instruction ignores the value of RA.</para>
		/// </summary>
		BitP = 1 << 3,

		/// <summary>
		/// ROH and ROL.
		/// </summary>
		BranchHintOffset = 1 << 4,

		/// <summary>
		/// Means that the instruction in not a SPU instruction, but rather an instruction
		/// with meaning to the compiler. Used for move and ret.
		/// </summary>
		Pseudo = 1 << 5,

		RegisterRtNotWritten = 1 << 6,
		RegisterRtRead = 1 << 7,
		ChannelAccess = 1 << 8,
		MemoryRead = 1 << 9,
		MemoryWrite = 1 << 10,
		MethodCall = 1 << 11,
	}

	/// <summary>
	/// See the Linux ABI for details on some of these.
	/// </summary>
	enum SpuStopCode
	{
		None = 0,

		/// <summary>
		/// Only used as an offset for other codes.
		/// </summary>
		CustomStopCodeBase = 0xa00,

		/// <summary>
		/// Used to test that <see cref="SpeContext"/> picks up stop codes in the range.
		/// </summary>
		PpeCallFailureTest = CustomStopCodeBase + 1,

		PpeCall = CustomStopCodeBase + 2,

		// Reserved ranges - custom runtime codes.
		OutOfMemory = 0x2010,


		// Reserved ranges - standard codes.
		/// <summary>
		/// Don't use this for app codes.
		/// </summary>
		ExitFailure = 0x2000,
		/// <summary>
		/// Don't use this for app codes.
		/// </summary>
		ExitSuccess = 0x2001,
		StackOverflow = 0x3FFE,
		DebuggerBreakpoint = 0x3FFF,
	}

	public enum SpuPipeline
	{
		None,
		Odd = 1,
		Even = 2,
	}

	/// <summary>
	/// SPU instruction definitions as taken from the 
	/// "Synergistic Processor Unit Instruction Set Architecture" version 1.2
	/// </summary>
	[DebuggerDisplay("{Name}")]
	public class SpuOpCode
	{
		private readonly string _name;
		private readonly int _opCode;

		private readonly SpuInstructionFormat _format;

		private readonly int _immediateBits;

		private readonly SpuOpCodeSpecialFeatures _specialFeatures;

		private readonly bool _registerRtNotWritten;
		private readonly bool _registerRtRead;

		private readonly int _opCodeWidth;

		private readonly string _title;

		private readonly SpuInstructionPart _parts;

		private readonly SpuPipeline _pipeline;
		private readonly int _latency;

		static Dictionary<SpuOpCodeEnum, SpuOpCode> s_enumCodeMap;

		static object s_lock = new object();

		private SpuOpCode(string name, string title, SpuInstructionFormat format, String opcode, 
			SpuPipeline pipeline, int latency)
			: this(name, title, format, opcode, SpuOpCodeSpecialFeatures.None, pipeline, latency) { }

		private SpuOpCode(string name, string title, SpuInstructionFormat format, String opcode, SpuOpCodeSpecialFeatures features,
			SpuPipeline pipeline, int latency)
		{
			_name = name;
			_title = title;
			_format = format;
			_opCodeWidth = opcode.Length;
			_opCode = Convert.ToInt32(opcode, 2) << (32 - _opCodeWidth);
			_pipeline = pipeline;
			_latency = latency;

			_registerRtNotWritten = (features & SpuOpCodeSpecialFeatures.RegisterRtNotWritten) != 0;
			_registerRtRead = (features & SpuOpCodeSpecialFeatures.RegisterRtRead) != 0;

			_specialFeatures = features;

			switch (format)
			{
				case SpuInstructionFormat.None:
					break;
				case SpuInstructionFormat.RR:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Rb;
					break;
				case SpuInstructionFormat.RR2:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra;
					break;
				case SpuInstructionFormat.RR1:
					_parts = SpuInstructionPart.Ra;
					break;
				case SpuInstructionFormat.Rrr:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Rb | SpuInstructionPart.Rc;
					break;
				case SpuInstructionFormat.RI7:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate;
					_immediateBits = 7;
					break;
				case SpuInstructionFormat.RI8:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate;
					_immediateBits = 8;
					break;
				case SpuInstructionFormat.RI10:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ra | SpuInstructionPart.Immediate;
					_immediateBits = 10;
					break;
				case SpuInstructionFormat.RI14:
					_parts = SpuInstructionPart.Immediate;
					_immediateBits = 14;
					break;
				case SpuInstructionFormat.RI16:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Immediate;
					_immediateBits = 16;
					break;
				case SpuInstructionFormat.RI16NoRegs:
					_parts = SpuInstructionPart.Immediate;
					_immediateBits = 16;
					break;
				case SpuInstructionFormat.RI18:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Immediate;
					_immediateBits = 18;
					break;
				case SpuInstructionFormat.Channel:
					_parts = SpuInstructionPart.Rt | SpuInstructionPart.Ca;
					_immediateBits = 7;
					break;
				case SpuInstructionFormat.Weird:
				case SpuInstructionFormat.Custom:
					break;
				default:
					throw new ArgumentException();
			}
		}

		internal static SpuOpCode GetOpCode(SpuOpCodeEnum code)
		{
			lock (s_lock)
			{
				if (s_enumCodeMap != null)
					return s_enumCodeMap[code];

				Dictionary<string, SpuOpCode> opcodenames = new Dictionary<string, SpuOpCode>(StringComparer.OrdinalIgnoreCase);
				foreach (SpuOpCode opcode in GetSpuOpCodes())
					opcodenames.Add(opcode.Name, opcode);


				s_enumCodeMap = new Dictionary<SpuOpCodeEnum, SpuOpCode>();
				FieldInfo[] fields = typeof (SpuOpCodeEnum).GetFields(BindingFlags.Static | BindingFlags.Public);
				foreach (FieldInfo fieldInfo in fields)
				{
					SpuOpCode oc;
					SpuOpCodeEnum val = (SpuOpCodeEnum) fieldInfo.GetValue(null);
					if (val == SpuOpCodeEnum.None)
						continue;

					bool found = opcodenames.TryGetValue(fieldInfo.Name, out oc);
					Utilities.Assert(found, "Enum names must match opcode names.");

					s_enumCodeMap.Add(val, oc);
				}
			}

			return s_enumCodeMap[code];
		}

		/// <summary>
		/// Returns the SPU opcodes that are defined and checks that their field names are the same as the name that is given to the constructor.
		/// <para>
		/// IMPORTANT: There is generated code that depend on this method, 
		/// so if you change it be sure generate new code with <see cref="CodeGenUtils"/>.
		/// </para>
		/// </summary>
		/// <returns>Pairs of opcodes and their <see cref="SpuOpCodeEnum"/> values.</returns>
		internal static List<SpuOpCode> GetSpuOpCodes()
		{
			FieldInfo[] fields = typeof (SpuOpCode).GetFields(BindingFlags.Static | BindingFlags.Public);

			List<SpuOpCode> opcodes = new List<SpuOpCode>();

			foreach (FieldInfo field in fields)
			{
				if (field.FieldType != typeof(SpuOpCode))
					continue;

				SpuOpCode oc = (SpuOpCode) field.GetValue(null);

				Utilities.Assert(oc.Name == field.Name,
				                 string.Format("Name of opcode field {0} is not the same as the opcode name ({1}).", field.Name,
				                               oc.Name));
				opcodes.Add(oc);
			}

			return opcodes;
		}

		public string Name
		{
			get { return _name; }
		}

		public string Title
		{
			get { return _title; }
		}

		/// <summary>
		/// The instruction format indicates the bit layout of instructions using the opcode.
		/// </summary>
		public SpuInstructionFormat Format
		{
			get { return _format; }
		}

		internal SpuInstructionPart Parts
		{
			get { return _parts; }
		}

		public int OpCodeWidth
		{
			get { return _opCodeWidth; }
		}

		public int OpCode
		{
			get { return _opCode; }
		}

		/// <summary>
		/// Some instructions (store) have a common layout, but the rt register is not written to.
		/// For those instructions, this property returns true.
		/// </summary>
		public bool RegisterRtNotWritten
		{
			get { return _registerRtNotWritten; }
		}

		public bool RegisterRtRead
		{
			get { return _registerRtRead; }
		}

		/// <summary>
		/// Indicates if the opcode has bits for immediate data. 
		/// ROH and ROL are not indicated by this property.
		/// </summary>
		public bool HasImmediate
		{
			get { return _immediateBits != 0; }
		}

		public SpuOpCodeSpecialFeatures SpecialFeatures
		{
			get { return _specialFeatures; }
		}

		public SpuPipeline Pipeline
		{
			get { return _pipeline; }
		}

		public int Latency
		{
			get { return _latency; }
		}

		/// <summary>
		/// TODO: Add the rest of the instructions: hint-for-branch, control instructions, channel instructions.
		/// </summary>
		//		public static SpuOpCode[] OpCodes = new SpuOpCode[]
		//			{
		public static readonly SpuOpCode lqd =
			new SpuOpCode("lqd", "Load Quadword (d-form)", SpuInstructionFormat.RI10, "00110100", SpuOpCodeSpecialFeatures.MemoryRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode lqx =
			new SpuOpCode("lqx", "Load Quadword (x-form)", SpuInstructionFormat.RR, "00111000100", SpuOpCodeSpecialFeatures.MemoryRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode lqa =
			new SpuOpCode("lqa", "Load Quadword (a-form)", SpuInstructionFormat.RI16, "001100001", SpuOpCodeSpecialFeatures.MemoryRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode lqr =
			new SpuOpCode("lqr", "Load Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16, "001100111", SpuOpCodeSpecialFeatures.MemoryRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode stqd =
			new SpuOpCode("stqd", "Store Quadword (d-form)", SpuInstructionFormat.RI10, "00100100", SpuOpCodeSpecialFeatures.MemoryWrite | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode stqx =
			new SpuOpCode("stqx", "Store Quadword (x-form)", SpuInstructionFormat.RR, "00101000100", SpuOpCodeSpecialFeatures.MemoryWrite | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode stqa =
			new SpuOpCode("stqa", "Store Quadword (a-form)", SpuInstructionFormat.RI16, "001000001", SpuOpCodeSpecialFeatures.MemoryWrite | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode stqr =
			new SpuOpCode("stqr", "Store Quadword Instruction Relative (a-form)", SpuInstructionFormat.RI16, "001000111", SpuOpCodeSpecialFeatures.MemoryWrite | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode cbd =
			new SpuOpCode("cbd", "Generate Controls for Byte Insertion (d-form)", SpuInstructionFormat.RI7, "00111110100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode cbx =
			new SpuOpCode("cbx", "Generate Controls for Byte Insertion (x-form)", SpuInstructionFormat.RR, "00111010100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode chd =
			new SpuOpCode("chd", "Generate Controls for Halfword Insertion (d-form)", SpuInstructionFormat.RI7, "00111110101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode chx =
			new SpuOpCode("chx", "Generate Controls for Halfword Insertion (x-form)", SpuInstructionFormat.RR, "00111010101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode cwd =
				new SpuOpCode("cwd", "Generate Controls for Word Insertion (d-form)", SpuInstructionFormat.RI7, "00111110110", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode cwx =
				new SpuOpCode("cwx", "Generate Controls for Word Insertion (x-form)", SpuInstructionFormat.RR, "00111010110", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode cdd =
				new SpuOpCode("cdd", "Generate Controls for Doubleword Insertion (d-form)", SpuInstructionFormat.RI7, "00111110111", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode cdx =
				new SpuOpCode("cdx", "Generate Controls for Doubleword Insertion (x-form)", SpuInstructionFormat.RR, "00111010111", SpuPipeline.Odd, 4);

		// Constant form section.
		public static readonly SpuOpCode ilh =
				new SpuOpCode("ilh", "Immediate Load Halfword", SpuInstructionFormat.RI16, "010000011", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ilhu =
				new SpuOpCode("ilhu", "Immediate Load Halfword Upper", SpuInstructionFormat.RI16, "010000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode il =
				new SpuOpCode("il", "Immediate Load Word", SpuInstructionFormat.RI16, "010000001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ila =
				new SpuOpCode("ila", "Immediate Load Address", SpuInstructionFormat.RI18, "0100001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode iohl =
				new SpuOpCode("iohl", "Immediate Or Halfword Lower", SpuInstructionFormat.RI16, "011000001", SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Even, 2);
		public static readonly SpuOpCode fsmbi =
				new SpuOpCode("fsmbi", "Form Select Mask for Bytes Immediate", SpuInstructionFormat.RI16, "001100101", SpuPipeline.Odd, 4);

		// 5. Integer and Logical OpCodes
		public static readonly SpuOpCode ah =
				new SpuOpCode("ah", "Add Halfword", SpuInstructionFormat.RR, "00011001000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ahi =
				new SpuOpCode("ahi", "Add Halfword Immediate", SpuInstructionFormat.RI10, "00011101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode a =
				new SpuOpCode("a", "Add Word", SpuInstructionFormat.RR, "00011000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ai =
				new SpuOpCode("ai", "Add Word Immediate", SpuInstructionFormat.RI10, "00011100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode sfh =
				new SpuOpCode("sfh", "Subtract from Halfword", SpuInstructionFormat.RR, "00001001000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode sfhi =
				new SpuOpCode("sfhi", "Subtract from Halfword Immediate", SpuInstructionFormat.RI10, "00001101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode sf =
				new SpuOpCode("sf", "Subtract from Word", SpuInstructionFormat.RR, "00001000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode sfi =
				new SpuOpCode("sfi", "Subtract from Word Immediate", SpuInstructionFormat.RI10, "00001100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode addx =
				new SpuOpCode("addx", "Add Extended", SpuInstructionFormat.RR, "01101000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cg =
				new SpuOpCode("cg", "Carry Generate", SpuInstructionFormat.RR, "00011000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgx =
				new SpuOpCode("cgx", "Carry Generate Extended", SpuInstructionFormat.RR, "01101000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode sfx =
				new SpuOpCode("sfx", "Subtract from Extended", SpuInstructionFormat.RR, "01101000001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode bg =
				new SpuOpCode("bg", "Borrow Generate", SpuInstructionFormat.RR, "00001000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode bgx =
				new SpuOpCode("bgx", "Borrow Generate Extended", SpuInstructionFormat.RR, "01101000011", SpuPipeline.Even, 2);
		public static readonly SpuOpCode mpy =
				new SpuOpCode("mpy", "Multiply", SpuInstructionFormat.RR, "01111000100", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyu =
				new SpuOpCode("mpyu", "Multiply Unsigned", SpuInstructionFormat.RR, "01111001100", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyi =
				new SpuOpCode("mpyi", "Multiply Immediate", SpuInstructionFormat.RI10, "01110100", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyui =
				new SpuOpCode("mpyui", "Multiply Unsigned Immediate", SpuInstructionFormat.RI10, "01110101", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpya =
				new SpuOpCode("mpya", "Multiply and Add", SpuInstructionFormat.Rrr, "1100", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyh =
				new SpuOpCode("mpyh", "Multiply High", SpuInstructionFormat.RR, "01111000101", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpys =
				new SpuOpCode("mpys", "Multiply and Shift Right", SpuInstructionFormat.RR, "01111000111", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyhh =
				new SpuOpCode("mpyhh", "Multiply High High", SpuInstructionFormat.RR, "01111000110", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyhha =
				new SpuOpCode("mpyhha", "Multiply High High and Add", SpuInstructionFormat.RR, "01101000110", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyhhu =
				new SpuOpCode("mpyhhu", "Multiply High High Unsigned", SpuInstructionFormat.RR, "01111001110", SpuPipeline.Even, 7);
		public static readonly SpuOpCode mpyhhau =
				new SpuOpCode("mpyhhau", "Multiply High High Unsigned and Add", SpuInstructionFormat.RR, "01101001110", SpuPipeline.Even, 7);
		// p83 clz
		public static readonly SpuOpCode clz =
				new SpuOpCode("clz", "Count Leading Zeros", SpuInstructionFormat.RR2, "01010100101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cntb =
				new SpuOpCode("cntb", "Count Ones in Bytes", SpuInstructionFormat.RR2, "01010110100", SpuPipeline.Even, 4);
		public static readonly SpuOpCode fsmb =
				new SpuOpCode("fsmb", "Form Select Mask for Bytes", SpuInstructionFormat.RR2, "00110110110", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode fsmh =
				new SpuOpCode("fsmh", "Form Select Mask for Halfwords", SpuInstructionFormat.RR2, "00110110101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode fsm =
				new SpuOpCode("fsm", "Form Select Mask for Words", SpuInstructionFormat.RR2, "00110110100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode gbb =
				new SpuOpCode("gbb", "Gather Bits from Bytes", SpuInstructionFormat.RR2, "00110110010", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode gbh =
				new SpuOpCode("gbh", "Gather Bits from Halfwords", SpuInstructionFormat.RR2, "00110110001", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode gb =
				new SpuOpCode("gb", "Gather Bits from Words", SpuInstructionFormat.RR2, "00110110000", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode avgb =
				new SpuOpCode("avgb", "Average Bytes", SpuInstructionFormat.RR, "00011010011", SpuPipeline.Even, 4);
		public static readonly SpuOpCode absdb =
				new SpuOpCode("absdb", "Absolute Differences of Bytes", SpuInstructionFormat.RR, "00001010011", SpuPipeline.Even, 4);
		public static readonly SpuOpCode sumb =
				new SpuOpCode("sumb", "Sum Bytes into Halfwords", SpuInstructionFormat.RR, "01001010011", SpuPipeline.Even, 4);
		public static readonly SpuOpCode xsbh =
				new SpuOpCode("xsbh", "Extend Sign Byte to Halfword", SpuInstructionFormat.RR2, "01010110110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode xshw =
				new SpuOpCode("xshw", "Extend Sign Halfword to Word", SpuInstructionFormat.RR2, "01010101110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode xswd =
				new SpuOpCode("xswd", "Extend Sign Word to Doubleword", SpuInstructionFormat.RR2, "01010100110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode and =
				new SpuOpCode("and", "And", SpuInstructionFormat.RR, "00011000001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode andc =
				new SpuOpCode("andc", "And with Complement", SpuInstructionFormat.RR, "01011000001", SpuPipeline.Even, 2);

		public static readonly SpuOpCode andbi =
				new SpuOpCode("andbi", "And Byte Immediate", SpuInstructionFormat.RI10, "00010110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode andhi =
				new SpuOpCode("andhi", "And Halfword Immediate", SpuInstructionFormat.RI10, "00010101" ,SpuPipeline.Even, 2);
		public static readonly SpuOpCode andi =
				new SpuOpCode("andi", "And Word Immediate", SpuInstructionFormat.RI10, "00010100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode or =
				new SpuOpCode("or", "Or", SpuInstructionFormat.RR, "00001000001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode orc =
				new SpuOpCode("orc", "Or with Complement", SpuInstructionFormat.RR, "01011001001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode orbi =
				new SpuOpCode("orbi", "Or Byte Immediate", SpuInstructionFormat.RI10, "00000110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode orhi =
				new SpuOpCode("orhi", "Or Halfword Immediate", SpuInstructionFormat.RI10, "00000101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ori =
				new SpuOpCode("ori", "Or Word Immediate", SpuInstructionFormat.RI10, "00000100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode orx =
				new SpuOpCode("orx", "Or Across", SpuInstructionFormat.RR2, "00111110000", SpuPipeline.Even, 4);
		public static readonly SpuOpCode xor =
				new SpuOpCode("xor", "Exclusive Or", SpuInstructionFormat.RR, "01001000001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode xorbi =
				new SpuOpCode("xorbi", "Exclusive Or Byte Immediate", SpuInstructionFormat.RI10, "01000110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode xorhi =
				new SpuOpCode("xorhi", "Exclusive Or Halfword Immediate", SpuInstructionFormat.RI10, "01000101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode xori =
				new SpuOpCode("xori", "Exclusive Or Word Immediate", SpuInstructionFormat.RI10, "01000100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode nand =
				new SpuOpCode("nand", "Nand", SpuInstructionFormat.RR, "00011001001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode nor =
				new SpuOpCode("nor", "Nor", SpuInstructionFormat.RR, "00001001001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode eqv =
				new SpuOpCode("eqv", "Equivalent", SpuInstructionFormat.RR, "01001001001", SpuPipeline.Even, 2);
		public static readonly SpuOpCode selb =
				new SpuOpCode("selb", "Select Bits", SpuInstructionFormat.Rrr, "1000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode shufb =
				new SpuOpCode("shufb", "Shuffle Bytes", SpuInstructionFormat.Rrr, "1011", SpuPipeline.Odd, 4);

		// 6. Shift and Rotate OpCodes
		public static readonly SpuOpCode shlh =
				new SpuOpCode("shlh", "Shift Left Halfword", SpuInstructionFormat.RR, "00001011111", SpuPipeline.Even, 4);
		public static readonly SpuOpCode shlhi =
				new SpuOpCode("shlhi", "Shift Left Halfword Immediate", SpuInstructionFormat.RI7, "00001111111", SpuPipeline.Even, 4);
		public static readonly SpuOpCode shl =
				new SpuOpCode("shl", "Shift Left Word", SpuInstructionFormat.RR, "00001011011", SpuPipeline.Even, 4);
		public static readonly SpuOpCode shli =
				new SpuOpCode("shli", "Shift Left Word Immediate", SpuInstructionFormat.RI7, "00001111011", SpuPipeline.Even, 4);
		public static readonly SpuOpCode shlqbi =
				new SpuOpCode("shlqbi", "Shift Left Quadword by Bits", SpuInstructionFormat.RR, "00111011011", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode shlqbii =
				new SpuOpCode("shlqbii", "Shift Left Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111011", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode shlqby =
				new SpuOpCode("shlqby", "Shift Left Quadword by Bytes", SpuInstructionFormat.RR, "00111011111", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode shlqbyi =
				new SpuOpCode("shlqbyi", "Shift Left Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111111", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode shlqbybi =
				new SpuOpCode("shlqbybi", "Shift Left Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001111", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode roth =
				new SpuOpCode("roth", "Rotate Halfword", SpuInstructionFormat.RR, "00001011100", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rothi =
				new SpuOpCode("rothi", "Rotate Halfword Immediate", SpuInstructionFormat.RI7, "00001111100", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rot =
				new SpuOpCode("rot", "Rotate Word", SpuInstructionFormat.RR, "00001011000", SpuPipeline.Even, 4);
		public static readonly SpuOpCode roti =
				new SpuOpCode("roti", "Rotate Word Immediate", SpuInstructionFormat.RI7, "00001111000", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotqby =
				new SpuOpCode("rotqby", "Rotate Quadword by Bytes", SpuInstructionFormat.RR, "00111011100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqbyi =
				new SpuOpCode("rotqbyi", "Rotate Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqbybi =
				new SpuOpCode("rotqbybi", "Rotate Quadword by Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqbi =
				new SpuOpCode("rotqbi", "Rotate Quadword by Bits", SpuInstructionFormat.RR, "00111011000", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqbii =
				new SpuOpCode("rotqbii", "Rotate Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111000", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rothm =
				new SpuOpCode("rothm", "Rotate and Mask Halfword", SpuInstructionFormat.RR, "00001011101", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rothmi =
				new SpuOpCode("rothmi", "Rotate and Mask Halfword Immediate", SpuInstructionFormat.RI7, "00001111101", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotm =
				new SpuOpCode("rotm", "Rotate and Mask Word", SpuInstructionFormat.RR, "00001011001", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotmi =
				new SpuOpCode("rotmi", "Rotate and Mask Word Immediate", SpuInstructionFormat.RI7, "00001111001", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotqmby =
				new SpuOpCode("rotqmby", "Rotate and Mask Quadword by Bytes", SpuInstructionFormat.RR, "00111011101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqmbyi =
				new SpuOpCode("rotqmbyi", "Rotate and Mask Quadword by Bytes Immediate", SpuInstructionFormat.RI7, "00111111101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqmbybi =
				new SpuOpCode("rotqmbybi", "Rotate and Mask Quadword Bytes from Bit Shift Count", SpuInstructionFormat.RR, "00111001101", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqmbi =
				new SpuOpCode("rotqmbi", "Rotate and Mask Quadword by Bits", SpuInstructionFormat.RR, "00111011001", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotqmbii =
				new SpuOpCode("rotqmbii", "Rotate and Mask Quadword by Bits Immediate", SpuInstructionFormat.RI7, "00111111001", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode rotmah =
				new SpuOpCode("rotmah", "Rotate and Mask Algebraic Halfword", SpuInstructionFormat.RR, "00001011110", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotmahi =
				new SpuOpCode("rotmahi", "Rotate and Mask Algebraic Halfword Immediate", SpuInstructionFormat.RI7, "00001111110", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotma =
				new SpuOpCode("rotma", "Rotate and Mask Algebraic Word", SpuInstructionFormat.RR, "00001011010", SpuPipeline.Even, 4);
		public static readonly SpuOpCode rotmai =
				new SpuOpCode("rotmai", "Rotate and Mask Algebraic Word Immediate", SpuInstructionFormat.RI7, "00001111010", SpuPipeline.Even, 4);

		// 7. Compare, Branch, and Halt OpCodes
		public static readonly SpuOpCode heq =
				new SpuOpCode("heq", "Halt If Equal", SpuInstructionFormat.RR, "01111011000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode heqi =
				new SpuOpCode("heqi", "Halt If Equal Immediate", SpuInstructionFormat.RI10, "01111111", SpuPipeline.Even, 2);
		public static readonly SpuOpCode hgt =
				new SpuOpCode("hgt", "Halt If Greater Than", SpuInstructionFormat.RR, "01001011000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode hgti =
				new SpuOpCode("hgti", "Halt If Greater Than Immediate", SpuInstructionFormat.RI10, "01001111", SpuPipeline.Even, 2);
		public static readonly SpuOpCode hlgt =
				new SpuOpCode("hlgt", "Halt If Logically Greater Than", SpuInstructionFormat.RR, "01011011000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode hlgti =
				new SpuOpCode("hlgti", "Halt If Logically Greater Than Immediate", SpuInstructionFormat.RI10, "01011111", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceqb =
				new SpuOpCode("ceqb", "Compare Equal Byte", SpuInstructionFormat.RR, "01111010000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceqbi =
				new SpuOpCode("ceqbi", "Compare Equal Byte Immediate", SpuInstructionFormat.RI10, "01111110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceqh =
				new SpuOpCode("ceqh", "Compare Equal Halfword", SpuInstructionFormat.RR, "01111001000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceqhi =
				new SpuOpCode("ceqhi", "Compare Equal Halfword Immediate", SpuInstructionFormat.RI10, "01111101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceq =
				new SpuOpCode("ceq", "Compare Equal Word", SpuInstructionFormat.RR, "01111000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode ceqi =
				new SpuOpCode("ceqi", "Compare Equal Word Immediate", SpuInstructionFormat.RI10, "01111100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgtb =
				new SpuOpCode("cgtb", "Compare Greater Than Byte", SpuInstructionFormat.RR, "01001010000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgtbi =
				new SpuOpCode("cgtbi", "Compare Greater Than Byte Immediate", SpuInstructionFormat.RI10, "01001110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgth =
				new SpuOpCode("cgth", "Compare Greater Than Halfword", SpuInstructionFormat.RR, "01001001000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgthi =
				new SpuOpCode("cgthi", "Compare Greater Than Halfword Immediate", SpuInstructionFormat.RI10, "01001101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgt =
				new SpuOpCode("cgt", "Compare Greater Than Word", SpuInstructionFormat.RR, "01001000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode cgti =
				new SpuOpCode("cgti", "Compare Greater Than Word Immediate", SpuInstructionFormat.RI10, "01001100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgtb =
				new SpuOpCode("clgtb", "Compare Logical Greater Than Byte", SpuInstructionFormat.RR, "01011010000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgtbi =
				new SpuOpCode("clgtbi", "Compare Logical Greater Than Byte Immediate", SpuInstructionFormat.RI10, "01011110", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgth =
				new SpuOpCode("clgth", "Compare Logical Greater Than Halfword", SpuInstructionFormat.RR, "01011001000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgthi =
				new SpuOpCode("clgthi", "Compare Logical Greater Than Halfword Immediate", SpuInstructionFormat.RI10, "01011101", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgt =
				new SpuOpCode("clgt", "Compare Logical Greater Than Word", SpuInstructionFormat.RR, "01011000000", SpuPipeline.Even, 2);
		public static readonly SpuOpCode clgti =
				new SpuOpCode("clgti", "Compare Logical Greater Than Word Immediate", SpuInstructionFormat.RI10, "01011100", SpuPipeline.Even, 2);
		public static readonly SpuOpCode br =
				new SpuOpCode("br", "Branch Relative", SpuInstructionFormat.RI16NoRegs, "001100100", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode bra =
				new SpuOpCode("bra", "Branch Absolute", SpuInstructionFormat.RI16NoRegs, "001100000", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brsl =
				new SpuOpCode("brsl", "Branch Relative and Set Link", SpuInstructionFormat.RI16, "001100110", SpuOpCodeSpecialFeatures.MethodCall, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brasl =
				new SpuOpCode("brasl", "Branch Absolute and Set Link", SpuInstructionFormat.RI16, "001100010", SpuOpCodeSpecialFeatures.MethodCall, SpuPipeline.Odd, 4);
		// p175
		public static readonly SpuOpCode bi =
				new SpuOpCode("bi", "Branch Indirect", SpuInstructionFormat.RR1, "00110101000", SpuOpCodeSpecialFeatures.BitDE, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode iret =
				new SpuOpCode("iret", "Interrupt Return", SpuInstructionFormat.RR1, "00110101010", SpuOpCodeSpecialFeatures.BitDE, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode bisled =
				new SpuOpCode("bisled", "Branch Indirect and Set Link if External Data", SpuInstructionFormat.RR2, "00110101011", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.MethodCall, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode bisl =
				new SpuOpCode("bisl", "Branch Indirect and Set Link", SpuInstructionFormat.RR2, "00110101001", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.MethodCall, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brnz =
				new SpuOpCode("brnz", "Branch If Not Zero Word", SpuInstructionFormat.RI16, "001000010", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brz =
				new SpuOpCode("brz", "Branch If Zero Word", SpuInstructionFormat.RI16, "001000000", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brhnz =
				new SpuOpCode("brhnz", "Branch If Not Zero Halfword", SpuInstructionFormat.RI16, "001000110", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode brhz =
				new SpuOpCode("brhz", "Branch If Zero Halfword", SpuInstructionFormat.RI16, "001000100", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode biz =
				new SpuOpCode("biz", "Branch Indirect If Zero", SpuInstructionFormat.RR2, "00100101000", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode binz =
				new SpuOpCode("binz", "Branch Indirect If Not Zero", SpuInstructionFormat.RR2, "00100101001", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode bihz =
				new SpuOpCode("bihz", "Branch Indirect If Zero Halfword", SpuInstructionFormat.RR2, "0100101010", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		public static readonly SpuOpCode bihnz =
				new SpuOpCode("bihnz", "Branch Indirect If Not Zero Halfword", SpuInstructionFormat.RR2, "00100101011", SpuOpCodeSpecialFeatures.BitDE | SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);
		// 8. Hint-for-Branch OpCodes: Unusual instruction format, so currently omitted.

		// 9. Floating point.
		public static readonly SpuOpCode fa =
				new SpuOpCode("fa", "Floating Add", SpuInstructionFormat.RR, "01011000100", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfa =
				new SpuOpCode("dfa", "Double Floating Add", SpuInstructionFormat.RR, "01011001100", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fs =
				new SpuOpCode("fs", "Floating Subtract", SpuInstructionFormat.RR, "01011000101", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfs =
				new SpuOpCode("dfs", "Double Floating Subtract", SpuInstructionFormat.RR, "01011001101", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fm =
				new SpuOpCode("fm", "Floating Multiply", SpuInstructionFormat.RR, "01011000110", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfm =
				new SpuOpCode("dfm", "Double Floating Multiply", SpuInstructionFormat.RR, "01011001110", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fma =
				new SpuOpCode("fma", "Floating Multiply and Add", SpuInstructionFormat.Rrr, "1110", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfma =
				new SpuOpCode("dfma", "Double Floating Multiply and Add", SpuInstructionFormat.RR, "01101011100", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fnms =
				new SpuOpCode("fnms", "Floating Negative Multiply and Subtract", SpuInstructionFormat.Rrr, "1101", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfnms =
				new SpuOpCode("dfnms", "Double Floating Negative Multiply and Subtract", SpuInstructionFormat.RR, "01101011110", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fms =
				new SpuOpCode("fms", "Floating Multiply and Subtract", SpuInstructionFormat.Rrr, "1111", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfms =
				new SpuOpCode("dfms", "Double Floating Multiply and Subtract", SpuInstructionFormat.RR, "01101011101", SpuPipeline.Even, 6);
		public static readonly SpuOpCode dfnma =
				new SpuOpCode("dfnma", "Double Floating Negative Multiply and Add", SpuInstructionFormat.RR, "01101011111", SpuPipeline.Even, 13);
		public static readonly SpuOpCode frest =
				new SpuOpCode("frest", "Floating Reciprocal Estimate", SpuInstructionFormat.RR2, "00110111000", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode frsqest =
				new SpuOpCode("frsqest", "Floating Reciprocal Absolute Square Root Estimate", SpuInstructionFormat.RR2, "00110111001", SpuPipeline.Odd, 4);
		public static readonly SpuOpCode fi =
				new SpuOpCode("fi", "Floating Interpolate", SpuInstructionFormat.RR, "01111010100", SpuPipeline.Even, 7);
		// p220
		public static readonly SpuOpCode csflt =
				new SpuOpCode("csflt", "Convert Signed Integer to Floating", SpuInstructionFormat.RI8, "0111011010", SpuPipeline.Even, 7);
		public static readonly SpuOpCode cflts =
				new SpuOpCode("cflts", "Convert Floating to Signed Integer", SpuInstructionFormat.RI8, "0111011000", SpuPipeline.Even, 7);
		public static readonly SpuOpCode cuflt =
				new SpuOpCode("cuflt", "Convert Unsigned Integer to Floating", SpuInstructionFormat.RI8, "0111011011", SpuPipeline.Even, 7);
		public static readonly SpuOpCode cfltu =
				new SpuOpCode("cfltu", "Convert Floating to Unsigned Integer", SpuInstructionFormat.RI8, "0111011001", SpuPipeline.Even, 7);
		public static readonly SpuOpCode frds =
				new SpuOpCode("frds", "Floating Round Double to Single", SpuInstructionFormat.RR2, "01110111001", SpuPipeline.Even, 13);
		public static readonly SpuOpCode fesd =
				new SpuOpCode("fesd", "Floating Extend Single to Double", SpuInstructionFormat.RR2, "01110111000", SpuPipeline.Even, 13);

		// Can't find latency info for these.
		public static readonly SpuOpCode dfceq =
				new SpuOpCode("dfceq", "Double Floating Compare Equal", SpuInstructionFormat.RR, "01111000011", SpuPipeline.None, 0);
		public static readonly SpuOpCode dfcmeq =
				new SpuOpCode("dfcmeq", "Double Floating Compare Magnitude Equal", SpuInstructionFormat.RR, "01111001011", SpuPipeline.None, 0);
		public static readonly SpuOpCode dfcgt =
				new SpuOpCode("dfcgt", "Double Floating Compare Greater Than", SpuInstructionFormat.RR, "01011000011", SpuPipeline.None, 0);
		public static readonly SpuOpCode dfcmgt =
				new SpuOpCode("dfcmgt", "Double Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR, "01011001011", SpuPipeline.None, 0);
		public static readonly SpuOpCode dftsv =
				new SpuOpCode("dftsv", "Double Floating Test Special Value", SpuInstructionFormat.RI7, "01110111111", SpuPipeline.None, 0);

		public static readonly SpuOpCode fceq =
				new SpuOpCode("fceq", "Floating Compare Equal", SpuInstructionFormat.RR, "01111000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode fcmeq =
				new SpuOpCode("fcmeq", "Floating Compare Magnitude Equal", SpuInstructionFormat.RR, "01111001010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode fcgt =
				new SpuOpCode("fcgt", "Floating Compare Greater Than", SpuInstructionFormat.RR, "01011000010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode fcmgt =
				new SpuOpCode("fcmgt", "Floating Compare Magnitude Greater Than", SpuInstructionFormat.RR, "01011001010", SpuPipeline.Even, 2);
		public static readonly SpuOpCode fscrwr =
				new SpuOpCode("fscrwr", "Floating-Point Status and Control Register Write", SpuInstructionFormat.RR2, "01110111010", SpuPipeline.Even, 7);
		public static readonly SpuOpCode fscrrd =
				new SpuOpCode("fscrrd", "Floating-Point Status and Control Register Read", SpuInstructionFormat.RR1, "01110011000", SpuPipeline.Even, 13);
		// 10. Control OpCodes
		// p238
		//			};
		public static readonly SpuOpCode stop =
				new SpuOpCode("stop", "Stop and Signal", SpuInstructionFormat.RI14, "00000000000", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 4);

		public static readonly SpuOpCode lnop =
			new SpuOpCode("lnop", "No Operation (Load)", SpuInstructionFormat.Weird, "00000000001", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Odd, 0);

		public static readonly SpuOpCode nop =
			new SpuOpCode("nop", "No Operation (Execute)", SpuInstructionFormat.Weird, "01000000001", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead, SpuPipeline.Even, 0);

		public static readonly SpuOpCode rdch =
			new SpuOpCode("rdch", "Read Channel", SpuInstructionFormat.Channel, "00000001101", SpuOpCodeSpecialFeatures.ChannelAccess, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode rchcnt =
			new SpuOpCode("rchcnt", "Read Channel Count", SpuInstructionFormat.Channel, "00000001111", SpuOpCodeSpecialFeatures.ChannelAccess, SpuPipeline.Odd, 6);
		public static readonly SpuOpCode wrch =
			new SpuOpCode("wrch", "Write Channel", SpuInstructionFormat.Channel, "00100001101", SpuOpCodeSpecialFeatures.RegisterRtNotWritten | SpuOpCodeSpecialFeatures.RegisterRtRead | SpuOpCodeSpecialFeatures.ChannelAccess, SpuPipeline.Odd, 6);


		// *****************************************
		// Pseudo instructions.

		/// <summary>
		/// This is a pseudo-instruction.
		/// </summary>
		public static readonly SpuOpCode move = 
			new SpuOpCode("move", "Move (pseudo)", SpuInstructionFormat.Custom, "0", SpuOpCodeSpecialFeatures.Pseudo, SpuPipeline.None, 0);
		public static readonly SpuOpCode ret =
			new SpuOpCode("ret", "Function return (pseudo)", SpuInstructionFormat.Custom, "0", SpuOpCodeSpecialFeatures.Pseudo, SpuPipeline.None, 0);

	}
}
