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
using System.Runtime.Serialization;
using CellDotNet.Intermediate;
using CellDotNet.Spe;

namespace CellDotNet
{
	[Serializable]
	public class ILSemanticErrorException : Exception
	{
		public ILSemanticErrorException() { }
		public ILSemanticErrorException(string message) : base(message) { }
		public ILSemanticErrorException(string message, Exception inner) : base(message, inner) { }
		protected ILSemanticErrorException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class ILParseException : Exception
	{
		public ILParseException() { }
		public ILParseException(string message) : base(message) { }
		public ILParseException(string message, Exception inner) : base(message, inner) { }
		protected ILParseException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class InvalidIRTreeException : Exception
	{
		public InvalidIRTreeException() { }
		public InvalidIRTreeException(string message) : base(message) { }
		public InvalidIRTreeException(string message, Exception inner) : base(message, inner) { }
		protected InvalidIRTreeException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	class ILNotImplementedException : Exception
	{
		public ILNotImplementedException() { }
		public ILNotImplementedException(string message) : base(message) { }
		public ILNotImplementedException(string message, Exception inner) : base(message, inner) { }

		public ILNotImplementedException(TreeInstruction inst) : this(inst.Opcode.IRCode.ToString()) { }

		public ILNotImplementedException(IRCode ilcode) : this(ilcode.ToString()) { }

		protected ILNotImplementedException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class BadSpuInstructionException : Exception
	{
		public BadSpuInstructionException() { }
		internal BadSpuInstructionException(SpuInstruction inst) : base("Opcode: " + inst.OpCode.Name) { }
		public BadSpuInstructionException(string message) : base(message) { }
		public BadSpuInstructionException(string message, Exception inner) : base(message, inner) { }
		protected BadSpuInstructionException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}


	[Serializable]
	public class BadCodeLayoutException : Exception
	{
		public BadCodeLayoutException() { }
		public BadCodeLayoutException(string message) : base(message) { }
		public BadCodeLayoutException(string message, Exception inner) : base(message, inner) { }
		protected BadCodeLayoutException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class LibSpeException : Exception
	{
		public LibSpeException() : base() { }
		public LibSpeException(string message) : base(message) { }
		public LibSpeException(string message, Exception inner) : base(message, inner) { }
		protected LibSpeException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class SpeExecutionException : Exception
	{
		public SpeExecutionException() { }
		public SpeExecutionException(string message) : base(message) { }
		public SpeExecutionException(string message, Exception inner) : base(message, inner) { }
		protected SpeExecutionException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class SpeOutOfMemoryException : SpeExecutionException
	{
		public SpeOutOfMemoryException() : base("All available memory on the SPE has been allocated.") { }
		public SpeOutOfMemoryException(string message) : base(message) { }
		public SpeOutOfMemoryException(string message, Exception inner) : base(message, inner) { }
		protected SpeOutOfMemoryException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class SpeStackOverflowException : SpeExecutionException
	{
		public SpeStackOverflowException() : base("A stack overflow on the SPE has occurred.") { }
		public SpeStackOverflowException(string message) : base(message) { }
		public SpeStackOverflowException(string message, Exception inner) : base(message, inner) { }
		protected SpeStackOverflowException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class SpeDebugException : SpeExecutionException
	{
		public SpeDebugException() { }
		public SpeDebugException(string message) : base(message) { }
		public SpeDebugException(string message, Exception inner) : base(message, inner) { }
		protected SpeDebugException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class PpeCallException : Exception
	{
		public PpeCallException() { }
		public PpeCallException(string message) : base(message) { }
		public PpeCallException(string message, Exception inner) : base(message, inner) { }
		protected PpeCallException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class InvalidInstructionParametersException : Exception
	{
		public InvalidInstructionParametersException() { }
		public InvalidInstructionParametersException(string message) : base(message) { }
		public InvalidInstructionParametersException(string message, Exception inner) : base(message, inner) { }
		protected InvalidInstructionParametersException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class DebugAssertException : Exception
	{
		public DebugAssertException() { }
		public DebugAssertException(string message) : base(message) { }
		public DebugAssertException(string message, Exception inner) : base(message, inner) { }
		protected DebugAssertException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	/// <summary>
	/// Used to test stuff.
	/// </summary>
	[Serializable]
	internal class DummyException : Exception
	{
		public DummyException() { }
		public DummyException(string message) : base(message) { }
		public DummyException(string message, Exception inner) : base(message, inner) { }
		protected DummyException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class ShellExecutionException : Exception
	{
		public ShellExecutionException() { }
		public ShellExecutionException(string message) : base(message) { }
		public ShellExecutionException(string message, Exception inner) : base(message, inner) { }
		protected ShellExecutionException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class RegisterAllocationException : Exception
	{
		public RegisterAllocationException() { }
		public RegisterAllocationException(string message) : base(message) { }
		public RegisterAllocationException(string message, Exception inner) : base(message, inner) { }
		protected RegisterAllocationException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}
}
