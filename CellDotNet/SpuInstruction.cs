using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil.Cil;

namespace CellDotNet
{
    /// <summary>
    /// Represents an SPU instruction.
    /// </summary>
    class SpuInstruction
    {
        private SpuOpCode _opcode;
        public SpuOpCode OpCode
        {
            get { return _opcode; }
            set { _opcode = value; }
        }

        private int _constant;
        public int Constant
        {
            get { return _constant; }
            set { _constant = value; }
        }

        private VirtualRegister _source1;

        public VirtualRegister Source1
        {
            get { return _source1; }
            set { _source1 = value; }
        }

        private VirtualRegister _source2;

        public VirtualRegister Source2
        {
            get { return _source2; }
            set { _source2 = value; }
        }

        private VirtualRegister _source3;

        public VirtualRegister Source3
        {
            get { return _source3; }
            set { _source3 = value; }
        }

        // Maby a little over kill, but result in more nice/better programming.
        public ICollection<VirtualRegister> Sources
        {
            get
            {
                ICollection<VirtualRegister> s = new LinkedList<VirtualRegister>();
                if (_source1 != null) s.Add(_source1);
                if (_source2 != null) s.Add(_source2);
                if (_source3 != null) s.Add(_source3);
                return s;
            }
        }

        private VirtualRegister _destination;

        public VirtualRegister Destination
        {
            get { return _destination; }
            set { _destination = value; }
        }
    }
}
