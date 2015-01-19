using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CapsuleFileSystem
{
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

        public void CheckAndRepair()
        {
            switch (ControlBlock[ControlBlockFlagOffset])
            {
                case OkFlag:
                    return;
                case SaveFlag:
                    CriticalWrite();
                    break;
                default:
                    break;
            }
        }

        public void Write(byte fileName, byte offset, byte[] buffer)
        {
            ControlBlock[ControlBlockFlagOffset] = OkFlag;
            var descriptorIndex = FindDescriptor(fileName);
            var descriptorAddress = descriptorIndex * DescriptorBytes;
            var firstBlockAddress = DescriptorMemory[descriptorAddress + DescriptorFirstBlockFieldOffset];
            if (!CanWrite(firstBlockAddress, offset, (byte)buffer.Length)) throw new OutOfMemoryException();
            // commence buffer copying
            var firstByteToWriteAddress = (byte)(firstBlockAddress + offset);
            var lastByteToWrite = (byte)(firstByteToWriteAddress + buffer.Length);
            byte i;
            for (i = firstByteToWriteAddress; i < lastByteToWrite; i += 2)
            {
                WriteAtomic(i, buffer[i], buffer[i + 1]);
            }
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

        private bool CanWrite(byte firstBlockAddress, byte offset, byte bufferLength)
        {
            var memMapByteOffset = firstBlockAddress >> 3; // dziele na 8
            var memMapBitIndex = firstBlockAddress % 8;
            var memMapByte = MemoryMap[memMapByteOffset];
            // TODO ?
            return true;
        }

        private void CriticalWrite()
        {
            var fileSize = ControlBlock[ControlBlockFileSizeOffset];
            var descriptorIndex = ControlBlock[ControlBlockDescriptorIndexOffset];
            var firstBlockAddress = DescriptorMemory[descriptorIndex * DescriptorBytes + DescriptorFirstBlockFieldOffset];
            var oldSize = DescriptorMemory[descriptorIndex * DescriptorBytes + DescriptorFileLengthFieldOffset];
            var sizeDelta = (byte)(fileSize - oldSize);
            MemoryMapMarkUsed((byte) (firstBlockAddress + oldSize), sizeDelta);
        }

        /// <summary>
        /// Marks memory map as used for provided address through count.
        /// </summary>
        private void MemoryMapMarkUsed(byte from, byte count)
        {
            // mark memory map
        }

        private byte FindDescriptor(byte fileName)
        {
            return 0;
        }

        private void WriteAtomic(byte offset, byte byte1, byte byte2)
        {
            FileMemory[offset] = byte1;
            FileMemory[offset + 1] = byte2;
        }
    }
}
