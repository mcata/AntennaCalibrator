using AntennaCalibrator.GA;
using AntennaCalibrator.Utilis;

namespace AntennaCalibrator
{
    internal class Calibrator
    {
        static void Main(string[] args)
        {
            var logger = SetupLogging.CreateThreadLogger(@".\", "Calibrator");
            logger.Information($"Antenna Calibrator ({AppVersion.GetApplicationInfo()})");

            // Load the configuration
            var config = ConfigurationLoader.LoadFromFile(@".\ancillary\config\config.xml");
            if (config == null)
            {
                logger.Error("Failed to load configuration file.");
                return;
            }

            var population = new SteadyStatePopulation(config.PopulationSize, logger);
            population.CreateInitialGeneration(config.MeanStdValues.Mean, config.MeanStdValues.Std, config.StartValues.Values.Take(3).ToArray());

            var ga = new GeneticAlgorithm(
                population,
                new Fitness(config, logger),
                new RouletteWheelSelection(),
                new SbxCrossover(config.Crossover.Probability, config.Crossover.DistributionIndex),
                new GaussianMutation(config.Mutation.Probability, config.Mutation.Noise),
                logger
            );

            ga.Run(config.Generation.Number, config.Generation.StagnantNumber);
        }
    }
}
