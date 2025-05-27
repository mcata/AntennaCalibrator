namespace AntennaCalibrator.View.Shared.Model
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

    public class GenerationChromosome
    {
        public double? Fitness { get; set; }
        public Statistic? Statistic { get; set; }
        public double[] Genes { get; set; } = Array.Empty<double>();
    }

    public class Statistic()
    {
        public double[] Average { get; set; } = [];
        public double[] StandardDev { get; set; } = [];

        public string ToString(bool writeCoordinate = false)
        {
            return writeCoordinate
                ? $"Coordinate: avg = [{Average[0]:F4}, {Average[1]:F4}, {Average[2]:F4}]; std = [{StandardDev[0]:F4}, {StandardDev[1]:F4}, {StandardDev[1]:F4}]"
                : $"Coordinate: std = [{StandardDev[0]:F4}, {StandardDev[1]:F4}, {StandardDev[1]:F4}]";
        }
    }
}
