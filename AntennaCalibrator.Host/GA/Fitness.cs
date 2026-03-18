using AntennaCalibrator.Utilis;
using Serilog;

namespace AntennaCalibrator.GA
{
    /// <summary>
    /// Classe per la valutazione della fitness di un cromosoma.
    /// La fitness combina RMSE dei residui su bande di elevazione e deviazione standard delle coordinate.
    /// Entrambi i componenti sono normalizzati per garantire una scala omogenea.
    /// </summary>
    internal class Fitness
    {
        private readonly Configuration _configuration;
        private readonly ILogger? _logger;

        private const double _epsilon = 1e-6;

        // Pesi configurabili per la combinazione dei componenti della fitness
        // Default: equal weighting (0.5, 0.5) come raccomandato nella letteratura NSGA-II
        public double WeightResiduals { get; set; } = 0.5;
        public double WeightCoordinates { get; set; } = 0.5;

        // Valori di riferimento per la normalizzazione (stimati dalla letteratura GNSS)
        // RMSE tipico per antenne ben calibrate: ~1-5mm
        // StdDev tipica per coordinate GPS: ~10-50mm
        private const double ReferenceRMSE = 5.0;      // mm
        private const double ReferenceStdDev = 50.0;   // mm (somma in quadratura XYZ)

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

        // Baseline fitness calcolata sui valori iniziali forniti (PCO/PCV di riferimento)
        private double _baselineFitness = 0.0;

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

                // Calcola RMSE medio su tutte le bande di elevazione (in mm)
                double meanRMSE = rmseList.Count > 0 ? rmseList.Average() : double.MaxValue;

                // Calcola deviazione standard combinata delle coordinate (in mm)
                // Usa la norma euclidea delle deviazioni standard XYZ
                double stdX = statistic.StandardDev[0];
                double stdY = statistic.StandardDev[1];
                double stdZ = statistic.StandardDev[2];
                double combinedStdDev = Math.Sqrt(stdX * stdX + stdY * stdY + stdZ * stdZ);

                // Normalizzazione dei componenti rispetto ai valori di riferimento
                // Formula: normalized = reference / (measured + epsilon)
                // Questo porta entrambi i componenti in una scala comparabile [0, ~1]
                // dove valori più alti indicano migliore performance
                double normalizedRMSE = ReferenceRMSE / (meanRMSE + _epsilon);
                double normalizedStdDev = ReferenceStdDev / (combinedStdDev + _epsilon);

                // Combinazione pesata dei componenti normalizzati
                // I pesi sono configurabili (default 0.5, 0.5 per equal weighting)
                double combinedFitness = WeightResiduals * normalizedRMSE +
                                         WeightCoordinates * normalizedStdDev;

                // Normalizza i pesi se la loro somma non è 1.0
                double weightSum = WeightResiduals + WeightCoordinates;
                if (Math.Abs(weightSum - 1.0) > _epsilon)
                {
                    combinedFitness /= weightSum;
                }

                // Scala la fitness per avere valori più leggibili (moltiplica per 100)
                double fitness = combinedFitness * 100.0;

                // Sottrai la baseline per mostrare il miglioramento rispetto alla configurazione iniziale
                // Se la fitness > baseline, abbiamo migliorato
                double relativeFitness = fitness - _baselineFitness;

                _logger?.Verbose($"Fitness components: RMSE={meanRMSE:F3}mm (norm={normalizedRMSE:F3}), " +
                               $"StdDev={combinedStdDev:F3}mm (norm={normalizedStdDev:F3}), " +
                               $"Combined={fitness:F3}, Relative={relativeFitness:F3}");

                return (relativeFitness, statistic);
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

        /// <summary>
        /// Calcola il valore di fitness di riferimento (baseline) usando i valori PCV/PCO iniziali.
        /// Questo permette di normalizzare la fitness e valutare il miglioramento rispetto alla configurazione iniziale.
        /// </summary>
        private void CalculateFactor(double[] pco)
        {
            // Usa i valori PCV iniziali dalla configurazione (se forniti)
            // Altrimenti usa valori zero come baseline neutra
            var pcv = _configuration.StartValues.Values.Skip(3).Take(19).ToList();

            // Se non ci sono abbastanza valori PCV, usa zero (baseline neutra)
            while (pcv.Count < 19)
            {
                pcv.Add(0.0);
            }

            var genes = pco.Concat(pcv).ToArray();
            var chromosome = new Chromosome(genes);
            var result = Evaluate(chromosome);
            _baselineFitness = result.fitness;

            _logger?.Information($"Baseline fitness calculated: {_baselineFitness:F4} (using reference PCV/PCO values)");
        }
    }
}
