using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Metadata;

namespace CellDotNet
{
	/// <summary>
	/// Cecil basic .NET data types.
	/// </summary>
	class BaseTypes
	{
		static BaseTypes()
		{
			AssemblyDefinition corlib = AssemblyFactory.GetAssembly(typeof (int).Assembly.Location);
			foreach (TypeDefinition type in corlib.MainModule.Types)
			{
				if (type.MetadataToken == new MetadataToken(typeof(int).MetadataToken))
					_int32 = type;
				else if (type.MetadataToken == new MetadataToken(typeof(long).MetadataToken))
					_int64 = type;
				else if (type.MetadataToken == new MetadataToken(typeof(bool).MetadataToken))
					_bool = type;
				else if (type.MetadataToken == new MetadataToken(typeof(string).MetadataToken))
					_string = type;
			}
		}

		static private TypeDefinition _int32;
		static public TypeDefinition Int32
		{
			get { return _int32; }
		}

		static private TypeDefinition _string;
		static public TypeDefinition String
		{
			get { return _string; }
		}

		static private TypeDefinition _int64;
		static public TypeDefinition Int64
		{
			get { return _int64; }
		}

		static private TypeDefinition _bool;
		static public TypeDefinition Bool
		{
			get { return _bool; }
		}

	}
}
