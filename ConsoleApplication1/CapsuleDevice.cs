using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class CapsuleDevice
    {
        private bool isMounted = false;
        public void Mount()
        {
            isMounted = true;
        }

        public void Unmount()
        {
            isMounted = false;
        }

        public void Write(byte fileName, byte offset, byte[] buffer)
        {

        }

        private bool CanSave(byte address, byte blockCount)
        {
            return false;
        }
    }
}
