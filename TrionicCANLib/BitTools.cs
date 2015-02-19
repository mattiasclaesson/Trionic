using System;
using System.Collections.Generic;
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

        internal static ulong GetUlong(byte[] arr, int startIndex, int count)
        {
            ulong result = 0x00;
            for (int i = startIndex+count-1; i >= startIndex; i--)
            {
                result <<= 8;
                result += arr[i];
            }
            return result;
        }

        internal static ulong GetFrameBytes(int frameNo, byte[] array, int startIndex)
        {
            var res = GetUlong(array, startIndex, 7);
            res = res << 8 | (ulong)(frameNo & 0xFF);
            return res;
        }

        internal static bool GetBit(byte b, int pos)
        {
            return (b & (1 << pos)) != 0;
        }

        internal static byte SetBit(byte b, int pos, bool value)
        {
            byte mask = (byte)(1 << pos);
            return (byte)(value ? (b | mask) : (b & ~mask));
        }

    }
}
