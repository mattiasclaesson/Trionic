using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TrionicCANLib.API;

namespace TrionicCANLib.SeedKey
{
    public static class AlgorithmDictionary
    {
        private static Dictionary<ECU, Algorithm> algorithms = new Dictionary<ECU, Algorithm>
        {
            {ECU.TRIONIC8, new Algorithm(new Step(0x6B, 0x65, 0x07), new Step(0x4C, 0x0A, 0x77), new Step(0x7E, 0xF8, 0xDA), new Step(0x98, 0x3F, 0x52))}, // 39 000002E5 EC 6B 65 07 4C 0A 77 7E F8 DA 98 3F 52 .ke.L.w~...?R
            {ECU.MOTRONIC96, new Algorithm(new Step(0x98, 0x38, 0x08), new Step(0x7E, 0xF2, 0x94), new Step(0x6B, 0xE0, 0x02), new Step(0x4C, 0x03, 0x48))}, // 0B 0000008F 3D 98 38 08 7E F2 94 6B E0 02 4C 03 48 =.8.~..k..L.H
            {ECU.MOTRONIC961, new Algorithm(new Step(0xF8, 0x1F, 0x80), new Step(0x05, 0x31, 0x6B), new Step(0x2A, 0x03, 0x4D), new Step(0x75, 0x68, 0x15))},
            {ECU.EDC16C39, new Algorithm(new Step(0x6B, 0x7A, 0x04), new Step(0x7E, 0x82, 0x74), new Step(0x4C, 0x05,0x43), new Step(0x05, 0x1B, 0x9D))},
            {ECU.EDC17C19, new Algorithm(new Step(0x75, 0x50, 0xB0), new Step(0x6B, 0x2C, 0x01), new Step(0x05, 0xC3, 0x42), new Step(0x14, 0x40, 0x93))}
        };

        public static bool TryGetValue(ECU ecu, out Algorithm result)
        {
            return (algorithms.TryGetValue(ecu, out result));
        }
    }
}