using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapsuleFileSystem
{
    /// <summary>
    /// Author: Amadeusz Sadowski
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            byte fileName = 0xAD;
            var device = new CapsuleDevice(fileName);
            device.Mount();
            bool exceptionCaught = false;
            try
            {
                device.Write((byte)(fileName + 0x1), 0, new byte[] { 0, 0 });
            }
            catch (NotSupportedException)
            {
                // good, that was incorrect file name
                exceptionCaught = true;
            }
            if (!exceptionCaught) throw new Exception();
            // test positive
            exceptionCaught = false;
            // not we expect unmount during operation
            device.IsUnmountInterruptOn = true;
            try
            {
                device.Write(fileName, 0, new byte[] { 0xFF, 0xF0, 0x00, 0x1F, 0x80 });
            }
            catch (DeviceUnmountedException)
            {
                exceptionCaught = true;
                // good, an exception should've been thrown
            }
            if (!exceptionCaught) throw new Exception();
            // break to debug and see memory contents
            device.Mount(); // repairing
            // debug and see the memory is repaired
        }
    }
}
