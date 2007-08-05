using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CellDotNet
{
	[Serializable]
	public class ILException : Exception
	{
		//
		// For guidelines regarding the creation of new exception types, see
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/cpgenref/html/cpconerrorraisinghandlingguidelines.asp
		// and
		//    http://msdn.microsoft.com/library/default.asp?url=/library/en-us/dncscol/html/csharp07192001.asp
		//

		public ILException() { }
		public ILException(string message) : base(message) { }
		public ILException(string message, Exception inner) : base(message, inner) { }
		protected ILException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}

	[Serializable]
	public class InvalidILTreeException : Exception
	{
		public InvalidILTreeException() { }
		public InvalidILTreeException(string message) : base(message) { }
		public InvalidILTreeException(string message, Exception inner) : base(message, inner) { }
		protected InvalidILTreeException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
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
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
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
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}


	[Serializable]
	public class LibSpeException : Exception
	{
		private static Type s_stdlib;

		static LibSpeException()
		{
			//  /usr/lib/mono/gac/Mono.Posix/2.0.0.0__0738eb9f132ed756/Mono.Posix.dll
			Assembly ass = Assembly.LoadFrom("/usr/lib/mono/gac/Mono.Posix/2.0.0.0__0738eb9f132ed756/Mono.Posix.dll");
			s_stdlib = ass.GetType("Mono.Unix.Native.Stdlib");
		}

		static string GetErrorMessage()
		{
			return "";
//			return " code: " + s_stdlib.GetMethod("GetLastError").Invoke(null, null);
		}

		public LibSpeException() { }
		public LibSpeException(string message) : base(message + GetErrorMessage()) { }
		public LibSpeException(string message, Exception inner) : base(message + GetErrorMessage(), inner) { }
		protected LibSpeException(
		  System.Runtime.Serialization.SerializationInfo info,
		  System.Runtime.Serialization.StreamingContext context)
			: base(info, context) { }
	}
}
