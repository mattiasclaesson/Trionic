using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TrionicCANLib.SeedKey
{
    public class Algorithm
    {
        Step[] steps;

        public Algorithm(Step step0, Step step1, Step step2, Step step3)
        {
            steps = new Step[4];
            steps[0] = step0;
            steps[1] = step1;
            steps[2] = step2;
            steps[3] = step3;
        }

        public UInt16 SeedToKey(UInt16 seed)
        {
            UInt16 key = seed;

            foreach (Step step in steps)
            {
                // Perform the operation on the seed value.
                key = (UInt16)step.Operation(key);
            }

            return key;
        }
    }
}
