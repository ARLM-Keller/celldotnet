using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CellDotNet
{
	/// <summary>
	/// This is our own representation of a type. It wraps a <see c="System.ReflectionType" /> and also
	/// contains runtime information about the type, such as field offsets and methods.
	/// 
	/// <para>
	/// Every TypeDescription instance is a complete type; that is, it does not represent a generic
	/// type without type parameters or an array without element type.
	/// </para>
	/// 
	/// TODO: Figure out what to do about generics.
	/// </summary>
	class TypeDescription
	{
		private Type _reflectionType;
		/// <summary>
		/// Contains the <see cref="System.Type"/> representation of the type.
		/// </summary>
		public Type ReflectionType
		{
			get { return _reflectionType; }
		}

		/// <summary>
		/// Returns the number of quadwords that instances of the type takes up.
		/// </summary>
		public int QuadWordCount
		{
			get
			{
				return Utilities.Align16(Marshal.SizeOf(_reflectionType)) / 16;
			}
		}

		public TypeDescription(Type type)
		{
			if (type == null)
				throw new ArgumentNullException();

			if (type.IsByRef || type.IsPointer)
				throw new ArgumentException("Argument is & or *.");
			if (type.IsArray)
				throw new ArgumentException("Argument is array.");
			if (type.IsGenericTypeDefinition)
				throw new ArgumentException("Argument is a generic type.");

			_reflectionType = type;
		}



		public TypeDescription(GenericType genericType, params StackTypeDescription[] genericParameters)
		{
			if (genericType == null)
				throw new ArgumentNullException();
			if (genericParameters == null || genericParameters.Length == 0)
				throw new ArgumentException("genericParameters");

			// Create the type with the runtime to verify that the combination is okay.
			List<Type> genTypes = new List<Type>();
			foreach (StackTypeDescription t in genericParameters)
				genTypes.Add(t.GetNonPointerType());
			_reflectionType = genericType.Type.MakeGenericType(genTypes.ToArray());

			_genericType = genericType;
			_genericParameters = genericParameters;
		}

//		/// <summary>
//		/// Creates a description of an array.
//		/// </summary>
//		/// <param name="type"></param>
//		/// <param name="arrayDimension"></param>
//		public TypeDescription(StackTypeDescription type, int arrayDimension)
//		{
//			if (type == null)
//				throw new ArgumentNullException("type");
//			if (arrayDimension < 1)
//				throw new ArgumentOutOfRangeException("arrayDimension");
//
//			_reflectionType = type;
//			_arrayDimension = arrayDimension;
//		}

//		private int _arrayDimension;
//		public int ArrayDimension
//		{
//			get { return _arrayDimension; }
//		}


		private GenericType _genericType;
		public GenericType GenericType
		{
			get { return _genericType; }
		}

		private StackTypeDescription[] _genericParameters;
		public IList<StackTypeDescription> GenericParameters
		{
			get { return _genericParameters; }
		}
	}

	/// <summary>
	/// A generic type without type parameters.
	/// </summary>
	class GenericType
	{
		private Type _type;
		/// <summary>
		/// The generic type without parameters.
		/// </summary>
		public Type Type
		{
			get { return _type; }
		}

		public GenericType(Type type)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (!type.IsGenericTypeDefinition)
				throw new ArgumentException("type argument is not generic.");

			_type = type;
		}
	}
}
