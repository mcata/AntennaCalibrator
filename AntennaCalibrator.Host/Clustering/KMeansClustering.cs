using Accord.MachineLearning;
using AntennaCalibrator.GA;

namespace AntennaCalibrator.Clustering
{
    internal class KMeansClustering
    {
        public static IEnumerable<IEnumerable<Cluster>> PerformCluster(IEnumerable<Chromosome> population, int k, double threshold)
        {
            double[][] data = population
                .Select(c => c.GetGenes().ToArray())
                .ToArray();

            var kmeans = new KMeans(k)
            {
                Tolerance = 1e-4,
                MaxIterations = 300
            };

            var clusters = kmeans.Learn(data);
            int[] labels = clusters.Decide(data);

            var cObj = new List<Cluster>();
            for (int i = 0; i < labels.Length; i++)
            {
                cObj.Add(new Cluster
                {
                    Id = labels[i],
                    Fitness = population.ElementAt(i).Fitness,
                    Chromosome = population.ElementAt(i)
                });
            }

            var _ = cObj.GroupBy(x => x.Id)
                .Select(g => g.ToList())
                .ToList();

            return cObj
                .GroupBy(x => x.Id)
                .Select(g => GetCluster(g, threshold))
                //.Where(x => x.Count() >= 5)
                .ToList();
        }

        private static IEnumerable<Cluster> GetCluster(IEnumerable<Cluster> cluster, double threshold)
        {
            var best = cluster.OrderByDescending(x => x.Fitness).First();
            var result = new List<Cluster>();

            var bestGenes = best.Chromosome.GetGenes();

            foreach (var item in cluster)
            {
                if (item == best) continue;

                var count = bestGenes
                    .Zip(item.Chromosome.GetGenes(), (a, b) => Math.Abs(a - b))
                    .Count(d => d < threshold);

                if (count > 13)
                    result.Add(item);
            }

            return result;
        }

    }
}
