using AntennaCalibrator.Clustering;
using AntennaCalibrator.Utilis;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace AntennaCalibrator.GA
{
    internal class GeneticAlgorithm
    {
        private readonly SteadyStatePopulation _population;
        private readonly Fitness _fitness;
        private readonly RouletteWheelSelection _selection;
        private readonly SbxCrossover _crossover;
        private readonly GaussianMutation _mutation;
        private readonly ILogger? _logger;

        private int _generations { get; set; }

        public GeneticAlgorithm(SteadyStatePopulation population, Fitness fitness, RouletteWheelSelection selection,
                                SbxCrossover crossover, GaussianMutation mutation, ILogger? logger)
        {
            _population = population;
            _fitness = fitness;
            _selection = selection;
            _crossover = crossover;
            _mutation = mutation;
            _logger = logger;
        }

        public async Task Run(int generations, int? stagnantGenerationsNumber = null, List<double>? values = null, string pipeName = "ChromosomePipe")
        {
            _generations = generations;

            var runTimer = new Stopwatch();
            runTimer.Start();

            if (Directory.Exists(@".\temp")) Directory.Delete(@".\temp", true);

            using var pipeService = new PipeSenderService(pipeName);

            for (int i = 0; i < generations; i++)
            {
                var genTimer = new Stopwatch();
                genTimer.Start();

                _logger?.Information($"Evolution generation {i + 1}");

                EvolveOneGeneration();

                if ((i + 1) % stagnantGenerationsNumber == 0)
                {
                    double[] improvedGenes = LocalSearch(_population.BestChromosome, Evaluate);
                    _population.BestChromosome.ReplaceGenes(0, improvedGenes);
                    EvaluateFitness(new ConcurrentBag<Chromosome> { _population.BestChromosome });
                }

                _logger?.Information($"\tFitness of best Chromosome: {_population.BestChromosome.Fitness:F4} ({_population.BestChromosome.Statistic!.ToString()})");
                FileManager.WriteBestChromosomePerGeneration($@".\temp\best_chromosomes.csv", _population.CurrentGeneration.Chromosomes, i + 1);

                genTimer.Stop();
                var timeElapsed = FormatTimeElapsed(genTimer.Elapsed);
                _logger?.Information($"Generation {i + 1} ended. Time: {timeElapsed}");

                var topChromosomes = _population.CurrentGeneration.Chromosomes
                    .OrderByDescending(c => c.Fitness)
                    .Take(5)
                    .ToList();

                var topChromosomesList = topChromosomes
                    .Select(c => new GenerationChromosome
                    {
                        Fitness = c.Fitness,
                        Statistic = c.Statistic,
                        Genes = c.GetGenes().Skip(3).ToArray() // Skip first 3 genes (PCO)
                    })
                    .ToList();


                if (values != null)
                {
                    topChromosomesList.Insert(0, new GenerationChromosome
                    {
                        Fitness = 0,
                        Statistic = null,
                        Genes = values.ToArray().Skip(3).ToArray()
                    });
                }

                var summary = new GenerationSummary
                {
                    FitnessStatistic = CalculateFitnessStatistic(),
                    TopChromosomes = topChromosomesList
                };

                await pipeService.SendAsync(summary);

                if (IsStagnant(_population.BestChromosome, stagnantGenerationsNumber))
                {
                    ReplaceLowFitnessChromosomes();
                }
            }

            runTimer.Stop();
            var totalTime = FormatTimeElapsed(runTimer.Elapsed);
            _logger?.Information($"Genetic Algorithm finished. Total time: {totalTime}");

            var fitnessAcrossGeneration = _population.Generations
                .Select(g => (double)g.Chromosomes.Max(c => c.Fitness)!)
                .ToList();
            File.WriteAllLines(@".\temp\fitness.txt", fitnessAcrossGeneration.Select(f => f.ToString("0.00000")));
            ExternalTools.LaunchGraphMananger(@".\temp\fitness.txt");
        }

        private void EvolveOneGeneration()
        {
            var currentChromosomes = _population.CurrentGeneration.Chromosomes;
            EvaluateFitness(currentChromosomes);

            var parents = SelectParents();
            var offspring = Cross(parents.ToList(), _generations);
            Mutate(offspring, (double)parents.Average(c => c.Fitness)!, _generations);

            var candidateChromosomes = Reinsert([.. offspring]);

            _logger?.Information($"\tPerforming clustering");
            var clusters = KMeansClustering.PerformCluster(candidateChromosomes, k: 5, threshold: 0.001);
            //var algClusters = AlglibClustering.KmeansClustering(candidateChromosomes.ToList(), k: 5, threshold: 0.1);
            var (survivors, nDrops) = RemoveDuplicates(clusters, candidateChromosomes);

            var nextGeneration = new ConcurrentBag<Chromosome>(survivors);
            for (int i = 0; i < nDrops; i++)
            {
                var baseChromosome = _population.CurrentGeneration.Chromosomes.First();
                nextGeneration.Add(baseChromosome.CreateNew());
            }

            var populationDir = @".\temp\population";
            Directory.CreateDirectory(populationDir);
            FileManager.WritePopulation($@"{populationDir}\{_population.CurrentGeneration.Number}.csv", _population.CurrentGeneration.Chromosomes.OrderByDescending(c => c.Fitness));

            _population.EndCurrentGeneration();
            _population.CreateNewGeneration(nextGeneration);
        }

        private void EvaluateFitness(ConcurrentBag<Chromosome> chromosomes)
        {
            var unevalutedChromosomes = chromosomes.Count(c => c.Fitness == null);
            if (unevalutedChromosomes == 0) return;

            int total = chromosomes.Count;
            int completed = 0;
            int lastPercentage = -1;

            _logger?.Information($"\tEvaluating fitness of {unevalutedChromosomes} chromosomes");

            Parallel.ForEach(chromosomes, chromosome =>
            {
                var result = _fitness.Evaluate(chromosome);
                chromosome.Fitness = result.fitness;
                chromosome.Statistic = result.statistic;

                int done = Interlocked.Increment(ref completed);
                int percent = (done * 100) / total;

                if (percent != lastPercentage)
                {
                    lastPercentage = percent;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write($"\r[{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")} (VERB) Calibrator] >   Completed: {done}/{total} - {percent}%");
                }
            });

            Console.WriteLine();
        }

        public double Evaluate(double[] genes)
        {
            var chromosome = new Chromosome(genes);
            var (fitness, _) = _fitness.Evaluate(chromosome);
            return fitness;
        }

        private IEnumerable<Chromosome> SelectParents()
        {
            _logger?.Information($"\tSelecting parents");
            var chromosomes = _population.CurrentGeneration.Chromosomes.ToList();
            return new List<Chromosome>
            {
                _selection.PerformSelection(chromosomes),
                _selection.PerformSelection(chromosomes)
            };
        }

        private IEnumerable<Chromosome> Cross(IList<Chromosome> parents, int generations, double? distributionIndex = null)
        {
            #region Adaptive probability
            const double pcLow = 0.6;
            const double pcHigh = 0.9;

            var chromosomes = _population.CurrentGeneration.Chromosomes;

            double f = parents.Max(c => c.Fitness) ?? 0;
            double fMax = chromosomes.Max(c => c.Fitness) ?? 0;
            double fAvg = chromosomes.Average(c => c.Fitness) ?? 0;

            double pcTemp = pcLow;

            if (Math.Abs(fMax - fAvg) > double.Epsilon)
            {
                pcTemp = f >= fAvg
                    ? Math.Clamp((pcHigh * (fMax - f)) / (fMax - fAvg), pcLow, pcHigh)
                    : pcHigh;
            }

            double probability = pcTemp * Math.Exp(_population.CurrentGeneration.Number / generations);
            #endregion

            #region Adaptive η
            const double ηMin = 2.0;
            const double ηMax = 20.0;

            distributionIndex = ηMin + ((ηMax - ηMin) * _population.CurrentGeneration.Number / (double)generations);
            #endregion

            _logger?.Information($"\tPerforming crossover (p = {probability:F2}; eta = {distributionIndex:F1})");
            _logger?.Verbose($"\t Parents fitness: p1: {parents[0].Fitness:F4}, p2: {parents[1].Fitness:F4}");
            return _crossover.PerformCross(parents, probability, distributionIndex);
        }

        private void Mutate(IEnumerable<Chromosome> chromosomes, double parentsAvgFitness, int generations)
        {
            const double pmLow = 0.01;
            const double pmHigh = 0.1;

            var _chromosomes = _population.CurrentGeneration.Chromosomes;

            double f = parentsAvgFitness;
            double fMax = _chromosomes.Max(c => c.Fitness) ?? 0;
            double fAvg = _chromosomes.Average(c => c.Fitness) ?? 0;

            double pmTemp = pmLow;

            if (Math.Abs(fMax - fAvg) > double.Epsilon)
            {
                pmTemp = f >= fAvg
                    ? Math.Clamp((pmLow + (pmHigh - pmLow) * (fMax - f)) / (fMax - fAvg), pmLow, pmHigh)
                    : pmHigh;
            }

            double probability = pmTemp * Math.Exp(_population.CurrentGeneration.Number / generations);

            _logger?.Information($"\tPerforming mutation (p = {probability.ToString("0.00")})");
            foreach (var chromosome in chromosomes)
            {
                _mutation.PerformMutate(chromosome, probability);
            }
        }

        private ConcurrentBag<Chromosome> Reinsert(ConcurrentBag<Chromosome> offspring)
        {
            _logger?.Information("\tReinserting offspring");

            EvaluateFitness(offspring);

            var currentChromosomes = _population.CurrentGeneration.Chromosomes.ToList();
            var fAvg = currentChromosomes.Average(c => c.Fitness)!;
            var fMin = currentChromosomes.Min(c => c.Fitness)!;

            var acceptedOffspring = new List<Chromosome>();

            foreach (var child in offspring)
            {
                _logger?.Verbose($"\t Child fitness: {child.Fitness:F4}");
                if ((double)child.Fitness! > fAvg || (double)child.Fitness! > fMin)
                {
                    acceptedOffspring.Add(child);
                }
            }

            // Unisco alla popolazione attuale i figli accettati
            var combined = currentChromosomes
                .Concat(acceptedOffspring)
                .OrderByDescending(c => c.Fitness)
                .Take(currentChromosomes.Count);

            return new ConcurrentBag<Chromosome>(combined);
        }

        private bool IsStagnant(Chromosome bestChromosome, int? stagnantGenerationsNumber)
        {
            if (stagnantGenerationsNumber == null)
            {
                return false;
            }

            var generations = _population.Generations.ToList();
            if (generations.Count < stagnantGenerationsNumber + 1)
            {
                return false;
            }

            var lastGenerations = generations.Skip(generations.Count - stagnantGenerationsNumber.Value - 1).Take(stagnantGenerationsNumber.Value);
            return lastGenerations.All(g => g.BestChromosome.Fitness == bestChromosome.Fitness);
        }

        private void ReplaceLowFitnessChromosomes()
        {
            _logger?.Warning("Stagnation detected. Replacing chromosomes with fitness < (mean - sigma)");

            var chromosomes = _population.CurrentGeneration.Chromosomes.ToList();
            var fitnessValues = chromosomes.Select(c => c.Fitness ?? 0).ToList();

            double mean = fitnessValues.Average();
            double stdDev = Math.Sqrt(fitnessValues.Average(v => Math.Pow(v - mean, 2)));
            double threshold = mean - stdDev;

            _logger?.Verbose($"\tFitness mean: {mean:F4}, std: {stdDev:F4}, threshold: {threshold:F4}");

            // Crea lista aggiornata con rimozione dei peggiori
            var survivors = chromosomes.Where(c => (c.Fitness ?? 0) >= threshold).ToList();
            int replacementsNeeded = _population.Size - survivors.Count;
            _logger?.Verbose($"\tReplacing {replacementsNeeded} chromosomes");

            var baseChromosome = survivors.FirstOrDefault() ?? chromosomes.OrderByDescending(c => c.Fitness ?? 0).First();
            var newIndividuals = Enumerable
                .Range(0, replacementsNeeded)
                .Select(_ => baseChromosome.CreateNew());

            var newGeneration = survivors.Concat(newIndividuals);
            _population.CurrentGeneration.Chromosomes = new ConcurrentBag<Chromosome>(newGeneration);
        }

        private (IEnumerable<Chromosome> survivors, int nDrops) RemoveDuplicates(IEnumerable<IEnumerable<Cluster>> clusters, ConcurrentBag<Chromosome> chromosomes)
        {
            var nDrops = 0;
            var population = chromosomes.ToList();

            foreach (var cluster in clusters)
            {
                var bestFitness = chromosomes.Max(c => c.Fitness);

                foreach (var item in cluster)
                {
                    var individues = population
                                     .Where(x => (x.Id == item.Chromosome.Id) && (item.Fitness != bestFitness))
                                     .ToList();

                    foreach (var individue in individues)
                    {
                        population.Remove(individue);
                        nDrops++;
                    }
                }
            }

            if (nDrops > 0)
                _logger?.Warning($"\tRemoved {nDrops} similar chromosomes from population");

            return (population, nDrops);
        }

        private FitnessStatistic CalculateFitnessStatistic()
        {
            var chromosomes = _population.CurrentGeneration.Chromosomes.ToList();
            var fitnessValues = chromosomes.Select(c => c.Fitness ?? 0).ToList();

            double mean = fitnessValues.Average();
            double stdDev = Math.Sqrt(fitnessValues.Average(v => Math.Pow(v - mean, 2)));

            return new FitnessStatistic
            {
                Average = mean,
                Best = fitnessValues.Max(),
                StandardDeviation = stdDev
            };
        }

        double[] LocalSearch(Chromosome chromosome, Func<double[], double> fitnessFunc, double step = 0.01, int maxIterations = 5)
        {
            // Coordinate descent local search:
            // algoritmo che ottimizza una sola variabile per volta, mantenendo tutte le altre fisse.

            double[] genes = chromosome.GetGenes().ToArray();
            double[] bestGenes = (double[])chromosome.GetGenes().ToArray().Clone();
            double bestFitness = (double)chromosome.Fitness!;
            _logger?.Information($"[Coordinate descent] Initial fitness: {bestFitness:F4}");

            for (int iter = 0; iter < maxIterations; iter++)
            {
                bool improved = false;
                _logger?.Verbose($"\tIteration {iter + 1}");

                for (int i = 4; i < bestGenes.Length; i++)
                {
                    foreach (var direction in new[] { +1.0, -1.0 })
                    {
                        double[] candidate = (double[])bestGenes.Clone();
                        candidate[i] += direction * step;

                        double candidateFitness = fitnessFunc(candidate);
                        _logger?.Verbose($"\t [Gene {i}] Direction {(direction > 0 ? "+" : "-")}");

                        const double MinImprovement = 1e-4;
                        if (candidateFitness - bestFitness > MinImprovement)
                        {
                            bestGenes = candidate;
                            bestFitness = candidateFitness;
                            improved = true;
                            _logger?.Verbose($"\t   Improvement found! New fitness: {bestFitness:F4}");
                            break; // passa al prossimo gene
                        }
                    }
                }

                if (!improved)
                {
                    _logger?.Verbose("\tNo improvements found, end local search");
                    break;
                }
            }

            _logger?.Verbose($"\tEnd - Improvement: {bestFitness - chromosome.Fitness:F4}");
            return bestGenes;
        }

        private string FormatTimeElapsed(TimeSpan timeElapsed)
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", timeElapsed.Hours, timeElapsed.Minutes, timeElapsed.Seconds);
        }
    }
}
