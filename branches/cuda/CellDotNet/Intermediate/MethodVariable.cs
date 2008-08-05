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
using CellDotNet.Spe;

namespace CellDotNet.Intermediate
{
	/// <summary>
	/// A variable in a method. Can either be a local variable or a parameter; <see cref="MethodParameter"/>
	/// inherits from this class since they have much in common: Escaping, stack position, type, virtual register.
	/// </summary>
	class MethodVariable
	{
		public StackTypeDescription StackType { get; private set; }

		public LocalVariableInfo LocalVariableInfo { get; private set; }

		/// <summary>
		/// This is true if the variable was created during IR tree construction.
		/// </summary>
		public bool IsStackVariable
		{
			get { return LocalVariableInfo == null; }
		}

		/// <summary>
		/// Variable number.
		/// </summary>
		private readonly int _index;
		public virtual int Index
		{
			get { return _index; }
		}

		/// <summary>
		/// Position of the variable on the stack, relative to the stack pointer. Measured in quadwords.
		/// </summary>
		public int StackLocation { get; set; }

		public bool? Escapes { get; set; }

		public virtual string Name
		{
			get
			{
				if (LocalVariableInfo != null)
					return LocalVariableInfo.ToString();
				else
					return "StackVar_" + Index;
			}
		}

		public virtual void SetType(StackTypeDescription stackType)
		{
			if (LocalVariableInfo != null)
				throw new InvalidOperationException("Can't change variable type.");

			StackType = stackType;
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

		private Type _reflectionType;

		public VirtualRegister VirtualRegister { get; set; }

		protected MethodVariable(StackTypeDescription stackType)
		{
			Utilities.AssertArgument(stackType != StackTypeDescription.None, "stackType != StackTypeDescription.None");
			StackType = stackType;
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
			StackType = stackType;
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
			LocalVariableInfo = localVariableInfo;
			_index = localVariableInfo.LocalIndex;
			StackType = stackType;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
