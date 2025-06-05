using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class Chromosome
    {
        public Guid Id { get; }
        public double? Fitness { get; set; }
        public Statistic? Statistic { get; set; }

        private double[] _genes;

        private readonly int nGenes = 22;
        private readonly double _meanPCV;
        private readonly double _stdPCV;
        private readonly double[] _PCO;

        public Chromosome(double meanPCV, double stdPCV, double[] PCO)
        {
            _meanPCV = meanPCV;
            _stdPCV = stdPCV;
            _PCO = PCO;

            Id = Guid.NewGuid();

            CreateGenes(nGenes);
        }

        public Chromosome(double[] genes)
        {
            _genes = (double[])genes.Clone();
            Fitness = null;
            Id = Guid.NewGuid();
        }

        public Chromosome CreateNew()
        {
            return new Chromosome(_meanPCV, _stdPCV, _PCO);
        }

        private void CreateGenes(int nGenes)
        {
            _genes = new double[nGenes];

            for (int i = 0; i < nGenes; i++)
                ReplaceGene(i, GenerateConstrainedGene(i));
        }

        private double GenerateConstrainedGene(int geneIndex)
        {
            if (geneIndex < 3)
                return _PCO[geneIndex];

            if (geneIndex == 3)
                return 0.0;

            double previousGene = _genes[geneIndex - 1];
            double newGeneCandidate;

            // Genera un nuovo gene che rispetti il vincolo |gene_i - gene_{i-1}| <= 1.5
            do
            {
                newGeneCandidate = Math.Round(Randomizer.SampleGaussian(_meanPCV, _stdPCV), 3);
            } 
            while (Math.Abs(newGeneCandidate - previousGene) > 1.5) ;

            return newGeneCandidate;
        }

        private double GenerateGene(int geneIndex)
        {
            if (geneIndex < 3)
                return _PCO[geneIndex];

            if (geneIndex == 3)
                return 0.0;

            var value = Randomizer.SampleGaussian(_meanPCV, _stdPCV);
            return Math.Round(value, 3);

        }

        public void ReplaceGene(int index, double gene)
        {
            if (index < 0 || index >= nGenes)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"There is no Gene on index {index} to be replaced.");
            }

            _genes[index] = Math.Round(gene, 3);
            Fitness = null;
        }

        public void ReplaceGenes(int startIndex, double[] genes)
        {
            if (genes.Length > 0)
            {
                if (startIndex < 0 || startIndex >= nGenes)
                {
                    throw new ArgumentOutOfRangeException(nameof(startIndex), $"There is no Gene on index {startIndex} to be replaced.");
                }

                Array.Copy(genes, 0, _genes, startIndex, Math.Min(genes.Length, nGenes - startIndex));

                Fitness = null;
            }
        }

        public double GetGene(int index)
        {
            if (index < 0 || index >= nGenes)
            {
                throw new ArgumentOutOfRangeException(nameof(index), $"There is no Gene on index {index}.");
            }

            return _genes[index];
        }

        public IEnumerable<double> GetGenes()
        {
            return _genes;
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
