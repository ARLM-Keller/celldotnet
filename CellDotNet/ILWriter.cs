using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Handy class for hand-made IL for use in unit testing.
	/// Currently it does not support opcodes that references things such as locals,
	/// parameters, types etc.
	/// </summary>
	class ILWriter
	{
		MemoryStream _il;
		BinaryWriter _writer;

		public ILWriter()
		{
			_il = new MemoryStream();
			_writer = new BinaryWriter(_il);
		}

		public void WriteOpcode(OpCode opcode)
		{
			if ((opcode.Value & 0xff00) == 0xfe00)
			{
				_writer.Write((byte)(opcode.Value >> 8));
				_writer.Write((byte)opcode.Value);
			}
			else
				_writer.Write((byte)opcode.Value);
		}

		public void WriteByte(int byteValue)
		{
			_writer.Write((byte)byteValue);
		}

		public void WriteInt32(int i)
		{
			_writer.Write(EncodeLittleEndian(i));
		}

		public void WriteFloat(float f1)
		{
			_writer.Write(EncodeLittleEndian((f1)));
		}

		private static byte[] EncodeLittleEndian(float f)
		{
			uint u = RecursiveInstructionSelector.ReinterpretAsUInt(f);
			return new byte[] { (byte)(u & 0xff), (byte)((u >> 8) & 0xff), (byte)((u >> 16) & 0xff), (byte)((u >> 24) & 0xff) };
		}

		private static byte[] EncodeLittleEndian(int u)
		{
			return new byte[] { (byte)(u & 0xff), (byte)((u >> 8) & 0xff), (byte)((u >> 16) & 0xff), (byte)((u >> 24) & 0xff) };
		}

		public byte[] ToByteArray()
		{
			return _il.ToArray();
		}

		public ILReader CreateReader()
		{
			return new ILReader(ToByteArray());
		}
	}
}
