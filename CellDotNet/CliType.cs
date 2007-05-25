using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CellDotNet
{
	/// <summary>
	/// The basic CLI types.
	/// </summary>
	/// <remarks>
	/// </remarks>
	enum CliStackType : byte
	{
		None = 0,
		Int32 = 1,
		Int64 = 2,
		NativeInt = 3,
		F = 4,
		/// <summary>
		/// Object reference
		/// </summary>
		O = 5,
		/// <summary>
		/// The "&amp;" type.
		/// </summary>
		ManagedPointer = 6
	}

	/// <summary>
	/// Note that this enum does not contain a ManagedPointer value: 
	/// Managed pointers are expressed with <see cref="StackTypeDescription"/>.
	/// </summary>
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

		public StackTypeDescription(CliBasicType _cliBasicType, CliNumericSize _numericSize, bool _isSigned)
		{
			this._cliBasicType = _cliBasicType;
			this._isSigned = _isSigned;
			this._numericSize = _numericSize;
			_isByRef = false;
		}

		private CliBasicType _cliBasicType;
		public CliBasicType CliBasicType
		{
			get { return _cliBasicType; }
		}

		private bool _isSigned;
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
			get { return "" + CliType + (IsByRef ? "&" : ""); }
		}

		private CliNumericSize _numericSize;
		/// <summary>
		/// Size of a numeric value in bytes.
		/// </summary>
		public CliNumericSize NumericSize
		{
			get { return _numericSize; }
		}

		private bool _isByRef;
		/// <summary>
		/// Indicates whether the type is a managed pointer (&amp; type).
		/// </summary>
		public bool IsByRef
		{
			get { return _isByRef; }
		}

		public StackTypeDescription GetByRef()
		{
			if (_isByRef)
				throw new InvalidOperationException("Already byref.");

			StackTypeDescription e = this;
			e._isByRef = true;
			return e;
		}

		public StackTypeDescription GetByValue()
		{
			if (!_isByRef)
				throw new InvalidOperationException("Type is not byref.");

			StackTypeDescription e = this;
			e._isByRef = false;
			return e;
		}

		#region Standard equality stuff.

		public static bool operator ==(StackTypeDescription x, StackTypeDescription y)
		{
			return x._cliBasicType == y._cliBasicType &&
			       x._isByRef == y._isByRef &&
			       x._isSigned == y._isSigned &&
			       x._numericSize == y._numericSize;
		}

		public static bool operator !=(StackTypeDescription x, StackTypeDescription y)
		{
			return !(x == y);
		}

		public override bool Equals(object obj)
		{
			StackTypeDescription? o = obj as StackTypeDescription?;
			if (o != null)
				return this == o;
			else
				return false;
		}

		public override int GetHashCode()
		{
			int result = _cliBasicType.GetHashCode();
			result = 29*result + _isSigned.GetHashCode();
			result = 29*result + _numericSize.GetHashCode();
			result = 29*result + _isByRef.GetHashCode();
			return result;
		}
		#endregion // Standard equality stuff.
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
