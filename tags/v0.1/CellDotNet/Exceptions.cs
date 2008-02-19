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
using CellDotNet.Spe;

namespace CellDotNet
{
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
