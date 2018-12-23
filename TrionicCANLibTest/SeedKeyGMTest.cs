using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrionicCANLib.API;
using TrionicCANLib.SeedKey;

namespace TrionicCANLibTest
{
    [TestClass]
    public class SeedKeyGMTest
    {
        [TestMethod]
        public void TestT8KeySeedUsingByteArray()
        {
            // Given
            // T8 SecurityLevel = AccessLevel.AccessLevel01
            // seed 5f94 [0 high][1 low]
            // key 5c84 [0 high][1 low]
            byte[] seed = { 0x5F, 0x94 };
            byte[] expectedKey = { 0x5C, 0x84 };

            // When
            byte[] actualKey = SeedKeyGM.CalculateKey(ECU.TRIONIC8, seed);

            // Then
            CollectionAssert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestT8KeySeedUsingUInt16()
        {
            // Given
            // T8 SecurityLevel = AccessLevel.AccessLevel01
            // seed 5f94 [0 high][1 low]
            // key 5c84 [0 high][1 low]
            UInt16 seed = 0x5F94;
            UInt16 expectedKey = 0x5C84;

            // When
            UInt16 actualKey = SeedKeyGM.CalculateKey(ECU.TRIONIC8, seed);

            // Then
            Assert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestME96KeySeedUsingByteArray()
        {
            // Given
            // ME96
            // seed C72B [0 high][1 low]
            // key 2C46 [0 high][1 low]
            byte[] seed = { 0xC7, 0x2B };
            byte[] expectedKey = { 0x2C, 0x46 };

            // When
            byte[] actualKey = SeedKeyGM.CalculateKey(ECU.MOTRONIC96, seed);

            // Then
            CollectionAssert.AreEqual(expectedKey, actualKey);
        }

        [TestMethod]
        public void TestEDC16C39KeySeedUsingByteArray()
        {
            // Given
            //EDC16C39
            // seed 6808 [0 high][1 low]
            // key 405F [0 high][1 low]
            byte[] seed = { 0x68, 0x08 };
            byte[] expectedKey = { 0x40, 0x5F };

            // When
            byte[] actualKey = SeedKeyGM.CalculateKey(ECU.EDC16C39, seed);

            // Then
            CollectionAssert.AreEqual(expectedKey, actualKey);
        }
    }
}
