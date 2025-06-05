using AntennaCalibrator.GA;
using AntennaCalibrator.Utilis;
using System.Diagnostics;

namespace AntennaCalibrator
{
    internal class Calibrator
    {
        static async Task Main(string[] args)
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

            var pipeName = Guid.NewGuid().ToString("N");
            LaunchUI(pipeName);

            var population = new SteadyStatePopulation(config.PopulationSize, logger);
            population.CreateInitialGeneration(config.MeanStdValues.Mean, config.MeanStdValues.Std, config.StartValues.Values.ToArray(), true);
            //population.CreateInitialGeneration(config.MeanStdValues.Mean, config.MeanStdValues.Std, config.StartValues.Values.Take(3).ToArray());

            var ga = new GeneticAlgorithm(
                population,
                new Fitness(config, logger),
                new RouletteWheelSelection(),
                new SbxCrossover(config.Crossover.Probability, config.Crossover.DistributionIndex),
                new GaussianMutation(config.Mutation.Probability, config.Mutation.Noise),
                logger
            );

            await ga.Run(config.Generation.Number, config.Generation.StagnantNumber, config.StartValues.Values, pipeName);
        }

        private static void LaunchUI(string args)
        {
            var uiPath = @".\Ancillary\sw\UI\AntennaCalibrator.View.exe";

            if (!File.Exists(uiPath)) return;

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = uiPath,
                Arguments = args,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
}
