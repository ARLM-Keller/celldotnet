using System;
using System.Collections.Generic;

namespace CellDotNet
{
	internal class HardwareRegister// : StoreLocation
	{
		// arrays implementere IList, og array bliver redonly når der bruges som IList.
		private static VirtualRegister[] _virtualHardwareRegisters;

		public static VirtualRegister[] VirtualHardwareRegisters
		{
			get { return _virtualHardwareRegisters; }
		}

		private static List<VirtualRegister> _callerSavesVirtualRegisters;

		public static List<VirtualRegister> CallerSavesRegisters
		{
			get { return _callerSavesVirtualRegisters; }
		}

		private static VirtualRegister[] _scratchVirtualRegisters;

		public static VirtualRegister[] ScratchVirtualRegisters
		{
			get { return _scratchVirtualRegisters; }
		}

		private static List<VirtualRegister> _calleeSavesVirtualRegisters;

		public static List<VirtualRegister> CalleeSavesRegisters
		{
			get { return _calleeSavesVirtualRegisters; }
		}

		/// <summary>
		/// The Link Register.
		/// </summary>
		public static VirtualRegister LR;

		/// <summary>
		/// R3, the first return value register.
		/// </summary>
		public static readonly VirtualRegister HardwareReturnValueRegister;


		/// <summary>
		/// The Stack Pointer register.
		/// </summary>
		public static readonly VirtualRegister SP;

		/// <summary>
		/// The Environment Pointer register.
		/// </summary>
		public static readonly VirtualRegister EnvPtr;

		public static CellRegister[] getCallerSavesCellRegisters()
		{
			CellRegister[] r = new CellRegister[72];
			for (int i = 3; i < numberOfCallerSaveRegister + 3; i++)
				r[i - 3] = (CellRegister) i;
			return r;
		}

		public static CellRegister[] getScratchSavesCellRegisters()
		{
			CellRegister[] r = new CellRegister[5];
			for (int i = 75; i <= 79; i++)
				r[i - 75] = (CellRegister) i;
			return r;
		}

		public static CellRegister[] getCalleeSavesCellRegisters()
		{
			CellRegister[] r = new CellRegister[48];
			for (int i = 80; i < numberOfCalleeSaveRegister + 80; i++)
				r[i - 80] = (CellRegister) i;
			return r;
		}

		// Max numbers, caller = 72, callee = 48
		private const int numberOfCallerSaveRegister = 72; //72
		private const int numberOfCalleeSaveRegister = 48; //48
//		private const int numberOfCallerSaveRegister = 6; //72
//		private const int numberOfCalleeSaveRegister = 16; //48

		static HardwareRegister()
		{
//			throw new Exception("NEJ!!");
			_virtualHardwareRegisters = new VirtualRegister[128];

			for (int i = 0; i <= 127; i++)
			{
				_virtualHardwareRegisters[i] = new VirtualRegister();
				_virtualHardwareRegisters[i].Register = (CellRegister) i;
			}

			_callerSavesVirtualRegisters = new List<VirtualRegister>(numberOfCallerSaveRegister);

			_scratchVirtualRegisters = new VirtualRegister[5];

			_calleeSavesVirtualRegisters = new List<VirtualRegister>(numberOfCalleeSaveRegister);

			for (int i = 3; i < numberOfCallerSaveRegister+3; i++)
				_callerSavesVirtualRegisters.Add(_virtualHardwareRegisters[i]);

			for (int i = 75; i <= 79; i++)
				_scratchVirtualRegisters[i - 75] = _virtualHardwareRegisters[i];

			for (int i = 80; i < numberOfCalleeSaveRegister+80; i++)
				_calleeSavesVirtualRegisters.Add(_virtualHardwareRegisters[i]);

			LR = GetHardwareRegister((CellRegister) 0);

			SP = GetHardwareRegister((CellRegister) 1);

			HardwareReturnValueRegister = GetHardwareRegister((CellRegister) 3);

			EnvPtr = GetHardwareRegister((CellRegister) 2);
		}

		// TODO skal udfases
		private int _register;

		public int Register
		{
			get { return _register; }
			set { _register = value; }
		}

		public static VirtualRegister GetHardwareRegister(int regnum)
		{
			if (regnum < 0 || regnum > 127)
				throw new ArgumentOutOfRangeException("regnum", regnum, "0 <= x <= 127");

			return GetHardwareRegister((CellRegister) regnum);
		}

		public static VirtualRegister GetHardwareRegister(CellRegister cr)
		{
			return _virtualHardwareRegisters[(int) cr];
		}

		public static VirtualRegister GetHardwareArgumentRegister(int argumentnum)
		{
			if (argumentnum < 0 || argumentnum > 71)
				throw new ArgumentOutOfRangeException("argumentnum", argumentnum, "0 <= x <= 71");

			return GetHardwareRegister(3 + argumentnum);
		}
	}

	public enum CellRegister
	{
		REG_0,
		REG_1,
		REG_2,
		REG_3,
		REG_4,
		REG_5,
		REG_6,
		REG_7,
		REG_8,
		REG_9,
		REG_10,
		REG_11,
		REG_12,
		REG_13,
		REG_14,
		REG_15,
		REG_16,
		REG_17,
		REG_18,
		REG_19,
		REG_20,
		REG_21,
		REG_22,
		REG_23,
		REG_24,
		REG_25,
		REG_26,
		REG_27,
		REG_28,
		REG_29,
		REG_30,
		REG_31,
		REG_32,
		REG_33,
		REG_34,
		REG_35,
		REG_36,
		REG_37,
		REG_38,
		REG_39,
		REG_40,
		REG_41,
		REG_42,
		REG_43,
		REG_44,
		REG_45,
		REG_46,
		REG_47,
		REG_48,
		REG_49,
		REG_50,
		REG_51,
		REG_52,
		REG_53,
		REG_54,
		REG_55,
		REG_56,
		REG_57,
		REG_58,
		REG_59,
		REG_60,
		REG_61,
		REG_62,
		REG_63,
		REG_64,
		REG_65,
		REG_66,
		REG_67,
		REG_68,
		REG_69,
		REG_70,
		REG_71,
		REG_72,
		REG_73,
		REG_74,
		REG_75,
		REG_76,
		REG_77,
		REG_78,
		REG_79,
		REG_80,
		REG_81,
		REG_82,
		REG_83,
		REG_84,
		REG_85,
		REG_86,
		REG_87,
		REG_88,
		REG_89,
		REG_90,
		REG_91,
		REG_92,
		REG_93,
		REG_94,
		REG_95,
		REG_96,
		REG_97,
		REG_98,
		REG_99,
		REG_100,
		REG_101,
		REG_102,
		REG_103,
		REG_104,
		REG_105,
		REG_106,
		REG_107,
		REG_108,
		REG_109,
		REG_110,
		REG_111,
		REG_112,
		REG_113,
		REG_114,
		REG_115,
		REG_116,
		REG_117,
		REG_118,
		REG_119,
		REG_120,
		REG_121,
		REG_122,
		REG_123,
		REG_124,
		REG_125,
		REG_126,
		REG_127
	}
}
