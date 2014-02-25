using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrionicCANLib
{
    public enum AccessLevel : int
    {
        AccessLevel01,
        AccessLevelFB,
        AccessLevelFD
    }

    public class SeedToKey
    {

        public byte[] calculateKey(byte[] a_seed, AccessLevel level)
        {
            int seed = a_seed[0] << 8 | a_seed[1];
            int key = 0;
            byte[] returnKey = new byte[2];
            key = convertSeed(seed);
            if (level == AccessLevel.AccessLevelFD)
            {
                key /= 3;
                key ^= 0x8749;
                key += 0x0ACF;
                key ^= 0x81BF;
            }
            else if (level == AccessLevel.AccessLevelFB)
            {
                key ^= 0x8749;
                key += 0x06D3;
                key ^= 0xCFDF;
            }
            returnKey[0] = (byte)((key >> 8) & 0xFF);
            returnKey[1] = (byte)(key & 0xFF);
            return returnKey;
        }

        public byte[] calculateKey(byte[] a_seed, bool basicAccess)
        {
            int seed = a_seed[0] << 8 | a_seed[1];
            int key = 0;
            byte[] returnKey = new byte[2];
            key = convertSeed(seed);
            if (!basicAccess)
            {
                // this does not apply to method 1
                key /= 3;
                key ^= 0x8749;
                key += 2767;
                key ^= 0x81BF;
            }
            returnKey[0] = (byte)((key >> 8) & 0xFF);
            returnKey[1] = (byte)(key & 0xFF);
            return returnKey;
        }

        public byte[] calculateKeyForCIM(byte[] a_seed)
        {
            int seed = a_seed[0] << 8 | a_seed[1];
            int key = 0;
            byte[] returnKey = new byte[2];
            key = convertSeedCIM(seed);

            returnKey[0] = (byte)((key >> 8) & 0xFF);
            returnKey[1] = (byte)(key & 0xFF);
            return returnKey;
        }

        private int convertSeed(int seed)
        {
            int key = (seed >> 5) | (seed << 11);
            return (key + 0xB988) & 0xFFFF;
        }

        private int convertSeedCIM(int seed)
        {
            int key = (seed + 0x9130) & 0xFFFF;
            key = (key >> 8) | (key << 8);
            return (0x3FC7 - key) & 0xFFFF;
        }
    }
}
