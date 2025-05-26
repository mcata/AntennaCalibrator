
using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class GenerationSummary
    {
        public int GenerationNumber { get; set; }
        public string TimeElapsed { get; set; }
        public List<GenerationChromosome> TopChromosomes { get; set; }
    }

    internal class GenerationChromosome
    {
        public double? Fitness { get; set; }
        public Statistic? Statistic { get; set; }
        public double[] Genes { get; set; } = Array.Empty<double>();
    }
}