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

	class GlobalVReg
	{
		public StackType StackType { get; private set; }
		public Type ReflectionType { get; private set; }
		public int ID { get; private set; }

		private object _value;

		public static GlobalVReg FromNumericType(StackType stacktype, int id)
		{
			return new GlobalVReg { StackType = stacktype, ID = id };
		}

		public static GlobalVReg FromType(StackType stacktype, Type reflectionType, int id)
		{
			return new GlobalVReg { StackType = stacktype, ReflectionType = reflectionType, ID = id };
		}

		public static GlobalVReg FromValue(StackType stacktype, Type reflectionType, object value)
		{
			return new GlobalVReg { StackType = stacktype, ReflectionType = reflectionType, _value = value };
		}

		public override string ToString()
		{
			return _value != null ? _value.ToString() : base.ToString();
		}

		public static GlobalVReg FromStackTypeDescription(StackTypeDescription stackType, int id)
		{
			switch (stackType.CliType)
			{
				case CliType.Int32:
					return FromNumericType(StackType.I4, id);
				case CliType.Int64:
					return FromNumericType(StackType.I8, id);
				case CliType.Float32:
					return FromNumericType(StackType.R4, id);
				case CliType.Float64:
					return FromNumericType(StackType.R8, id);
				case CliType.ObjectType:
					return FromType(StackType.Object, stackType.ComplexType.ReflectionType, id);
				case CliType.ValueType:
					return FromType(StackType.ValueType, stackType.ComplexType.ReflectionType, id);
				case CliType.ManagedPointer:
					return FromNumericType(StackType.ManagedPointer, id);
//					throw new NotImplementedException();
				case CliType.NativeInt:
					return FromNumericType(StackType.UnmanangedPointer, id);
				default:
					throw new ArgumentOutOfRangeException("stackType", "Bad CliType: " + stackType.CliType);
			}
		}
	}
}
