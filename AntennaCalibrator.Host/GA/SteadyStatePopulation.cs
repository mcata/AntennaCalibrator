using Serilog;
using System.Collections.Concurrent;

namespace AntennaCalibrator.GA
{
    internal class SteadyStatePopulation
    {
        private readonly ILogger? _logger;

        public int Size { get; }
        public Generation CurrentGeneration { get; private set; }
        public List<Generation> Generations { get; private set; } = [];
        public int GenerationNumber { get; private set; } = 0;
        public Chromosome BestChromosome { get; private set; }

        public SteadyStatePopulation(int size, ILogger? logger = null)
        {
            Size = size;
            _logger = logger;

            if (Size < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(size), "Population size must be greater than 0.");
            }
        }

        public void CreateInitialGeneration(double meanPCV, double stdPCV, double[] PCO, bool addRealPcv = false)
        {
            var chromosomes = new ConcurrentBag<Chromosome>();
            for (int i = 0; i < Size; i++)
            {
                var chromosome = new Chromosome(meanPCV, stdPCV, PCO);
                chromosomes.Add(chromosome);
            }

            if (addRealPcv)
            {
                chromosomes.First().ReplaceGenes(0, PCO);
            }

            CreateNewGeneration(chromosomes);
        }

        public void CreateNewGeneration(ConcurrentBag<Chromosome> chromosomes)
        {
            CurrentGeneration = new Generation(++GenerationNumber, chromosomes);
            Generations.Add(CurrentGeneration);
        }

        public void EndCurrentGeneration()
        {
            CurrentGeneration.End();

            if (BestChromosome == null || CurrentGeneration.BestChromosome.Fitness > BestChromosome.Fitness)
            {
                BestChromosome = CurrentGeneration.BestChromosome;
            }
        }
    }
}
