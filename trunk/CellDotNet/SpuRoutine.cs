using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;

namespace CellDotNet
{
	abstract class SpuRoutine : ObjectWithAddress
	{
		protected StackTypeDescription _returnType;
		protected ReadOnlyCollection<MethodParameter> _parameters;

		protected SpuRoutine()
		{
		}

		public SpuRoutine(string name) : this(name, null)
		{
			
		}

		public SpuRoutine(string name, MethodInfo signature) : base(name)
		{
			if (signature != null)
			{
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
				if (_parameters != null)
					return _parameters;
				throw new InvalidOperationException();
			}
		}
		public virtual StackTypeDescription ReturnType
		{
			get
			{
				if (_returnType != null)
					return _returnType;
				throw new InvalidOperationException();
			}
		}
	}
}
