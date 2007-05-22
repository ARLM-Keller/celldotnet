using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    class HardwareRegister : StorLocation
    {
        public static List<StorLocation> getCellRegisteres()
        {
            List<HardwareRegister> regs = new List<HardwareRegister>();

            for (int i = 0; i <= 127; i++ )
            {
                HardwareRegister hr = new HardwareRegister();
                hr.Register = i;
                regs.Add(hr);
            }
            return new List<StorLocation>();
        }

        public static Stack<StorLocation> getCellRegistersAsStack()
        {
            Stack<StorLocation> regsStack = new Stack<StorLocation>();
            List<StorLocation> regs = HardwareRegister.getCellRegisteres();

            for (int i = regs.Count - 1; i >= 0; i++)
            {
                regsStack.Push(regs[i]);
            }

            return regsStack;
        }

        private int _register;
        public int Register
        {
            get { return _register; }
            set { _register = value; }
        }
    }
/*
    public enum CellRegister
    {
        REG_0, REG_1, REG_2, REG_3, REG_4, REG_5, REG_6, REG_7, REG_8, REG_9,
        REG_10, REG_11, REG_12, REG_13, REG_14, REG_15, REG_16, REG_17, REG_18, REG_19,
        REG_20, REG_21, REG_22, REG_23, REG_24, REG_25, REG_26, REG_27, REG_28, REG_29,
        REG_30, REG_31, REG_32, REG_33, REG_34, REG_35, REG_36, REG_37, REG_38, REG_39,
        REG_40, REG_41, REG_42, REG_43, REG_44, REG_45, REG_46, REG_47, REG_48, REG_49,
        REG_50, REG_51, REG_52, REG_53, REG_54, REG_55, REG_56, REG_57, REG_58, REG_59,
        REG_60, REG_61, REG_62, REG_63, REG_64, REG_65, REG_66, REG_67, REG_68, REG_69,
        REG_70, REG_71, REG_72, REG_73, REG_74, REG_75, REG_76, REG_77, REG_78, REG_79,
        REG_80, REG_81, REG_82, REG_83, REG_84, REG_85, REG_86, REG_87, REG_88, REG_89,
        REG_90, REG_91, REG_92, REG_93, REG_94, REG_95, REG_96, REG_97, REG_98, REG_99,
        REG_100, REG_101, REG_102, REG_103, REG_104, REG_105, REG_106, REG_107, REG_108, REG_109,
        REG_110, REG_111, REG_112, REG_113, REG_114, REG_115, REG_116, REG_117, REG_118, REG_119,
        REG_120, REG_121, REG_122, REG_123, REG_124, REG_125, REG_126, REG_127
    }
 */
}
