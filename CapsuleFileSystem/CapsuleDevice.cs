using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapsuleFileSystem
{
    /// <summary>
    /// Demo capsule, allows only for testing writing to single file.
    /// </summary>
    internal class CapsuleDevice
    {
        private readonly FileSystem fileSystem;
        private bool isMounted = false;

        /// <summary>
        /// Creates capsule with single empty test file with given name.
        /// </summary>
        /// <param name="testFileName"></param>
        public CapsuleDevice(byte testFileName)
        {
            IsUnmountInterruptOn = false;
            fileSystem = new FileSystem(this);
            fileSystem.PrepareFileForTest(testFileName);
        }

        public bool IsUnmountInterruptOn { get; set; }

        public void Mount()
        {
            if (!isMounted)
                fileSystem.CheckAndRepair();
            isMounted = true;
        }

        public void Write(byte fileName, byte offset, byte[] buffer)
        {
            if (isMounted)
            {
                if (IsUnmountInterruptOn)
                {
                    isMounted = false;
                }
                fileSystem.Write(fileName, offset, buffer);
            }
            else
            {
                throw new DeviceUnmountedException();
            }
        }
    }
}
