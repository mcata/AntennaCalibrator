using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class RouletteWheelSelection
    {
        public Chromosome PerformSelection(IList<Chromosome> population)
        {
            double totalFitness = (double)population.Sum(c => c.Fitness)!;

            if (totalFitness == 0)
            {
                // Tutti hanno fitness zero → selezione casuale
                Random fallbackRandom = new Random();
                return population[fallbackRandom.Next(population.Count())];
            }

            // Genera un numero casuale nell'intervallo [0, totalFitness]
            double pick = Randomizer.NextDouble(0, totalFitness);

            double cumulative = 0.0;
            foreach (var individual in population)
            {
                cumulative += (double)individual.Fitness!;
                if (pick <= cumulative)
                {
                    return individual;
                }
            }

            // Per sicurezza, ritorna l'ultimo individuo
            return population[^1];
        }
    }
}
