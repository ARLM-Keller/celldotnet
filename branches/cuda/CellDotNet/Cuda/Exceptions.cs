using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace CellDotNet.Cuda
{
	[Serializable]
	public class PtxCompilationException : Exception
	{
		public PtxCompilationException() { }
		public PtxCompilationException(string message) : base(message) { }
		public PtxCompilationException(string message, Exception inner) : base(message, inner) { }
		protected PtxCompilationException(
			SerializationInfo info,
			StreamingContext context) : base(info, context) { }
	}
}
