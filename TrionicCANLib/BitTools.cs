using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrionicCANLib
{
    internal class BitTools
    {
        /// <summary>
        /// Reverses the order of bytes in the input
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static ulong ReverseOrder(ulong input)
        {
            ulong output = 0;
            output |= (input << 56) & 0xFFUL << 56;
            output |= (input << 40) & 0xFFUL << 48;
            output |= (input << 24) & 0xFFUL << 40;
            output |= (input << 8) & 0xFFUL << 32;
            output |= (input >> 8) & 0xFFUL << 24;
            output |= (input >> 24) & 0xFFUL << 16;
            output |= (input >> 40) & 0xFFUL << 8;
            output |= (input >> 56) & 0xFFUL;

            return output;
        }

        /// <summary>
        /// Gets size of can data (in correct order) i.e. 0x20900200000000
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static int GetDataSize(ulong input)
        {
            int size = 8;
            ulong mask = 0xFF;
            while ((input & mask) == 0)
            {
                size--;
                mask <<= 8;
            }
            return size;
        }

        /// <summary>
        /// Shifts the input, so it can be easily formatted for ELM interface
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        internal static ulong GetShiftedData(ulong input)
        {
            int count = 8;
            while ((input & 0xFF) == 0 && count > 0)
            {
                input >>= 8;
                count--;
            }
            return input;
        }
    }
}
