using AntennaCalibrator.Utilis;
using Serilog;

namespace AntennaCalibrator.GA
{
    internal class GaussianMutation
    {
        private readonly double _probability; // probabilità di mutazione
        private readonly double _stdDev;      // deviazione standard della gaussiana (in mm)
        private readonly ILogger? _logger;

        public GaussianMutation(double probability, double stdDev = 2.0, ILogger? logger = null)
        {
            _probability = probability;
            _stdDev = stdDev;
            _logger = logger;
        }

        public void PerformMutate(Chromosome chromosome, double? probability = null)
        {
            probability = probability ?? _probability;

            var genes = chromosome.GetGenes().ToList();

            for (int i = 4; i < genes.Count; i++)
            {
                if (Randomizer.NextDouble() <= probability)
                {
                    // perturbazione gaussiana centrata su 0 con deviazione _stdDev
                    double delta = Randomizer.NextSign() * Randomizer.SampleGaussian(0, _stdDev);
                    double newValue = genes[i] + delta;

                    _logger?.Verbose($"\t Mutazione su gene {i}: {genes[i]:F2} + {delta:F2} = {newValue:F2}");

                    chromosome.ReplaceGene(i, newValue);
                }
            }
        }
    }
}
