using AntennaCalibrator.Utilis;
using Serilog;

namespace AntennaCalibrator.GA
{
    internal class Fitness
    {
        private readonly Configuration _configuration;
        private readonly ILogger? _logger;

        private const double _epsilon = 1e-6;

        private readonly List<(double min, double max)> _elevationBands = new List<(double min, double max)>
        {
            (0, 2.5),
            (2.5, 7.5),
            (7.6, 12.5),
            (12.6, 17.5),
            (17.6, 22.5),
            (22.6, 27.5),
            (27.6, 32.5),
            (32.6, 37.5),
            (37.6, 42.5),
            (42.6, 47.5),
            (47.6, 52.5),
            (52.6, 57.5),
            (57.6, 62.5),
            (62.6, 67.5),
            (67.6, 72.5),
            (72.6, 77.5),
            (77.6, 82.5),
            (82.6, 87.5),
            (87.6, 90.0)
        };

        private double _factor = 0.0;

        public Fitness(Configuration configuration, ILogger? logger = null)
        {
            _configuration = configuration;
            _logger = logger;

            CalculateFactor(_configuration.StartValues.Values.Take(3).ToArray());
        }

        public (double fitness, Statistic? statistic) Evaluate(Chromosome chromosome)
        {
            if (chromosome.Fitness != null)
            {
                return (chromosome.Fitness.Value, chromosome.Statistic);
            }

            var tempDir = Path.Combine(".\\temp", Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempDir);

            try
            {
                var configSource = @".\ancillary\config";
                var configPath = Path.Combine(tempDir, "rnx2rtkp.conf");
                var antexPath = Path.Combine(tempDir, "satellites.atx");
                var igsFilePath = Path.Combine(tempDir, "igs14.atx");
                var outputPath = Path.Combine(tempDir, $"{_configuration.RoverAntenna}.out");

                File.Copy(Path.Combine(configSource, "rnx2rtkp.conf"), configPath);
                File.Copy(Path.Combine(configSource, "satellites.atx"), antexPath);
                File.Copy(Path.Combine(configSource, "igs14.atx"), igsFilePath);

                FileManager.WriteAntexFile(igsFilePath, _configuration.RoverAntenna, chromosome);

                FileManager.UpdateConfigFile(configPath, tempDir,
                    _configuration.ReferencePosition.GetPosition(),
                    _configuration.RoverAntenna,
                    _configuration.ReferenceAntenna,
                    _configuration.ErpFile.Files.First());

                ExternalTools.ExecuteRTKLIB(
                    Path.GetFullPath(configPath),
                    _configuration.RoverRinex.Files.First(),
                    _configuration.ReferenceRinex.Files.First(),
                    _configuration.NavigationRinex.Files.First(),
                    _configuration.Sp3File.Files.First(),
                    outputPath);

                var residuals = FileManager.ReadResidualsFromFile($"{outputPath}.stat");
                var coordinates = FileManager.ReadCoordinatesFromFile($"{outputPath}");
                var statistic = EvaluateCoordinates(coordinates);

                var validResiduals = residuals
                    .Where(r => r.ValidDataFlag == 1 && r.Q == 2)
                    .OrderBy(r => r.Elevation);

                var rmseList = new List<double>();

                foreach (var band in _elevationBands)
                {
                    var bandResiduals = validResiduals
                        .Where(r => r.Elevation >= band.min && r.Elevation <= band.max)
                        .Select(r => r.Value)
                        .ToList();

                    if (bandResiduals.Count > 0)
                    {
                        double mse = bandResiduals.Sum(r => r * r) / bandResiduals.Count;
                        rmseList.Add(Math.Sqrt(mse));
                    }
                    else { rmseList.Add(0); }
                }

                double meanRMSE = rmseList.Count > 0 ? rmseList.Average() : double.MaxValue;
                double std = Math.Sqrt(statistic.StandardDev.Sum(v => v * v));

                double fitnessRes = 1.0 / (meanRMSE + _epsilon);
                double fitnessStd = 1.0 / (std + _epsilon) * 100.0;

                double fitness = 0.2 * fitnessRes + 0.8 * fitnessStd;

                var scaledFitness = fitness - _factor;
                fitness = scaledFitness;

                return (fitness, statistic);
            }
            catch (Exception e)
            {
                _logger?.Error($"Error during fitness evaluation: {e.Message}");
                return (0.0, null);
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }

        private Statistic? EvaluateCoordinates(List<Coordinate> coordinates)
        {
            var filtered = coordinates.Where(p => p.Q == 1).ToList();
            if (!filtered.Any())
            {
                _logger?.Warning("\tNo valid positions with Q == 1");
                return null;
            }

            var xp = filtered.Select(p => p.X).ToList();
            var yp = filtered.Select(p => p.Y).ToList();
            var zp = filtered.Select(p => p.Z).ToList();

            double avgX = xp.Average();
            double avgY = yp.Average();
            double avgZ = zp.Average();

            double stdX = Math.Sqrt(xp.Average(x => Math.Pow(x - avgX, 2))) * 1000;
            double stdY = Math.Sqrt(yp.Average(y => Math.Pow(y - avgY, 2))) * 1000;
            double stdZ = Math.Sqrt(zp.Average(z => Math.Pow(z - avgZ, 2))) * 1000;

            return new Statistic()
            {
                Average = new double[3] { avgX, avgY, avgZ },
                StandardDev = new double[3] { stdX, stdY, stdZ }
            };
        }

        private void CalculateFactor(double[] pco)
        {
            var pcv = new List<double>();
            for (int i = 0; i < 19; i++)
            {
                double value = Randomizer.NextDouble(0, 5);
                pcv.Add(50 * value);
            }

            var genes = pco.Concat(pcv).ToArray();
            var chromosome = new Chromosome(genes);
            var result = Evaluate(chromosome);
            _factor = result.fitness;
        }
    }
}
