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

        public double Evaluate(Chromosome chromosome)
        {
            if (chromosome.Fitness != null)
            {
                return chromosome.Fitness.Value;
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

                var residues = FileManager.ReadResiduesFromFile($"{outputPath}.stat");

                double avgSquaredResidual = residues
                    .Where(r => r.Q == 2)
                    .Select(r => r.Value * r.Value)
                    .DefaultIfEmpty(1e6) // fallback per evitare DivisionByZero/NaN
                    .Average();

                double fitness = 1.0 / (avgSquaredResidual + _epsilon);

                return fitness;
            }
            catch (Exception e)
            {
                _logger?.Error($"Error during fitness evaluation: {e.Message}");
                return 0.0;
            }
            finally
            {
                if (Directory.Exists(tempDir))
                {
                    Directory.Delete(tempDir, true);
                }
            }
        }
    }
}
