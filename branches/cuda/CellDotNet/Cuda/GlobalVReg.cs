using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using CellDotNet.Intermediate;

namespace CellDotNet.Cuda
{
	/// <summary>
	/// The new stack type enum.
	/// </summary>
	enum StackType
	{
		None,
		I2,
		I4,
		I8,
		R4,
		R8,
		ValueType,
		Object,
		ManagedPointer,
		UnmanangedPointer,
	}

	/// <summary>
	/// Used by <see cref="GlobalVReg"/> to make is possible to determine how vregs should be treated and declared.
	/// </summary>
	enum VRegType
	{
		None,
		Register,
		Global,
		Local,
		Parameter,
		Shared,
		Texture,
		Constant,
		SpecialRegister,
		Immediate,
		/// <summary>
		/// An address value as defined by a ptx array.
		/// </summary>
		Address
	}

	/// <summary>
	/// This one currently represents any variables, argument, value on the stack, constant or special register.
	/// </summary>
	class GlobalVReg
	{
		public VRegType Type { get; private set; }
		public StackType StackType { get; private set; }
		public Type ReflectionType { get; private set; }
		public object ImmediateValue { get; private set; }

		/// <summary>
		/// A name / textual representation which will be used in assembler, 
		/// except for values of type <see cref="VRegType.Constant"/> which will use <see cref="ImmediateValue"/>.
		/// </summary>
		public string Name { get; set; }


		private GlobalVReg() { }

		public static GlobalVReg FromNumericType(StackType stacktype, VRegType type)
		{
			return new GlobalVReg { StackType = stacktype, Type = type };
		}

		public static GlobalVReg FromType(StackType stacktype, Type reflectionType, VRegType type)
		{
			return new GlobalVReg { StackType = stacktype, Type = type, ReflectionType = reflectionType };
		}

		public static GlobalVReg FromImmediate(object immediateValue, StackType stacktype)
		{
			return new GlobalVReg { StackType = stacktype, ImmediateValue = immediateValue, Type = VRegType.Immediate };
		}

		public static GlobalVReg FromImmediate(object immediateValue)
		{
			return FromImmediate(immediateValue, GetStackTypeForNumericType(immediateValue));
		}

		public static GlobalVReg FromSpecialRegister(StackType stacktype, VRegType type, string text)
		{
			return new GlobalVReg {Name = text, StackType = stacktype, Type = type};
		}

		public static GlobalVReg FromStaticField(FieldInfo field)
		{
			return new GlobalVReg { StackType = GetStackType(field.FieldType), Name = EncodeFieldName(field), Type = VRegType.Address };
		}

		private static string EncodeFieldName(FieldInfo field)
		{
			return field.Name;
		}

		public string GetAssemblyText()
		{
			if (ImmediateValue != null)
			{
				if (ImmediateValue is float || ImmediateValue is double)
				{
					string s = ImmediateValue.ToString();
					if (s.IndexOf('.') == -1)
						return ImmediateValue + ".0";
					return s;
				}
				return ImmediateValue.ToString();
			}
			else return Name;
		}

		public static GlobalVReg FromStackTypeDescription(StackTypeDescription stackType, VRegType type)
		{
			switch (stackType.CliType)
			{
				case CliType.Int32:
					return FromNumericType(StackType.I4, type);
				case CliType.Int64:
					return FromNumericType(StackType.I8, type);
				case CliType.Float32:
					return FromNumericType(StackType.R4, type);
				case CliType.Float64:
					return FromNumericType(StackType.R8, type);
				case CliType.ObjectType:
					{
						if (stackType.IsArray)
						{
//							StackType elementtype;
//							switch (stackType.GetArrayElementType().CliType)
//							{
//								case CliType.Int32: elementtype = StackType.I4; break;
//								case CliType.Float32: elementtype = StackType.R4; break;
//								case CliType.Int64: elementtype = StackType.I8; break;
//								case CliType.Float64: elementtype = StackType.R8; break;
//								case CliType.: elementtype = StackType.R4; break;
//									
//							}
							// the type isn't very accurate, but it will do for now...
							return FromType(StackType.Object, typeof(Array), type);
						}
						else
							return FromType(StackType.Object, stackType.ComplexType.ReflectionType, type);
					}
				case CliType.ValueType:
					return FromType(StackType.ValueType, stackType.ComplexType.ReflectionType, type);
				case CliType.ManagedPointer:
					return FromNumericType(StackType.ManagedPointer, type);
//					throw new NotImplementedException();
				case CliType.NativeInt:
					return FromNumericType(StackType.UnmanangedPointer, type);
				default:
					throw new ArgumentOutOfRangeException("stackType", "Bad CliType: " + stackType.CliType);
			}
		}

		/// <summary>
		/// Returns the size, including any padding
		/// </summary>
		/// <returns></returns>
		public int GetElementSize()
		{
			switch (StackType)
			{
				case StackType.I4:
				case StackType.R4:
					return 4;
				case StackType.I8:
				case StackType.R8:
					return 8;
				default:
					throw new NotImplementedException();
			}
		}

		private static StackType GetStackTypeForNumericType(object value)
		{
			if (value is int || value is uint)
				return StackType.I4;
			if (value is float)
				return StackType.R4;
			if (value is Double)
				return StackType.R8;
			throw new ArgumentOutOfRangeException("value", value, "wtf");
		}

		private static StackType GetStackType(Type type)
		{
			switch (System.Type.GetTypeCode(type))
			{
				case TypeCode.Int32:
				case TypeCode.UInt32:
					return StackType.I4;
				case TypeCode.Single:
					return StackType.R4;
				case TypeCode.Object:
					if (type.IsAssignableFrom(typeof(ValueType)))
						throw new NotSupportedException("Only some built-in value types are supported.");
					return StackType.Object;
				default:
					throw new ArgumentOutOfRangeException("type", type, "wtf");
			}
		}
	}
}
