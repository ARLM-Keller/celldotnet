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

namespace CellDotNet
{
	/// <summary>
	/// A variable in a method. Can either be a local variable or a parameter; <see cref="MethodParameter"/>
	/// inherits from this class since they have much in common: Escaping, stack position, type, virtual register.
	/// </summary>
	class MethodVariable
	{
		private StackTypeDescription _stackType;
		public StackTypeDescription StackType
		{
			get { return _stackType; }
		}

		private LocalVariableInfo _localVariableInfo;
		public LocalVariableInfo LocalVariableInfo
		{
			get { return _localVariableInfo; }
		}

		/// <summary>
		/// This is true if the variable was created during IR tree construction.
		/// </summary>
		public bool IsStackVariable
		{
			get { return _localVariableInfo == null; }
		}

		/// <summary>
		/// Variable number.
		/// </summary>
		private int _index;
		public virtual int Index
		{
			get { return _index; }
		}

		private int _stacklocation;
		/// <summary>
		/// Position of the variable on the stack, relative to the stack pointer. Measured in quadwords.
		/// </summary>
		public int StackLocation
		{
			get { return _stacklocation; }
			set { _stacklocation = value; }
		}

		private bool? _escapes;
		public bool? Escapes
		{
			get { return _escapes; }
			set { _escapes = value; }
		}

		public virtual string Name
		{
			get
			{
				if (_localVariableInfo != null)
					return _localVariableInfo.ToString();
				else
					return "StackVar_" + Index;
			}
		}

		public virtual void SetType(StackTypeDescription stackType)
		{
			if (_localVariableInfo != null)
				throw new InvalidOperationException("Can't change variable type.");

			_stackType = stackType;
			if (stackType.ComplexType != null)
				_reflectionType = stackType.ComplexType.ReflectionType;
		}

		/// <summary>
		/// This will currently (20070812) not be set for complex stack variables, so
		/// try to use <see cref="StackType"/> instead.
		/// </summary>
		public virtual Type ReflectionType
		{
			get
			{
				if (_reflectionType == null)
				{
					Utilities.Assert(IsStackVariable, "IsStackVariable");
					throw new InvalidOperationException("Stack variable type has not yet been determined.");
				}
				return _reflectionType;
			}
			set
			{
				if (_reflectionType != null)
					throw new InvalidOperationException("Variable already has a type.");
				_reflectionType = value;
			}
		}

		private VirtualRegister _virtualRegister;
		private Type _reflectionType;

		public VirtualRegister VirtualRegister
		{
			get { return _virtualRegister; }
			set { _virtualRegister = value; }
		}

		protected MethodVariable(StackTypeDescription stackType)
		{
			Utilities.AssertArgument(stackType != StackTypeDescription.None, "stackType != StackTypeDescription.None");
			_stackType = stackType;
		}

		/// <summary>
		/// For stack variables.
		/// </summary>
		/// <param name="variableIndex"></param>
		/// <param name="stackType"></param>
		public MethodVariable(int variableIndex, StackTypeDescription stackType)
		{
			Utilities.AssertArgument(variableIndex >= 1000, "Stack varibles indices should be >= 1000.");
			Utilities.AssertArgument(stackType != StackTypeDescription.None, "stackType != StackTypeDescription.None");
			_index = variableIndex;
			_stackType = stackType;
		}

		/// <summary>
		/// For CIL variables.
		/// </summary>
		/// <param name="localVariableInfo"></param>
		/// <param name="stackType"></param>
		public MethodVariable(LocalVariableInfo localVariableInfo, StackTypeDescription stackType)
		{
			Utilities.AssertArgumentNotNull(localVariableInfo, "localVariableInfo");
			Utilities.AssertArgument(stackType != StackTypeDescription.None, "stackType != StackTypeDescription.None");

			_reflectionType = localVariableInfo.LocalType;
			_localVariableInfo = localVariableInfo;
			_index = localVariableInfo.LocalIndex;
			_stackType = stackType;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
