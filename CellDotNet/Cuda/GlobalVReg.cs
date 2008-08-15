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
	/// This one currently represents any variables, argument, value on the stack, constant or special register.
	/// </summary>
	class GlobalVReg
	{
		public StackType StackType { get; private set; }
		public Type ReflectionType { get; private set; }
		public int ID { get; private set; }

		/// <summary>
		/// Used by ptx gen to declare parameters.
		/// </summary>
		public bool IsParameter { get; set; }

		private object _assemblyRepresentation;

		private GlobalVReg() { }

		public static GlobalVReg FromNumericType(StackType stacktype, int id)
		{
			return new GlobalVReg { StackType = stacktype, ID = id };
		}

		public static GlobalVReg FromType(StackType stacktype, Type reflectionType, int id)
		{
			return new GlobalVReg { StackType = stacktype, ReflectionType = reflectionType, ID = id };
		}

		public static GlobalVReg FromValue(StackType stacktype, Type reflectionType, object assemblyRepresentation)
		{
			return new GlobalVReg { StackType = stacktype, ReflectionType = reflectionType, _assemblyRepresentation = assemblyRepresentation };
		}

		public override string ToString()
		{
			return _assemblyRepresentation != null ? _assemblyRepresentation.ToString() : base.ToString();
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
