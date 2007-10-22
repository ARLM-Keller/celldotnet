using System;
using System.Collections.Generic;

namespace CellDotNet
{
	/// <summary>
	/// Annotate a struct with this attribute to indicate that it is immutable. This will cause
	/// it to be stored in a register if possible. Only put this attribute on structs that fits in a single register.
	/// </summary>
	internal sealed class ImmutableAttribute : Attribute
	{
	}
}
