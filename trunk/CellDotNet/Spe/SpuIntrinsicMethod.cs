// 
// Copyright (C) 2007 Klaus Hansen and Rasmus Halland
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet.Spe
{
	/// <summary>
	/// These values represents intrinsic functions.
	/// </summary>
	internal enum SpuIntrinsicMethod
	{
		None,
		Runtime_Stop,

		Mfc_GetAvailableQueueEntries,
		Mfc_Put,
		Mfc_Get,
		Vector_GetWord0,
		Vector_GetWord1,
		Vector_GetWord2,
		Vector_GetWord3,
		Vector_PutWord0,
		Vector_PutWord1,
		Vector_PutWord2,
		Vector_PutWord3,
		Int_Equals,
		Int_NotEquals,
		Float_Equals,
		Float_NotEquals,
		ReturnArgument1,
		CombineFourWords,
		SplatWord,
		CompareGreaterThanIntAndSelect,
		CompareGreaterThanFloatAndSelect,
		CompareEqualsIntAndSelect,
		ConvertIntToFloat,
		ConvertFloatToInteger,
		ConditionalSelectWord,
		ConditionalSelectVector
	}
}
