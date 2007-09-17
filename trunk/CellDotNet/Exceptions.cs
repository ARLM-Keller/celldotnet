using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

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
		static string GetErrorMessage()
		{
			// Hmm, I guess the error code could be overwritten by this check if
			// HasSpeHardware hasn't been used before...
			if (SpeContext.HasSpeHardware)
			{
				object ec = SpeContext.GetErrorCode();
				return " code: " + (ec != null ? ec : "(null)");
			}
			else
				return " code: (no SPE hardware is available)";
		}

		public LibSpeException() : base(GetErrorMessage()) { }
		public LibSpeException(string message) : base(message + GetErrorMessage()) { }
		public LibSpeException(string message, Exception inner) : base(message + GetErrorMessage(), inner) { }
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
	public class InvalidInstructionParametersException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

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
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public DebugAssertException() { }
		public DebugAssertException(string message) : base(message) { }
		public DebugAssertException(string message, Exception inner) : base(message, inner) { }
		protected DebugAssertException(
		  SerializationInfo info,
		  StreamingContext context)
			: base(info, context) { }
	}
}
