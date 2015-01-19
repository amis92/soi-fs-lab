using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapsuleFileSystem
{
    /// <summary>
    /// It only allows for creation and writing to test file, it's a demo only.
    /// To test operations, put 'throw new DeviceUnmountedException();' somewhere.
    /// </summary>
    internal class FileSystem
    {
        /*Control flags*/
        public const byte MoveFlag = 0x10;
        public const byte NewFlag = 0x8;
        public const byte ReadFlag = 0x4;
        public const byte AppendFlag = 0x2;
        public const byte SaveFlag = 0x1;
        public const byte OkFlag = 0x0;

        /*Control block offsets*/
        public const int ControlBlockDescriptorIndexOffset = 1;
        public const int ControlBlockFileNameOffset = 3;
        public const int ControlBlockFileSizeOffset = 2;
        public const int ControlBlockFlagOffset = 0;

        /*Descriptor offsets*/
        public const int DescriptorFileLengthFieldOffset = 2;
        public const int DescriptorFilenameFieldOffset = 0;
        public const int DescriptorFirstBlockFieldOffset = 1;

        public const byte DescriptorFirstBlockNoFileValue = 0xFF;


        /*sizes*/
        public const int MaxFileCount = 64;
        public const int AllocationBlockBytes = 4;
        public const int ControlBlockBytes = 4;
        public const int DescriptorBytes = 3;
        public const int DescriptorMemoryBytes = DescriptorBytes * MaxFileCount;
        public const int MemoryMapBytes = 28;
        public const int TotalMemoryBytes = 1024;

        public const int FileMemoryBytes = TotalMemoryBytes - ControlBlockBytes
            - MemoryMapBytes - DescriptorMemoryBytes;

        /* Memory definition */
        private readonly CapsuleDevice _device;
        private readonly byte[] ControlBlock = new byte[ControlBlockBytes];
        private readonly byte[] MemoryMap = new byte[MemoryMapBytes];
        private readonly byte[] DescriptorMemory = new byte[DescriptorMemoryBytes];
        private readonly byte[] FileMemory = new byte[FileMemoryBytes];

        public FileSystem(CapsuleDevice device)
        {
            _device = device;
            /// all bytes are zeroed.
        }

        /// <summary>
        /// Creates empty file in first descriptor with provided name.
        /// </summary>
        public void PrepareFileForTest(byte fileName)
        {
            if (fileName == 0x0) throw new ArgumentException("Filename cannot be 0!");
            DescriptorMemory[DescriptorFilenameFieldOffset] = fileName;
            DescriptorMemory[DescriptorFirstBlockFieldOffset] = DescriptorFirstBlockNoFileValue; // empty file
            DescriptorMemory[DescriptorFileLengthFieldOffset] = 0x0;
        }

        public void CheckAndRepair()
        {
            switch (ControlBlock[ControlBlockFlagOffset])
            {
                case OkFlag:
                    return;
                case SaveFlag:
                    CriticalWrite();
                    break;
                // other cases not implemented
                default:
                    break;
            }
        }

        public void Write(byte fileName, byte offset, byte[] buffer)
        {
            if (fileName != DescriptorMemory[DescriptorFilenameFieldOffset])
                throw new NotSupportedException("Only test file can be written to!");
            ControlBlock[ControlBlockFlagOffset] = OkFlag;
            var descriptorIndex = FindDescriptor(fileName);
            var descriptorAddress = descriptorIndex * DescriptorBytes;
            var firstBlockAddress = DescriptorMemory[descriptorAddress + DescriptorFirstBlockFieldOffset];
            if (firstBlockAddress == DescriptorFirstBlockNoFileValue)
            {
                // empty file, assign test blocks (we have empty memory)
                firstBlockAddress = MemMapFindFreeBlocks((byte)buffer.Length);
                DescriptorMemory[descriptorAddress + DescriptorFirstBlockFieldOffset] = firstBlockAddress;
            }
            if (!CanWrite(firstBlockAddress, offset, (byte)buffer.Length)) throw new OutOfMemoryException();
            // commence buffer copying
            var firstByteToWriteAddress = (byte)(firstBlockAddress + offset);
            var lastByteToWrite = (byte)(firstByteToWriteAddress + buffer.Length - 1);
            byte i = 0;
            for (i = firstByteToWriteAddress; i < lastByteToWrite; i += 2)
            {
                WriteAtomic(i, buffer[i], buffer[i + 1]);
            }
            // for example here:
            if (_device.IsUnmountInterruptOn) throw new DeviceUnmountedException();
            if (buffer.Length % 2 == 1)
            {
                // save last byte with second null byte
                WriteAtomic(i, buffer[i], 0x0);
            }
            // save new size to control block
            var fileSize = DescriptorMemory[descriptorAddress + DescriptorFileLengthFieldOffset];
            fileSize += (byte)buffer.Length;
            ControlBlock[ControlBlockFileSizeOffset] = fileSize;
            ControlBlock[ControlBlockDescriptorIndexOffset] = descriptorIndex;
            ControlBlock[ControlBlockFlagOffset] = SaveFlag;
            CriticalWrite();
        }

        /// <summary>
        /// Finds specified number of free blocks.
        /// </summary>
        /// <param name="blockCount"></param>
        /// <returns>Address of first free block from group.</returns>
        private byte MemMapFindFreeBlocks(byte blockCount)
        {
            // it's test method
            return 0;
        }

        private void CriticalWrite()
        {
            // read values
            var fileSize = ControlBlock[ControlBlockFileSizeOffset];
            var descriptorIndex = ControlBlock[ControlBlockDescriptorIndexOffset];
            var descriptorOffset = descriptorIndex * DescriptorBytes;
            var firstBlockAddress = DescriptorMemory[descriptorOffset + DescriptorFirstBlockFieldOffset];
            var oldSize = DescriptorMemory[descriptorOffset + DescriptorFileLengthFieldOffset];
            var sizeDelta = (byte)(fileSize - oldSize);
            // begin critical operations
            MemoryMapMarkUsed((byte)(firstBlockAddress + oldSize), sizeDelta);
            DescriptorMemory[descriptorOffset + DescriptorFileLengthFieldOffset] = fileSize;
            // finish operation
            ControlBlock[ControlBlockFlagOffset] = OkFlag;
        }

        /// <summary>
        /// Marks memory map as used. <paramref name="blockCount"/> blocks starting with 
        /// <paramref name="from block"/> are marked 'used'.
        /// </summary>
        private void MemoryMapMarkUsed(byte fromBlock, byte blockCount)
        {
            if (blockCount == 0) return;
            var memMapOffset = fromBlock >> 3; // divided by 8
            var bitOffset = fromBlock % 8;
            byte mask = 0x0;
            int i = bitOffset;
            while (i < 8)
            {
                mask = (byte)(mask | (0x1 << i));
                if (blockCount == 0)
                {
                    // add mask to map
                    MemoryMap[memMapOffset] = (byte)(MemoryMap[memMapOffset] | mask);
                    return;
                }
                if (i == 7)
                {
                    // add mask to map
                    MemoryMap[memMapOffset] = (byte)(MemoryMap[memMapOffset] | mask);
                    mask = 0x0;
                    ++memMapOffset;
                    i = 0; // plus one on the end of loop
                }
                else
                {
                    ++i;
                }
                --blockCount;
            }
        }

        private void WriteAtomic(byte offset, byte byte1, byte byte2)
        {
            FileMemory[offset] = byte1;
            FileMemory[offset + 1] = byte2;
        }

        private byte FindDescriptor(byte fileName)
        {
            // it must be test file anyway
            return 0;
        }

        private bool CanWrite(byte firstBlockAddress, byte offset, byte bufferLength)
        {
            // test file is the only file, it can alway be written
            return true;
        }
    }
}
