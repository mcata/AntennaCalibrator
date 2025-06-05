using AntennaCalibrator.Utilis;

namespace AntennaCalibrator.GA
{
    internal class RouletteWheelSelection
    {
        public Chromosome PerformSelection(IList<Chromosome> population)
        {
            var fitnessValues = population.Select(c => c.Fitness!.Value).ToList();
            double minFitness = fitnessValues.Min();
            double maxFitness = fitnessValues.Max();

            double fitnessRange = maxFitness - minFitness;
            if (fitnessRange == 0)
            {
                // Tutti hanno fitness uguale → selezione casuale
                Random fallbackRandom = new Random();
                return population[fallbackRandom.Next(population.Count)];
            }

            // Normalizza la fitness tra 0 e 1
            var normalizedFitnesses = fitnessValues
                .Select(f => (f - minFitness) / fitnessRange)
                .ToList();

            // Somma totale della fitness normalizzata
            double totalNormalizedFitness = normalizedFitnesses.Sum();

            // Numero casuale nell'intervallo [0, totalNormalizedFitness]
            double pick = Randomizer.NextDouble(0, totalNormalizedFitness);

            double cumulative = 0.0;
            for (int i = 0; i < population.Count; i++)
            {
                cumulative += normalizedFitnesses[i];
                if (pick <= cumulative)
                {
                    return population[i];
                }
            }

            // Per sicurezza, ritorna l'ultimo individuo
            return population[^1];
        }
    }
}
