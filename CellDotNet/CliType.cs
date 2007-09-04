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
	struct StackTypeDescription : IEquatable<StackTypeDescription>
	{
		public static readonly StackTypeDescription None = new StackTypeDescription();
		public static readonly StackTypeDescription Int32 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.FourBytes, true);
		public static readonly StackTypeDescription Int64 = new StackTypeDescription(CliBasicType.Integer, CliNumericSize.EightBytes, true);
		public static readonly StackTypeDescription Float32 = new StackTypeDescription(CliBasicType.Floating, CliNumericSize.FourBytes, true);
		public static readonly StackTypeDescription Float64 = new StackTypeDescription(CliBasicType.Floating, CliNumericSize.EightBytes, true);
		public static readonly StackTypeDescription ObjectType = new StackTypeDescription(CliBasicType.ObjectType, CliNumericSize.None, false);
		public static readonly StackTypeDescription ValueType = new StackTypeDescription(CliBasicType.Valuetype, CliNumericSize.None, false);
		public static readonly StackTypeDescription NativeInt = new StackTypeDescription(CliBasicType.NativeInt, CliNumericSize.None, true);

		public CliBasicType _cliBasicType;
		private bool _isSigned;
		private CliNumericSize _numericSize;
		private byte _indirectionLevel;
		private bool _isManaged;
		private bool _isArray;
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
			_isArray = false;
		}

		static public StackTypeDescription GetStackType(CliType clitype)
		{
			switch (clitype)
			{
				case CliType.Int32:
					return Int32;
				case CliType.Int64:
					return Int64;
				case CliType.NativeInt:
					return NativeInt;
				case CliType.Float32:
					return Float32;
				case CliType.Float64:
					return Float64;
				case CliType.ValueType:
				case CliType.ObjectType:
				case CliType.ManagedPointer:
					throw new NotImplementedException();
				default:
					throw new ArgumentException();
			}
		}

		/// <summary>
		/// For complex types.
		/// </summary>
		/// <param name="complexType"></param>
		public StackTypeDescription(TypeDescription complexType)
		{
			_cliBasicType = complexType.ReflectionType.IsValueType ? CliBasicType.Valuetype : CliBasicType.ObjectType;
			_isSigned = false;
			_numericSize = CliNumericSize.None;
			_indirectionLevel = 0;
			_isManaged = false;
			_complexType = complexType;
			_isArray = complexType.ReflectionType.IsArray;

			Utilities.PretendVariableIsUsed(DebuggerDisplay);
		}

		public CliBasicType CliBasicType
		{
			get
			{
				AssertSimple();
				return _cliBasicType;
			}
		}

		public TypeDescription ComplexType
		{
			get
			{
				AssertSimple();
				return _complexType;
			}
		}

		public bool IsSigned
		{
			get
			{
				AssertSimple();
				return _isSigned;
			}
		}

		/// <summary>
		/// You probably shouldn't use this property.
		/// </summary>
		internal CliType CliType
		{
			get
			{
				if (IndirectionLevel > 0)
				{
					if (IsManagedPointer)
						return CliType.ManagedPointer;
					else if (IndirectionLevel > 0)
						return CliType.NativeInt;
				}

				CliType t;
				switch (_cliBasicType)
				{
					case CliBasicType.Integer:
						switch (_numericSize)
						{
							case CliNumericSize.OneByte:
							case CliNumericSize.TwoBytes:
							case CliNumericSize.FourBytes:
								t = CliType.Int32;
								break;
							case CliNumericSize.EightBytes:
								t = CliType.Int64;
								break;
							default:
								throw new Exception();
						}
						break;
					case CliBasicType.Floating:
						if (_numericSize == CliNumericSize.FourBytes)
						{
							t = CliType.Float32;
							break;
						}
						else if (_numericSize == CliNumericSize.EightBytes)
						{
							t = CliType.Float64;
							break;
						}
						else
							throw new Exception();
					case CliBasicType.Valuetype:
						t = CliType.ValueType;
						break;
					case CliBasicType.ObjectType:
						t = CliType.ObjectType;
						break;
					case CliBasicType.NativeInt:
						t = CliType.NativeInt;
						break;
					case CliBasicType.None:
						t = CliType.None;
						break;
					default:
						throw new Exception();
				}

				return t;
			}
		}


		private string DebuggerDisplay
		{
			get { return ToString(); }
		}

		/// <summary>
		/// Size of a numeric value in bytes.
		/// </summary>
		public CliNumericSize NumericSize
		{
			get
			{
				AssertSimple();
				return _numericSize;
			}
		}

		/// <summary>
		/// Indicates whether this is an 1D array.
		/// </summary>
		public bool IsArray
		{
			get { return _isArray; }
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
		public bool IsManagedPointer
		{
			get
			{
				Utilities.Assert(_indirectionLevel > 0, "_indirectionLevel > 0");
				return _isManaged;
			}
		}

		/// <summary>
		/// Asserts that this is a simple type; that is, that this is a value type and 
		/// not a pointer or an array.
		/// </summary>
		private void AssertSimple()
		{
			if (IsArray || IndirectionLevel > 0)
				throw new InvalidOperationException("Invalid operation since this is not a simple type.");
		}

		/// <summary>
		/// Returns a type that repre
		/// </summary>
		/// <returns></returns>
		public StackTypeDescription GetArrayType()
		{
			if (_isArray)
				throw new NotSupportedException("Arrays of arrays are not supported.");

			StackTypeDescription arr = this;
			arr._isArray = true;

			return arr;
		}

		public StackTypeDescription GetArrayElementType()
		{
			if (!_isArray)
				throw new InvalidOperationException("This type is not an array type.");

			StackTypeDescription std = this;
			std._isArray = false;

			return std;
		}

		public StackTypeDescription GetPointer()
		{
			if (_isManaged)
				throw new InvalidOperationException();

			StackTypeDescription rv = this;
			rv._indirectionLevel++;
			return rv;
		}

		public int GetSizeWithPadding()
		{
			switch (CliBasicType)
			{
				case CliBasicType.Integer:
				case CliBasicType.Floating:
					return (int) NumericSize;
				default:
					throw new InvalidOperationException();
			}
		}

		public StackTypeDescription Dereference()
		{
			if (IndirectionLevel == 0)
				throw new InvalidOperationException("ReflectionType is not byref.");

			StackTypeDescription e = this;
			e._indirectionLevel--;
			e._isManaged = false;
			return e;
		}

		public StackTypeDescription DereferenceFully()
		{
			if (IndirectionLevel == 0)
				throw new InvalidOperationException("ReflectionType is not byref.");

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

			switch (CliType)
			{
				case CliType.None:
					return null;
				case CliType.Int32:
					return typeof(int);
				case CliType.Int64:
					return typeof(long);
				case CliType.NativeInt:
					return typeof(IntPtr);
				case CliType.Float32:
					return typeof(sbyte);
				case CliType.Float64:
					return typeof(float);
				case CliType.ValueType:
				case CliType.ObjectType:
					return ComplexType.ReflectionType;
				case CliType.ManagedPointer:
				default:
					throw new InvalidOperationException();
			}
		}

		public override string ToString()
		{
			string s;
			if (IsArray)
				s = GetArrayElementType().CliType.ToString();
			else if (IndirectionLevel > 0)
				s = DereferenceFully().CliType.ToString();
			else
				s = CliType.ToString();

			if (IndirectionLevel > 0)
			{
				// I guess this is not entirely correct if you got an unmanaged pointer to
				// a managed pointer...
				if (IsManagedPointer)
					s += new string('&', IndirectionLevel);
				else
					s += new string('*', IndirectionLevel);
			}

			if (IsArray)
				s += "[]";

			return s;
		}

		#region Equality stuff

		static public bool operator==(StackTypeDescription x, StackTypeDescription y)
		{
			return (x._cliBasicType == y._cliBasicType) &&
			       (x._complexType == y._complexType) &&
			       (x._indirectionLevel == y._indirectionLevel) &&
			       (x._isManaged == y._isManaged) &&
			       (x._isSigned == y._isSigned) &&
				   (x._isArray == y._isArray) &&
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
			result = 29*result + _isArray.GetHashCode();
			result = 29*result + (_complexType != null ? _complexType.GetHashCode() : 0);
			return result;
		}

		public bool Equals(StackTypeDescription other)
		{
			return this == other;
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
	/// </remarks>
	/// </summary>
	internal enum CliType
	{
		None = 0,
		Int32 = 1,
		Int64 = 2,
		NativeInt = 3,
		Float32 = 4,
		Float64 = 5,
		/// <summary>
		/// Any value type.
		/// </summary>
		ValueType = 6,
		/// <summary>
		/// Any object type.
		/// </summary>
		ObjectType = 7,
		/// <summary>
		/// The "&amp;" type.
		/// </summary>
		ManagedPointer = 8,
	}
}
