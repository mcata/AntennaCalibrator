using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class SbxCrossover
    {
        /// <summary>
        /// Probabilità di applicare il crossover su una coppia di cromosomi.
        /// </summary>
        public double Probability { get; }

        /// <summary>
        /// Indice di distribuzione (ηₙ). Più alto → figli più vicini ai genitori.
        /// </summary>
        public double DistributionIndex { get; }

        public SbxCrossover(double probability = 0.75, double distributionIndex = 20.0)
        {
            Probability = probability;
            DistributionIndex = distributionIndex;
        }

        public IEnumerable<Chromosome> PerformCross(IList<Chromosome> parents, double? probability = null, double? distributionIndex = null)
        {
            var _probability = probability == null ? Probability : probability;
            var _distributionIndex = distributionIndex == null ? DistributionIndex : (double)distributionIndex;

            var p1 = parents[0];
            var p2 = parents[1];

            var c1 = p1.CreateNew();
            var c2 = p2.CreateNew();

            for (int i = 4; i < p1.GetGenes().Count(); i++)
            {
                var g1 = p1.GetGene(i);
                var g2 = p2.GetGene(i);

                double offspring1 = g1;
                double offspring2 = g2;

                if (Randomizer.NextDouble() <= _probability)
                {
                    // calcola β_q
                    double u = Randomizer.NextDouble();
                    double beta;
                    if (u <= 0.5)
                    {
                        beta = Math.Pow(2.0 * u, 1.0 / (_distributionIndex + 1.0));
                    }
                    else
                    {
                        beta = Math.Pow(1.0 / (2.0 * (1.0 - u)), 1.0 / (_distributionIndex + 1.0));
                    }

                    // genera i due figli
                    offspring1 = 0.5 * ((1 + beta) * g1 + (1 - beta) * g2);
                    offspring2 = 0.5 * ((1 - beta) * g1 + (1 + beta) * g2);
                }

                // sostituisci i geni nei cromosomi figli
                c1.ReplaceGene(i, offspring1);
                c2.ReplaceGene(i, offspring2);
            }

            return new List<Chromosome> { c1, c2 };
        }
    }
}
