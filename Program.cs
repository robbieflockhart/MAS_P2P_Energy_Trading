using System;
using ActressMas;

namespace MASCWK
{
    class Program
    {
        static void Main(string[] args)
        {
            var env = new EnvironmentMas();

            var environmentAgent = new EnvironmentAgent();
            env.Add(environmentAgent, "environmentAgent");

            int noOfHouseholds = 5;

            for (int i = 0; i < noOfHouseholds; i++)
            {
                var householdAgent = new HouseholdAgent();
                env.Add(householdAgent, $"householdAgent{i:D2}");
            }
            env.Start();
            Console.ReadLine();
        }
    }
}
