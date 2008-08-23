using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CellDotNet.Cuda
{
	class SpecialMethodInfo
	{
		static Dictionary<MethodBase, SpecialMethodInfo> dict = new Dictionary<MethodBase, SpecialMethodInfo>
		{
			{typeof(ThreadIndex).GetProperty("X").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%tid.x"))},
			{typeof(ThreadIndex).GetProperty("Y").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%tid.y"))},
			{typeof(ThreadIndex).GetProperty("Z").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%tid.z"))},
			{typeof(BlockSize).GetProperty("X").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ntid.x"))},
			{typeof(BlockSize).GetProperty("Y").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ntid.y"))},
			{typeof(BlockSize).GetProperty("Z").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ntid.z"))},
			{typeof(BlockIndex).GetProperty("X").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ctaid.x"))},
			{typeof(BlockIndex).GetProperty("Y").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ctaid.y"))},
			{typeof(BlockIndex).GetProperty("Z").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%ctaid.z"))},
			{typeof(GridSize).GetProperty("X").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%nctaid.x"))},
			{typeof(GridSize).GetProperty("Y").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%nctaid.y"))},
			{typeof(GridSize).GetProperty("Z").GetGetMethod(), new SpecialMethodInfo(true, GlobalVReg.FromSpecialRegister(StackType.I2, VRegStorage.SpecialRegister, "%nctaid.z"))},
		};

		public bool IsGlobalVReg { get; private set; }
		public GlobalVReg GlobalVReg { get; private set; }


		internal SpecialMethodInfo(bool isGlobalVReg, GlobalVReg globalVReg)
		{
			IsGlobalVReg = isGlobalVReg;
			GlobalVReg = globalVReg;
		}

		public static bool TryGetMethodInfo(MethodBase method, out SpecialMethodInfo specialMethodInfo)
		{
			return dict.TryGetValue(method, out specialMethodInfo);
		}

		public static bool IsSpecialMethod(MethodBase method)
		{
			return dict.ContainsKey(method);
		}
	}
}
