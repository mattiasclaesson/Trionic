using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace TrionicCANLib
{
    public class BlockManager
    {
        private uint checksum32 = 0;
        private int _blockNumber = 0;

        // Legion mod
        private long Len = 0;



        private string _filename = string.Empty;
        byte[] filebytes = null;

        public bool SetFilename(string filename)
        {
            if (File.Exists(filename))
            {
                FileInfo fi = new FileInfo(filename);
                // Legion mod
                // if (fi.Length == 0x100000) 
                if (fi.Length == 0x100000 || fi.Length == 0x40100)
                {
                    Len = fi.Length; 
                    _filename = filename;
                    filebytes = File.ReadAllBytes(_filename);
                    return true;
                }
            }
            return false;
        }

        public byte[] GetNextBlock()
        {
            byte[] returnarray = GetCurrentBlock();
            _blockNumber++;
            return returnarray;
            
        }

        public byte[] GetCurrentBlock()
        {
            // get 0xEA bytes from the current file but return 0xEE bytes (7 * 34 = 238 = 0xEE bytes)
            // if the blocknumber is 0xF50 return less bytes because that is the last block
            int address = 0x020000 + _blockNumber * 0xEA;
            ByteCoder bc = new ByteCoder();
            if (_blockNumber == 0xF50)
            {
                byte[] array = new byte[0xE0];
                bc.ResetCounter();
                for (int byteCount = 0; byteCount < 0xE0; byteCount++)
                {
                    array[byteCount] = bc.codeByte(filebytes[address++]);
                }
                return array;
            }
            else
            {
                byte[] array = new byte[0xEE];
                bc.ResetCounter();
                for (int byteCount = 0; byteCount < 0xEA; byteCount++)
                {
                    array[byteCount] = bc.codeByte(filebytes[address++]);
                }
                return array;
            }
        }

        // Determine last part of the FLASH chip that is used (to save time when reading (DUMPing))
        // Address 0x020140 stores a pointer to the BIN file Header which is the last used area in FLASH
        public int GetLastBlockNumber()
        {
            int lastAddress = (int)filebytes[0x020141] << 16 | (int)filebytes[0x020142] << 8 | (int)filebytes[0x020143];
            // Add another 512 bytes to include header region (with margin)!!!
            lastAddress += 0x200;
            return (lastAddress - 0x020000) / 0xEA;
        }

        // Determine last address for md5-verification after flashing Trionic 8; main
        public uint GetLasAddress()
        {
            // Add another 512 bytes to include header region (with margin)!!!
            // Note; Legion is hardcoded to also add 512 bytes. -Do not change!
            return (uint)((int)filebytes[0x020141] << 16 | (int)filebytes[0x020142] << 8 | (int)filebytes[0x020143]) + 0x200;
        }

        // Legion hacks
        public bool mcpswapped()
        {
            if (filebytes[0] == 0x08 && filebytes[1] == 0x00 & filebytes[2] == 0x00 & filebytes[3] == 0x20)
                return true;

            return false;
        }
        public byte[] GetCurrentBlock_128(int block, bool byteswapped)
        {
            _blockNumber = block;
            int address = _blockNumber * 0x80;


            byte[] buffer = new byte[0x80];
            byte[] array = new byte[0x88];

            
            if (byteswapped)
            {
                for (int byteCount = 0; byteCount < 0x80; byteCount+=2)
                {
                    buffer[byteCount + 1] = filebytes[address];
                    buffer[byteCount] = filebytes[address + 1];
                    address += 2;
                }
            }
            else
            {
                for (int byteCount = 0; byteCount < 0x80; byteCount++)
                {
                    buffer[byteCount] = filebytes[address++];
                }
            }

            ByteCoder bc = new ByteCoder();
            bc.ResetCounter();

            for (int byteCount = 0; byteCount < 0x80; byteCount++)
            {
                array[byteCount] = bc.codeByte(buffer[byteCount]);
            }

            return array;
        }

        // Check if a whole block will be filled with 0xFF (So that it can be skipped)
        public bool FFblock(int address, int size)
        {
            int count = 0;

            for (int byteCount = 0; byteCount < size; byteCount++)
            {
                if (address == Len)
                    break;
                if (filebytes[address] == 0xFF)
                    count++;

                address++;
            }

            if (count == size)
                return true;

            return false;
        }

        // Generate checksum-32 of file
        public uint GetChecksum32()
        {
            checksum32 = 0;
            long i;

            for (i = 0; i < Len; i++)
                checksum32 += (byte)filebytes[i];

            return checksum32;
        }

        // Partition table of Trionic 8; Main
        private uint[] T8parts = 
        {
            0x000000, // Boot (0)
            0x004000, // NVDM
            0x006000, // NVDM
            0x008000, // HWIO
            0x020000, // APP
            0x040000, // APP
            0x060000, // APP
            0x080000, // APP
            0x0C0000, // APP
            0x100000, // End (9)

            0x000000, // Special cases for range-md5 instead of partitions
            0x004000,
            0x020000
        };

        // Generate md5 of selected partition/region
        public byte[] GetPartitionmd5(uint device, uint partition)
        {
            uint start = 0, e = 0, end = 0x40100;
            byte[] hash = new byte[16];
            bool byteswapped = false;
            
            // Trionic 8, Main
            if (partition < 13 && device == 6)
            {
                if (partition > 0)
                {
                    start = (partition < 10) ? T8parts[partition - 1] : T8parts[partition];
                    end   = (partition < 10) ? T8parts[partition    ] : GetLasAddress();
                }
                else
                    end = T8parts[9];
            }
            // Trionic 8; MCP
            else if (partition < 10 && device == 5)
            {
                byteswapped = mcpswapped();
                if (partition > 0)
                {
                    end   = (partition == 9) ? (uint)0x40100 : (partition << 15);
                    start = (partition == 9) ? (uint)0x40000 : (end - 0x8000);
                }
            }
            else
                return hash;

            byte[] buf = new byte[end - start];

            System.Security.Cryptography.MD5CryptoServiceProvider md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
            md5.Initialize();

            if (!byteswapped)
            {
                for (uint i = start; i < end; i++)
                    buf[e++] = filebytes[i];
            }
            else
            {
                for (uint i = start; i < end; i+=2)
                {
                    buf[e++] = filebytes[i + 1];
                    buf[e++] = filebytes[i];
                }
            }

            return md5.ComputeHash(buf);

        }


    }
}
