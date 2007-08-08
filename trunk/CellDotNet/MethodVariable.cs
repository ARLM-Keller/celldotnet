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
//		private StackTypeDescription _stackType;
//		public StackTypeDescription StackType
//		{
//			get { return _stackType; }
//		}

		private LocalVariableInfo _localVariableInfo;
		public LocalVariableInfo LocalVariableInfo
		{
			get { return _localVariableInfo; }
		}

		private int _index;
		public virtual int Index
		{
			get { return _index; }
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

		public virtual Type Type
		{
			get
			{
				if (_type == null)
					throw new InvalidOperationException(
						"No information is currently known about this variable. Probably it is a stack variable and type derival has not yet been performed.");
				return _type;
			}
			set
			{
				if (_type != null)
					throw new InvalidOperationException("Variable already has a type.");
				_type = value;
			}
		}


		private VirtualRegister _virtualRegister;
		private Type _type;

		public VirtualRegister VirtualRegister
		{
			get { return _virtualRegister; }
			set { _virtualRegister = value; }
		}

		protected MethodVariable() { }

		/// <summary>
		/// For stack variables.
		/// </summary>
		/// <param name="variableIndex"></param>
		/// <param name="?"></param>
		public MethodVariable(int variableIndex)
		{
			Utilities.AssertArgument(variableIndex >= 1000, "Stack varibles indices should be >= 1000.");
			_index = variableIndex;
		}

		public MethodVariable(LocalVariableInfo localVariableInfo)
		{
			Utilities.AssertArgumentNotNull(localVariableInfo, "localVariableInfo");

			_type = localVariableInfo.LocalType;
			_localVariableInfo = localVariableInfo;
			_index = localVariableInfo.LocalIndex;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
