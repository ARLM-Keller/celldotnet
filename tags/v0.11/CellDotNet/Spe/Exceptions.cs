using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CellDotNet.Spe
{
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
		public LibSpeException() { }
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
}
