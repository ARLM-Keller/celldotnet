using System;
using System.Collections.Generic;
using System.Linq;
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
	enum VRegStorage
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
		Immediate
	}

	/// <summary>
	/// This one currently represents any variables, argument, value on the stack, constant or special register.
	/// </summary>
	class GlobalVReg
	{
		public VRegStorage Storage { get; private set; }
		public StackType StackType { get; private set; }
		public Type ReflectionType { get; private set; }
		public object ImmediateValue { get; private set; }

		/// <summary>
		/// A name / textual representation which will be used in assembler, 
		/// except for values of type <see cref="VRegStorage.Constant"/> which will use <see cref="ImmediateValue"/>.
		/// </summary>
		public string Name { get; set; }


		private GlobalVReg() { }

		public static GlobalVReg FromNumericType(StackType stacktype, VRegStorage storage)
		{
			return new GlobalVReg { StackType = stacktype, Storage = storage };
		}

		public static GlobalVReg FromType(StackType stacktype, Type reflectionType, VRegStorage storage)
		{
			return new GlobalVReg { StackType = stacktype, Storage = storage, ReflectionType = reflectionType };
		}

		public static GlobalVReg FromImmediate(object immediateValue, StackType stacktype)
		{
			return new GlobalVReg { StackType = stacktype, ImmediateValue = immediateValue, Storage = VRegStorage.Immediate };
		}

		public static GlobalVReg FromImmediate(object immediateValue)
		{
			return FromImmediate(immediateValue, GetStackTypeForNumericType(immediateValue));
		}

		public string GetAssemblyText()
		{
			return ImmediateValue != null ? ImmediateValue.ToString() : Name;
		}

		public static GlobalVReg FromStackTypeDescription(StackTypeDescription stackType, VRegStorage storage)
		{
			switch (stackType.CliType)
			{
				case CliType.Int32:
					return FromNumericType(StackType.I4, storage);
				case CliType.Int64:
					return FromNumericType(StackType.I8, storage);
				case CliType.Float32:
					return FromNumericType(StackType.R4, storage);
				case CliType.Float64:
					return FromNumericType(StackType.R8, storage);
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
							return FromType(StackType.Object, typeof(Array), storage);
						}
						else
							return FromType(StackType.Object, stackType.ComplexType.ReflectionType, storage);
					}
				case CliType.ValueType:
					return FromType(StackType.ValueType, stackType.ComplexType.ReflectionType, storage);
				case CliType.ManagedPointer:
					return FromNumericType(StackType.ManagedPointer, storage);
//					throw new NotImplementedException();
				case CliType.NativeInt:
					return FromNumericType(StackType.UnmanangedPointer, storage);
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

	}
}
