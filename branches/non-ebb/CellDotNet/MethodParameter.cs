using System;
using System.Reflection;

namespace CellDotNet
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
