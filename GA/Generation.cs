using System.Collections.Concurrent;

namespace AntennaCalibrator.GA
{
    internal class Generation
    {
        public int Number { get; private set; }
        public DateTime CreationDate { get; private set; }
        public ConcurrentBag<Chromosome> Chromosomes { get; set; } = [];
        public Chromosome BestChromosome { get; private set; }

        public Generation(int number, ConcurrentBag<Chromosome> chromosomes)
        {
            Number = number;
            CreationDate = DateTime.Now;
            Chromosomes = chromosomes;
        }

        public Generation(int number, Chromosome bestChromosome)
        {
            Number = number;
            CreationDate = DateTime.Now;
            BestChromosome = bestChromosome;
        }

        public void End()
        {
            Chromosomes = new ConcurrentBag<Chromosome>(Chromosomes);
            BestChromosome = Chromosomes.OrderByDescending(c => c.Fitness!).First();
        }
    }
}
