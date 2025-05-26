using AntennaCalibrator.GA;

namespace AntennaCalibrator.Clustering
{
    internal class AlglibClustering
    {
        public static IEnumerable<IEnumerable<Cluster>> KmeansClustering(IList<Chromosome> population, int k, double threshold)
        {
            double[,] data = new double[population.Count, 22];

            for (int i = 0; i < population.Count; i++)
            {
                var genes = population[i].GetGenes();

                for (int j = 0; j < 22; j++)
                    data[i, j] = genes.ElementAt(j);
            }

            alglib.clusterizerstate s;
            alglib.kmeansreport krep;

            alglib.clusterizercreate(out s);
            alglib.clusterizersetpoints(s, data, 2);

            // Configurazione algoritmo kmeans++
            alglib.clusterizersetkmeansinit(s, 2);
            // Configutazione numero di riavvii e massimo numero di iterazioni
            alglib.clusterizersetkmeanslimits(s, 10, 300);
            alglib.clusterizerrunkmeans(s, k, out krep);

            var cidx = krep.cidx;
            var cObj = new List<Cluster>();

            for (int i = 0; i < cidx.Length; i++)
                cObj.Add(new Cluster
                {
                    Id = cidx[i],
                    Fitness = population[i].Fitness,
                    Chromosome = population[i]
                });

            var _ = cObj.GroupBy(x => x.Id)
                .Select(g => g.ToList())
                .ToList();

            return cObj
                .GroupBy(x => x.Id)
                .Select(g => GetCluster(g, threshold))
                .Where(x => x.Count() >= 5)
                .ToList();
        }

        //mcata: calcolare la distanza tra l'elemento con la fitness minore e gli altri dello stesso cluster
        //       solo per i cluster con # elementi > 5
        //       soglia: distanza < 0.1 x 12 geni
        private static List<Cluster> GetCluster(IEnumerable<Cluster> cluster, double threshold)
        {
            var clusterObj = new List<Cluster>();
            var bestChromosome = cluster
                                 .Where(x => x.Fitness == cluster.Max(x => x.Fitness))
                                 .First();
            var clusterElements = cluster
                                  .Where(x => x != bestChromosome)
                                  .ToList();

            for (int i = 1; i < clusterElements.Count; i++)
            {
                var distance = Distance(bestChromosome.Chromosome.GetGenes().ToArray(), clusterElements[i].Chromosome.GetGenes().ToArray());
                var nGenes = distance.Count(x => x < threshold);
                if (nGenes > 13)
                    clusterObj.Add(clusterElements[i]);
            }

            return clusterObj;
        }

        private static double[] Distance(double[] a, double[] b)
        {
            double[] distance = new double[a.Length];

            for (int i = 0; i < a.Length; i++)
                distance[i] = Math.Abs(a[i] - b[i]);

            return distance;
        }
    }
}
