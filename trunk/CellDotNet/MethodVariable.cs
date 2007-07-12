using System;
using System.Collections.Generic;
using System.Reflection;

namespace CellDotNet
{
	/// <summary>
	/// A variable in a method. Can either be a local variable or a parameter; <see cref="MethodParameter"/>
	/// inherits from this class since they have much in common: Escaping, stack position, type, virtual register.
	/// </summary>
	class MethodVariable
	{
		private LocalVariableInfo _locaVariableInfo;
		public LocalVariableInfo LocalVariableInfo
		{
			get { return _locaVariableInfo; }
		}

		public virtual int Index
		{
			get { return _locaVariableInfo.LocalIndex; }
		}

		private bool? _escapes;
		public bool? Escapes
		{
			get { return _escapes; }
			set { _escapes = value; }
		}

		public virtual string Name
		{
			get { return _locaVariableInfo.ToString(); }
		}

		public virtual Type Type
		{
			get { return _locaVariableInfo.LocalType; }
		}

		private VirtualRegister _virtualRegister;
		public VirtualRegister VirtualRegister
		{
			get { return _virtualRegister; }
			set { _virtualRegister = value; }
		}

		protected MethodVariable() { }

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
