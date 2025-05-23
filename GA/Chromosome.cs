using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class Chromosome
    {
        public double? Fitness { get; set; }
        public Statistic? Statistic { get; set; }

        private double[] genes;

        private readonly int nGenes = 22;
        private readonly double _meanPCV;
        private readonly double _stdPCV;
        private readonly double[] _PCO;

        public Chromosome(double meanPCV, double stdPCV, double[] PCO)
        {
            _meanPCV = meanPCV;
            _stdPCV = stdPCV;
            _PCO = PCO;

            CreateGenes(nGenes);
        }

        public Chromosome CreateNew()
        {
            return new Chromosome(_meanPCV, _stdPCV, _PCO);
        }

        private void CreateGenes(int nGenes)
        {
            genes = new double[nGenes];

            for (int i = 0; i < nGenes; i++)
                ReplaceGene(i, GenerateGene(i));
        }

        private double GenerateGene(int geneIndex)
        {
            if (geneIndex < 3)
            {
                return _PCO[geneIndex];
            }
            else if (geneIndex == 3)
            {
                return 0.0;
            }
            else
            {
                return Randomizer.SampleGaussian(_meanPCV, _stdPCV);
            }
        }

        public void ReplaceGene(int index, double gene)
        {
            if (index < 0 || index >= nGenes)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"There is no Gene on index {index} to be replaced.");
            }

            genes[index] = gene;
            Fitness = null;
        }

        public double GetGene(int index)
        {
            if (index < 0 || index >= nGenes)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"There is no Gene on index {index}.");
            }

            return genes[index];
        }

        public IEnumerable<double> GetGenes()
        {
            return genes;
        }

        public Chromosome Clone()
        {
            var clone = CreateNew();
            var genes = GetGenes();

            for (int i = 0; i < nGenes; i++)
            {
                clone.ReplaceGene(i, genes.ElementAt(i));
            }

            clone.Fitness = Fitness;
            return clone;
        }
    }
}
