using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
	/// <summary>
	/// The basic CLI types.
	/// </summary>
	/// <remarks>
	/// </remarks>
	enum CliType
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
	/// CLI types as available in metadata, including classes like 
	/// values types, objects and method pointers.
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
	enum CilType
	{
		None = 0,
		Int8,
		UInt8,
		Bool,
		Int16,
		UInt16,
		Char,
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
		/// <summary>
		/// Any method pointer.
		/// </summary>
		MethodPointer
	}
}
