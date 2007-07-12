using System;
using System.Collections.Generic;
using System.Text;

namespace CellDotNet
{
    class StackLocation : StoreLocation
    {
        public StackLocation() { }

        public StackLocation(int frameoffset)
        {
            _frameOffset = frameoffset;
        }

        private int _frameOffset;
        public int FrameOffset
        {
            get { return _frameOffset; }
            set { _frameOffset = value; }
        }


    }
}
