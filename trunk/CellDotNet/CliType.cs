using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CellDotNet
{
//	/// <summary>
//	/// The basic CLI types.
//	/// </summary>
//	/// <remarks>
//	/// </remarks>
//	enum CliStackType : byte
//	{
//		None = 0,
//		Int32 = 1,
//		Int64 = 2,
//		NativeInt = 3,
//		F = 4,
//		/// <summary>
//		/// Object reference
//		/// </summary>
//		O = 5,
//		/// <summary>
//		/// The "&amp;" type.
//		/// </summary>
//		ManagedPointer = 6
//	}

	enum CliBasicType : byte 
	{
		None,
		Integer,
		Floating,
		/// <summary>
		/// Any value type other than the builtin CLI types.
		/// </summary>
		Valuetype,
		ObjectType,
		NativeInt,
	}

	/// <summary>
	/// Sizes of numeric operands on the stack.
	/// </summary>
	enum CliNumericSize : byte
	{
		None,
		OneByte = 1,
		TwoBytes = 2,
		FourBytes = 4,
		EightBytes = 8,
	}

	/// <summary>
	/// Convenient description of a value on the stack; it can express managed pointers etc.
	/// <para>TODO: Express non-basic types.</para>
	/// </summary>
	[DebuggerDisplay("{DebuggerDisplay}")]
	struct StackTypeDescription
	{
		public static readonly StackTypeDescription None = new StackTypeDescription();
		public static readonly StackTypeDescription Int8 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.OneByte, true);
		public static readonly StackTypeDescription UInt8 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.OneByte, false);
		public static readonly StackTypeDescription Int16 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.TwoBytes, true);
		public static readonly StackTypeDescription UInt16 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.TwoBytes, false);
		public static readonly StackTypeDescription Int32 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.FourBytes, true);
		public static readonly StackTypeDescription UInt32 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.FourBytes, false);
		public static readonly StackTypeDescription Int64 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.EightBytes, true);
		public static readonly StackTypeDescription UInt64 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.EightBytes, false);
		public static readonly StackTypeDescription Float32 = new StackTypeDescription(CliBasicType.Floating, CliNumericSize.FourBytes, true);
		public static readonly StackTypeDescription Float64 = new StackTypeDescription(CliBasicType.Floating, CliNumericSize.EightBytes, true);
		public static readonly StackTypeDescription ObjectType = new StackTypeDescription(CliBasicType.ObjectType, CliNumericSize.None, false);
		public static readonly StackTypeDescription ValueType = new StackTypeDescription(CliBasicType.Valuetype, CliNumericSize.None, false);
		public static readonly StackTypeDescription NativeInt = new StackTypeDescription(CliBasicType.NativeInt, CliNumericSize.None, true);
		public static readonly StackTypeDescription NativeUInt = new StackTypeDescription(CliBasicType.NativeInt, CliNumericSize.None, false);

		public CliBasicType _cliBasicType;
		private bool _isSigned;
		private CliNumericSize _numericSize;
		private short _indirectionLevel;
		private bool _isManaged;
		public TypeDescription _complexType;

		/// <summary>
		/// For simple types.
		/// </summary>
		/// <param name="_cliBasicType"></param>
		/// <param name="_numericSize"></param>
		/// <param name="_isSigned"></param>
		public StackTypeDescription(CliBasicType _cliBasicType, CliNumericSize _numericSize, bool _isSigned)
		{
			this._cliBasicType = _cliBasicType;
			this._isSigned = _isSigned;
			this._numericSize = _numericSize;
			_indirectionLevel = 0;
			_isManaged = false;
			_complexType = null;
		}

		/// <summary>
		/// For complex types.
		/// </summary>
		/// <param name="complexType"></param>
		public StackTypeDescription(TypeDescription complexType)
		{
			_cliBasicType = complexType.Type.IsValueType ? CliBasicType.Valuetype : CliBasicType.ObjectType;
			_isSigned = false;
			_numericSize = CliNumericSize.None;
			_indirectionLevel = 0;
			_isManaged = false;
			_complexType = complexType;
		}

		public CliBasicType CliBasicType
		{
			get { return _cliBasicType; }
		}

		public TypeDescription ComplexType
		{
			get { return _complexType; }
		}


		public bool IsSigned
		{
			get { return _isSigned; }
		}

		/// <summary>
		/// You probably shouldn't use this property.
		/// </summary>
		internal CliType CliType
		{
			get
			{
				switch (_cliBasicType)
				{
					case CliBasicType.Integer:
						switch (_numericSize)
						{
							case CliNumericSize.OneByte:
								return IsSigned ? CliType.Int8 : CliType.UInt8;
							case CliNumericSize.TwoBytes:
								return IsSigned ? CliType.Int16 : CliType.UInt16;
							case CliNumericSize.FourBytes:
								return IsSigned ? CliType.Int32 : CliType.UInt32;
							case CliNumericSize.EightBytes:
								return IsSigned ? CliType.Int64 : CliType.UInt64;
							default:
								throw new Exception();
						}
					case CliBasicType.Floating:
						if (_numericSize == CliNumericSize.FourBytes)
							return CliType.Float32;
						else if (_numericSize == CliNumericSize.EightBytes)
							return CliType.Float64;
						else
							throw new Exception();
					case CliBasicType.Valuetype:
						return CliType.ValueType;
					case CliBasicType.ObjectType:
						return CliType.ObjectType;
					case CliBasicType.NativeInt:
						return IsSigned ? CliType.NativeInt : CliType.NativeUInt;
					case CliBasicType.None:
						return CliType.None;
					default:
						throw new Exception();
				}
			}
		}


		private string DebuggerDisplay
		{
			get { return "" + CliType + (IndirectionLevel > 0 ? "&" : ""); }
		}

		/// <summary>
		/// Size of a numeric value in bytes.
		/// </summary>
		public CliNumericSize NumericSize
		{
			get { return _numericSize; }
		}

		/// <summary>
		/// Both managed and unmanaged pointers.
		/// </summary>
		public int IndirectionLevel
		{
			get { return _indirectionLevel; }
		}

		public bool IsByRef
		{
			get { return IndirectionLevel > 0; }
		}

		public StackTypeDescription GetManagedPointer()
		{
			if (_isManaged)
				throw new InvalidOperationException();

			StackTypeDescription rv = this;
			rv._isManaged = true;
			rv._indirectionLevel++;
			return rv;
		}

		/// <summary>
		/// Only relevant for pointer type: Says whether the pointer is managed or unmanaged.
		/// </summary>
		public bool IsManaged
		{
			get { return _isManaged; }
		}

		public StackTypeDescription GetPointer()
		{
			if (_isManaged)
				throw new InvalidOperationException();

			StackTypeDescription rv = this;
			rv._indirectionLevel++;
			return rv;
		}

		public StackTypeDescription Dereference()
		{
			if (IndirectionLevel == 0)
				throw new InvalidOperationException("Type is not byref.");

			StackTypeDescription e = this;
			e._indirectionLevel--;
			e._isManaged = false;
			return e;
		}

		public StackTypeDescription DereferenceFully()
		{
			if (IndirectionLevel == 0)
				throw new InvalidOperationException("Type is not byref.");

			StackTypeDescription e = this;
			e._indirectionLevel = 0;
			e._isManaged = false;
			return e;
		}

		/// <summary>
		/// Returns the <see cref="System.Type"/> representation of the type, also if it is a simle type.
		/// Throws an <see cref="InvalidOperationException"/> if this type is a pointer type - that is, if <see cref="IndirectionLevel"/> > 0.
		/// </summary>
		/// <returns></returns>
		public Type GetNonPointerType()
		{
			if (IndirectionLevel > 0)
				throw new InvalidOperationException("Only valid for non-pointer types.");

			switch (this.CliType)
			{
				case CliType.None:
					return null;
				case CliType.Int8:
					return typeof (sbyte);
				case CliType.UInt8:
					return typeof(byte);
				case CliType.Int16:
					return typeof(short);
				case CliType.UInt16:
					return typeof(ushort);
				case CliType.Int32:
					return typeof(int);
				case CliType.UInt32:
					return typeof(uint);
				case CliType.Int64:
					return typeof(long);
				case CliType.UInt64:
					return typeof(ulong);
				case CliType.NativeInt:
					return typeof(IntPtr);
				case CliType.NativeUInt:
					return typeof(UIntPtr);
				case CliType.Float32:
					return typeof(sbyte);
				case CliType.Float64:
					return typeof(float);
				case CliType.ValueType:
				case CliType.ObjectType:
					return ComplexType.Type;
				case CliType.ManagedPointer:
				default:
					throw new InvalidOperationException();
			}
		}

		#region Equality stuff

		static public bool operator==(StackTypeDescription x, StackTypeDescription y)
		{
			return (x._cliBasicType == y._cliBasicType) &&
			       (x._complexType == y._complexType) &&
			       (x._indirectionLevel == y._indirectionLevel) &&
			       (x._isManaged == y._isManaged) &&
			       (x._isSigned == y._isSigned) &&
			       (x._numericSize == y._numericSize);
		}

		public static bool operator !=(StackTypeDescription x, StackTypeDescription y)
		{
			return !(x == y);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is StackTypeDescription)) return false;
			StackTypeDescription stackTypeDescription = (StackTypeDescription) obj;
			return this == stackTypeDescription;
		}

		public override int GetHashCode()
		{
			int result = _cliBasicType.GetHashCode();
			result = 29*result + _isSigned.GetHashCode();
			result = 29*result + _numericSize.GetHashCode();
			result = 29*result + _indirectionLevel;
			result = 29*result + _isManaged.GetHashCode();
			result = 29*result + (_complexType != null ? _complexType.GetHashCode() : 0);
			return result;
		}

		#endregion
	}


	/// <summary>
	/// CLI types as available in metadata, including classes like 
	/// values types, objects and method pointers.
	/// <para>
	/// Most often, you would want to use <see cref="StackTypeDescription"/> 
	/// instead of this enumeration.
	/// </para>
	/// <remarks>
	/// This enumeration contains more than the six basic CLI types: 
	/// It also contains variations of the numeric types.
	/// 
	/// <para>
	/// NOTE: It is important that within the numeric groups (int, float)
	/// the types are defined in order of increasing size. 
	/// This is used to determine expression types.
	/// </para>
	/// </remarks>
	/// </summary>
	internal enum CliType
	{
		None = 0,
		Int8,
		UInt8,
		Int16,
		UInt16,
		Int32,
		UInt32,
		Int64,
		UInt64,
		NativeInt,
		NativeUInt,
		Float32,
		Float64,
		/// <summary>
		/// Any value type.
		/// </summary>
		ValueType,
		/// <summary>
		/// Any object type.
		/// </summary>
		ObjectType,
		/// <summary>
		/// The "&amp;" type.
		/// </summary>
		ManagedPointer,
	}
}