using NLog;
using System;
using TrionicCANLib.API;

namespace TrionicCANLib.SeedKey
{
    public class SeedKeyGM
    {
        static private Logger logger = LogManager.GetCurrentClassLogger();

        public static byte[] CalculateKey(ECU ecu, byte[] seed)
        {
            UInt16 combinedSeed = (UInt16)(seed[0] << 8 | seed[1]);

            UInt16 key = CalculateKey(ecu, combinedSeed);

            byte[] returnKey = new byte[2];
            returnKey[0] = (byte)((key >> 8) & 0xFF);
            returnKey[1] = (byte)(key & 0xFF);
            return returnKey;
        }

        public static UInt16 CalculateKey(ECU ecu, UInt16 seed)
        {
            Algorithm algorithm;
            AlgorithmDictionary.TryGetValue(ecu, out algorithm);
            UInt16 key = algorithm.SeedToKey(seed);
            return key;
        }
    }
}