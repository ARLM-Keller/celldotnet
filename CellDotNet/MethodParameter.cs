using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// Represents a parameter to a method in <see cref="MethodCompiler"/>.
	/// </summary>
	class MethodParameter : MethodVariable
	{
		private ParameterInfo _parameterInfo;
		public ParameterInfo ParameterInfo
		{
			get { return _parameterInfo; }
		}

		public override string Name
		{
			get { return _parameterInfo.Name; }
		}

		public override int Index
		{
			get { return _parameterInfo.Position; }
		}

		public override void SetType(StackTypeDescription stackType)
		{
			throw new InvalidOperationException("Can't change parameter type.");
		}

		public override Type ReflectionType
		{
			get { return _parameterInfo.ParameterType; }
		}

		public MethodParameter(ParameterInfo parameterInfo, StackTypeDescription stackType) : base(stackType)
		{
			Utilities.AssertArgumentNotNull(parameterInfo, "parameterInfo");
			_parameterInfo = parameterInfo;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
