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
using System.Collections.ObjectModel;
using System.Reflection;
using CellDotNet.Intermediate;

namespace CellDotNet.Spe
{
	abstract class SpuRoutine : ObjectWithAddress
	{
		private readonly bool hasSignature;
		private readonly StackTypeDescription _returnType;
		private readonly ReadOnlyCollection<MethodParameter> _parameters;

		protected SpuRoutine()
		{
		}

		protected SpuRoutine(string name) : this(name, null)
		{
			
		}

		protected SpuRoutine(string name, MethodInfo signature) : base(name)
		{
			if (signature != null)
			{
				hasSignature = true;
				TypeDeriver td = new TypeDeriver();
				_returnType = td.GetStackTypeDescription(signature.ReturnType);
				List<MethodParameter> plist = new List<MethodParameter>();
				foreach (ParameterInfo paraminfo in signature.GetParameters())
				{
					plist.Add(new MethodParameter(paraminfo, td.GetStackTypeDescription(paraminfo.ParameterType)));
				}
				_parameters = plist.AsReadOnly();
			}
		}

		public virtual ReadOnlyCollection<MethodParameter> Parameters
		{
			get
			{
				if (hasSignature)
					return _parameters;
				throw new InvalidOperationException();
			}
		}
		public virtual StackTypeDescription ReturnType
		{
			get
			{
				if (hasSignature)
					return _returnType;
				throw new InvalidOperationException();
			}
		}
	}
}
