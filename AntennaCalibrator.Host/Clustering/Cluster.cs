using AntennaCalibrator.GA;

namespace AntennaCalibrator.Clustering
{
    internal class Cluster
    {
        public int Id { get; set; }
        public double? Fitness { get; set; }
        public Chromosome Chromosome { get; set; }
    }
}
