using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TrionicCANLib.API;

namespace TrionicCANLibTest
{
    [TestClass]
    public class SeedKeyTest
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
    }
}
