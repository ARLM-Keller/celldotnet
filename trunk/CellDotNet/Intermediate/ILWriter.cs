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
using System.IO;
using System.Reflection.Emit;

namespace CellDotNet.Intermediate
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
			uint u = Utilities.ReinterpretAsUInt(f);
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
