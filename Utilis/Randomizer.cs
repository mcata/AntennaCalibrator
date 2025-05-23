namespace AntennaCalibrator.Utilis
{
    internal class Randomizer
    {
        protected static Random Random = new Random();

        public static double NextDouble(double min, double max)
        {
            return Random.NextDouble() * (max - min) + min;
        }

        public static double NextDouble()
        {
            return Random.NextDouble();
        }

        public static int NextInt(int maximum)
        {
            return Random.Next(maximum);
        }

        public static int NextSign()
        {
            var retVal = Random.Next(-1, 1);
            return retVal == 0 ? 1 : retVal;
        }

        public static List<int> NextInt(int n, int maximum)
        {
            var retval = new List<int>();

            while (retval.Count < n)
            {
                var value = Random.Next(maximum);
                if (!retval.Contains(value))
                    retval.Add(value);
            }

            return retval;
        }

        public static List<int> NextInt(int n, int min, int max)
        {
            var retval = new List<int>();

            while (retval.Count < n)
            {
                var value = (int)Math.Ceiling(NextDouble(min, max));
                if (!retval.Contains(value))
                    retval.Add(value);
            }

            return retval;
        }

        public static double SampleGaussian(double mean, double stddev)
        {
            // Box-Muller transform per generare numeri casuali da una distribuzione gaussiana
            double u1 = 1.0 - Random.NextDouble();
            double u2 = 1.0 - Random.NextDouble();

            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);

            return mean + stddev * randStdNormal;
        }
    }
}
