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

		public override Type Type
		{
			get { return _parameterInfo.ParameterType; }
		}

//		private bool? _escapes;

//		/// <summary>
//		/// Returns a boolean depending on wheter the parameter escapes or not; that is, whether
//		/// it's address is taken. Early in the processing this is not known.
//		/// </summary>
//		public bool? Escapes
//		{
//			get { return _escapes; }
//			set { _escapes = value; }
//		}

		public MethodParameter(ParameterInfo parameterInfo)
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
