using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Emit;

namespace CellDotNet
{
	/// <summary>
	/// Handy class for hand-made IL for use in unit testing.
	/// Currently it does not work with opcodes that references things such as locals,
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
			if ((opcode.Value >> 8) == 0xfe)
				_writer.Write(opcode.Value);
			else
				_writer.Write((byte)opcode.Value);
		}

		public void WriteByte(int byteValue)
		{
			_writer.Write((byte)byteValue);
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
