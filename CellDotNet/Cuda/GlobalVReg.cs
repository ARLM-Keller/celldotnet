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
			return new GlobalVReg { StackType = stacktype };
		}

		public static GlobalVReg FromType(StackType stacktype, Type reflectionType, VRegStorage storage)
		{
			return new GlobalVReg { StackType = stacktype, Storage = storage, ReflectionType = reflectionType };
		}

		public static GlobalVReg FromValue(StackType stacktype, Type reflectionType, object immediateValue, VRegStorage storage)
		{
			return new GlobalVReg { StackType = stacktype, Storage = storage, ReflectionType = reflectionType, ImmediateValue = immediateValue };
		}

		public override string ToString()
		{
			return ImmediateValue != null ? ImmediateValue.ToString() : base.ToString();
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
					return FromType(StackType.Object, stackType.ComplexType.ReflectionType, storage);
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
	}
}
