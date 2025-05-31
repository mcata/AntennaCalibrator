using AntennaCalibrator.Utilis;
using Serilog;

namespace AntennaCalibrator.GA
{
    internal class Fitness
    {
        private readonly Configuration _configuration;
        private readonly ILogger? _logger;

        private const double _epsilon = 1e-6;

        public Fitness(Configuration configuration, ILogger? logger = null)
        {
            _configuration = configuration;
            _logger = logger;
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
                // Preparazione file di configurazione
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
                    configPath,
                    _configuration.RoverRinex.Files.First(),
                    _configuration.ReferenceRinex.Files.First(),
                    _configuration.NavigationRinex.Files.First(),
                    _configuration.Sp3File.Files.First(),
                    outputPath);

                var residuals = FileManager.ReadResidualsFromFile($"{outputPath}.stat");
                var coordinates = FileManager.ReadCoordinatesFromFile($"{outputPath}");
                var statistic = EvaluateCoordinates(coordinates);

                var validResiduals = residuals.Where(r => r.ValidDataFlag == 1 && r.Q == 2);
                double sumOfWeightedSquares = validResiduals.Sum(r =>
                {
                    double weight = Math.Pow(Math.Sin(r.Elevation * Math.PI / 180.0), 2); // peso = sin²(e)
                    return weight * Math.Pow(r.Value, 2);
                });

                double totalWeight = validResiduals.Sum(r =>
                    Math.Pow(Math.Sin(r.Elevation * Math.PI / 180.0), 2));

                double weightedRMSE = Math.Sqrt(sumOfWeightedSquares / totalWeight);

                double fitness = 1.0 / (weightedRMSE + _epsilon);

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
            var filtered = coordinates.Where(p => p.Q == 2).ToList();
            if (!filtered.Any())
            {
                _logger?.Warning("\tNo valid positions with Q == 2");
                return null;
            }

            var xp = filtered.Select(p => p.X).ToList();
            var yp = filtered.Select(p => p.Y).ToList();
            var zp = filtered.Select(p => p.Z).ToList();

            double avgX = xp.Average();
            double avgY = yp.Average();
            double avgZ = zp.Average();

            double stdX = Math.Sqrt(xp.Average(x => Math.Pow(x - avgX, 2)));
            double stdY = Math.Sqrt(yp.Average(y => Math.Pow(y - avgY, 2)));
            double stdZ = Math.Sqrt(zp.Average(z => Math.Pow(z - avgZ, 2)));

            return new Statistic()
            {
                Average = new double[3] { avgX, avgY, avgZ },
                StandardDev = new double[3] { stdX, stdY, stdZ }
            };
        }

    }
}
