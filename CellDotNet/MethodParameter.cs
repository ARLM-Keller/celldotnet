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
using System.Reflection;

namespace CellDotNet.Intermediate
{
	/// <summary>
	/// Represents a parameter to a method in <see cref="MethodCompiler"/>.
	/// </summary>
	class MethodParameter : MethodVariable
	{
		private bool _isInstanceMethod;

		private ParameterInfo _parameterInfo;

		public override string Name
		{
			get { return _parameterInfo != null ? _parameterInfo.Name : "this"; }
		}

		public override int Index
		{
			get
			{
				if (_parameterInfo == null)
					return 0;
				else if(_isInstanceMethod)
				{
					return _parameterInfo.Position + 1;
				}
				return _parameterInfo.Position;
			}
		}

		public override void SetType(StackTypeDescription stackType)
		{
			throw new InvalidOperationException("Can't change parameter type.");
		}

		public override Type ReflectionType
		{
//			get { return StackType.ComplexType.ReflectionType; }
			get
			{
				if (_parameterInfo != null)
					return _parameterInfo.ParameterType;
				else
					return StackType.ComplexType.ReflectionType;
			}
		}

		public MethodParameter(ParameterInfo parameterInfo, StackTypeDescription stackType) : base(stackType)
		{
			Utilities.AssertArgumentNotNull(parameterInfo, "parameterInfo");
			_parameterInfo = parameterInfo;
		}

		/// <summary>
		/// Instantiate a <code>MethodParameter</code> representing a this parameter to a instance method.
		/// </summary>
		/// <param name="stackType"></param>
		public MethodParameter(StackTypeDescription stackType) : base(stackType)
		{
			_isInstanceMethod = true;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
