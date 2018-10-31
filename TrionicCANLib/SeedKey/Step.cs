using System;

using NLog;


namespace TrionicCANLib.SeedKey
{
    public class Step
    {
        private Logger logger = LogManager.GetCurrentClassLogger();

        private byte operation;
        private byte parameter0;
        private byte parameter1;

        public Step(byte operation, byte parameter0, byte parameter1)
        {
            this.operation = operation;
            this.parameter0 = parameter0;
            this.parameter1 = parameter1;
        }

        private static uint RotateLeft(uint value, int count)
        {
            return (value << count) | (value >> (16 - count));
        }

        private static uint RotateRight(uint value, int count)
        {
            return (value >> count) | (value << (16 - count));
        }

        private static uint Subtract(uint key, byte parameter0, byte parameter1)
        {
            return key - (UInt16)(parameter1 + (UInt16)((parameter0) << 8));
        }

        public uint Operation(uint key)
        {
            logger.Debug(string.Format("Key {0} Operation {1} Param0 {2} Param1 {3}", key, operation, parameter0, parameter1));
            switch (operation)
            {
                case 5:
                    return RotateLeft(key, 8);
                case 0x14:
                    return key + (UInt16)(parameter1 + (UInt16)((parameter0) << 8));
                case 0x2A:
                    if (parameter0 < parameter1)
                        return ~key + 1;
                    else
                        return ~key;
                case 0x37:
                    return key & (UInt16)(parameter0 + (UInt16)((parameter1) << 8));
                case 0x4C:
                    return RotateLeft(key, parameter0);
                case 0x52:
                    return key | (UInt16)(parameter0 + (UInt16)((parameter1) << 8));
                case 0x6B:
                    return RotateRight(key, parameter1);
                case 0x75:
                    return key + (UInt16)(parameter0 + (UInt16)((parameter1) << 8));
                case 0x7E:
                    if (parameter0 >= parameter1)
                        return RotateLeft(key, 8) + parameter1 + (UInt16)(parameter0 << 8);
                    else
                        return RotateLeft(key, 8) + parameter0 + (UInt16)(parameter1 << 8);
                case 0x98:
                    return Subtract(key, parameter0, parameter1);
                case 0xF8:
                    return key - (UInt16)(parameter0 + (UInt16)((parameter1) << 8));
                default:
                    break;
            }

            return key;
        }
    }
}
