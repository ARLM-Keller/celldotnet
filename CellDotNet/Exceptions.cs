using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace CellDotNet
{
	[Serializable]
	public class ILSemanticErrorException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

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
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

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
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

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
}
