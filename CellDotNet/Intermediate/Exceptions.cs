using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace CellDotNet.Intermediate
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
	public class InvalidIRException : Exception
	{
		public InvalidIRException() { }
		public InvalidIRException(string message) : base(message) { }
		public InvalidIRException(string message, Exception inner) : base(message, inner) { }
		protected InvalidIRException(
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

}
