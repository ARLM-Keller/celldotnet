using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// This attribute indicates that the method to which it is applied should be translated directly into the
	/// specified SPU opcode.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	class SpuOpCodeAttribute : Attribute
	{
	}
}
