
using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class GenerationSummary
    {
        public FitnessStatistic FitnessStatistic { get; set; }
        public List<GenerationChromosome> TopChromosomes { get; set; }
    }

    internal class FitnessStatistic
    {
        public double Average { get; set; }
        public double Best { get; set; }
        public double StandardDeviation { get; set; }
    }

    internal class GenerationChromosome
    {
        public double? Fitness { get; set; }
        public Statistic? Statistic { get; set; }
        public double[] Genes { get; set; } = Array.Empty<double>();
    }
}