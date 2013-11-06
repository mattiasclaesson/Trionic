using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TrionicCANLib
{
    public class BlockManager
    {
        private int _blockNumber = 0;

        private string _filename = string.Empty;
        byte[] filebytes = null;

        public bool SetFilename(string filename)
        {
            if (File.Exists(filename))
            {
                FileInfo fi = new FileInfo(filename);
                if (fi.Length == 0x100000)
                {
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
    }
}
