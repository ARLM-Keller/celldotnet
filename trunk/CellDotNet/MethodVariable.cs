using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// A local variable in a method.
	/// </summary>
	class MethodVariable
	{
		private LocalVariableInfo _locaVariableInfo;
		public LocalVariableInfo LocalVariableInfo
		{
			get { return _locaVariableInfo; }
		}

		public int Index
		{
			get { return _locaVariableInfo.LocalIndex; }
		}

		private bool? _escapes;
		public bool? Escapes
		{
			get { return _escapes; }
			set { _escapes = value; }
		}

		public string Name
		{
			get { return _locaVariableInfo.ToString(); }
		}

		public Type Type
		{
			get { return _locaVariableInfo.LocalType; }
		}


		public MethodVariable(LocalVariableInfo locaVariableInfo)
		{
			Utilities.AssertArgumentNotNull(locaVariableInfo, "locaVariableInfo");

			_locaVariableInfo = locaVariableInfo;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
