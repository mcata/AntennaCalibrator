using AntennaCalibrator.GA;
using System.Globalization;
using System.Text;

namespace AntennaCalibrator.Utilis
{
    internal class FileManager
    {
        public static void UpdateConfigFile(string path, string configPath, string[] refpos, string rovAnt, string refAnt, string erpFile)
        {
            var content = File.ReadAllLines(path).ToList();

            var idx = content.IndexOf(content.First(x => x.Contains("ant2-pos1")));
            if (idx == -1) throw new InvalidOperationException("ant2-pos1 not found in config file.");

            for (int i = 0; i < refpos.Length; i++)
            {
                var line = content[idx + i];
                var sIdx = line.IndexOf('=') + 1;
                var eIdx = line.IndexOf('#');

                content[idx + i] = line[..sIdx] + refpos[i] + " " + line[eIdx..];
            }

            ReplaceFileInFile(rovAnt, "ant1-anttype", ref content);
            ReplaceFileInFile(refAnt, "ant2-anttype", ref content);
            ReplaceFileInFile(erpFile, "file-eopfile", ref content);
            ReplaceFileInFile(Path.Combine(configPath, "satellites.atx"), "file-satantfile", ref content);
            ReplaceFileInFile(Path.Combine(configPath, "igs14.atx"), "file-rcvantfile", ref content);

            File.WriteAllLines(path, content);
        }

        public static List<Residue> ReadResidualsFromFile(string path)
        {
            const int delayMs = 2500;

            while (IsFileLocked(path))
            {
                Thread.Sleep(delayMs);
            }

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                using (var reader = new StreamReader(stream))
                {
                    var residues = new List<Residue>();
                    string? line;

                    while ((line = reader.ReadLine()) != null)
                    {
                        if (line.Contains("$SAT"))
                        {
                            var tokens = line.Split(',', StringSplitOptions.RemoveEmptyEntries);
                            if (tokens.Length >= 12)
                            {
                                residues.Add(new Residue
                                {
                                    Elevation = double.Parse(tokens[6], CultureInfo.InvariantCulture),
                                    Value = double.Parse(tokens[8], CultureInfo.InvariantCulture),
                                    Q = int.Parse(tokens[11])
                                });
                            }
                        }
                    }

                    return residues;
                }
            }

            throw new IOException($"Impossibile leggere il file '{path}'.");
        }

        public static List<Coordinate> ReadCoordinatesFromFile(string path)
        {
            const int delayMs = 2500;

            while (IsFileLocked(path))
            {
                Thread.Sleep(delayMs);
            }

            using (var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(stream))
            {
                var coordinates = new List<Coordinate>();
                string? line;
                bool startReading = false;

                while ((line = reader.ReadLine()) != null)
                {
                    if (!startReading)
                    {
                        if (!line.Contains("%"))
                            startReading = true;

                        continue;
                    }

                    var tokens = line.Split(" ", StringSplitOptions.RemoveEmptyEntries);
                    if (tokens.Length >= 6)
                    {
                        coordinates.Add(new Coordinate
                        {
                            X = double.Parse(tokens[2], CultureInfo.InvariantCulture),
                            Y = double.Parse(tokens[3], CultureInfo.InvariantCulture),
                            Z = double.Parse(tokens[4], CultureInfo.InvariantCulture),
                            Q = int.Parse(tokens[5])
                        });
                    }
                }

                return coordinates;
            }

            throw new IOException($"Impossibile leggere il file '{path}'.");
        }

        public static void WriteAntexFile(string path, string roverAnt, Chromosome chromosome)
        {
            var content = File.ReadAllLines(path).ToList();

            int startIdx = content.FindIndex(line => line.Contains(roverAnt)) - 1;
            if (startIdx >= 0)
            {
                int endIdx = content.FindIndex(startIdx, line => line.Contains("END OF ANTENNA"));
                if (endIdx >= 0)
                {
                    content.RemoveRange(startIdx, endIdx - startIdx + 1);
                }
            }

            var sb = new StringBuilder();
            var dt = DateTime.Now.ToString("dd-MMM-yy", CultureInfo.CreateSpecificCulture("en-US"));

            sb.AppendLine("".PadLeft(76) + "START OF ANTENNA");
            sb.AppendLine($"{roverAnt,-16}{"NONE",-44}{"TYPE / SERIAL NO",-20}");
            sb.AppendLine($"{"GA",-20}{"YetItMoves",-25}{"5",-5}{dt,-10}{"METH / BY / # / DATE",-20}");
            sb.AppendLine($"{"5.0",8}{"DAZ1",56}");
            sb.AppendLine($"{"0.0",8}{"90.0",6}{"5.0",6}{"ZEN1 / ZEN2 / DZEN",58}");
            sb.AppendLine($"{"1",6}{"# OF FREQUENCIES",70}");
            sb.AppendLine($"{"IGS14_0000",-11}{"SINEX CODE",71}");
            sb.AppendLine($"{"G01",6}{"START OF FREQUENCY",72}");

            var genes = chromosome.GetGenes();
            var pco = genes.Take(3).Select(g => FormatGene(g));
            sb.AppendLine($"{string.Join("", pco.Select(v => v.PadLeft(10)))}{"NORTH / EAST / UP",47}");

            var pcv = genes.Skip(3).Take(19).Select(g => FormatGene(g)).ToArray();
            sb.AppendLine($"{"NOAZI",8}{string.Join("", pcv.Select(v => v.PadLeft(8)))}");

            sb.AppendLine($"{"G01",6}{"END OF FREQUENCY",70}");
            sb.Append("".PadLeft(74) + "END OF ANTENNA");

            content.Add(sb.ToString());
            File.WriteAllLines(path, content);
        }

        public static void WritePopulation(string path, IEnumerable<Chromosome> population, char separator = ';')
        {
            var sb = new StringBuilder();
            var culture = CultureInfo.InvariantCulture;

            // Intestazione CSV
            var header = new List<string> { "Index", "Fitness" };
            header.AddRange(Enumerable.Range(0, 22).Select(i => $"Gene{i + 1}"));
            sb.AppendLine(string.Join(separator, header));

            // Dati
            for (int i = 0; i < population.Count(); i++)
            {
                var chromosome = population.ElementAt(i);

                var row = new List<string>
                {
                    i.ToString(),
                    ((double)chromosome.Fitness!).ToString("0.000000", culture)
                };

                row.AddRange(chromosome.GetGenes().Select(g =>
                {
                    var val = g;
                    var str = val.ToString("0.000", culture);
                    return val > 0 ? "+" + str : str;
                }));

                sb.AppendLine(string.Join(separator, row));
            }

            File.WriteAllText(path, sb.ToString());
        }

        public static void WriteBestChromosomePerGeneration(string path, IEnumerable<Chromosome> population, int generationNumber, char separator = ';')
        {
            var culture = CultureInfo.InvariantCulture;

            var best = population
                .Where(c => c.Fitness.HasValue)
                .OrderByDescending(c => c.Fitness)
                .First();

            var fitnessValues = population.Select(c => c.Fitness ?? 0).ToList();
            double mean = fitnessValues.Average();
            double sigma = Math.Sqrt(fitnessValues.Average(f => Math.Pow(f - mean, 2)));

            // Prepara l'intestazione se il file non esiste
            bool fileExists = File.Exists(path);
            var sb = new StringBuilder();

            if (!fileExists)
            {
                var header = new List<string> { "Generation", "Fitness", "Mean", "Sigma" };
                header.AddRange(Enumerable.Range(0, 22).Select(i => $"Gene{i + 1}"));
                sb.AppendLine(string.Join(separator, header));
            }

            var row = new List<string>
            {
                generationNumber.ToString(),
                ((double)best.Fitness!).ToString("0.000000", culture),
                mean.ToString("0.000000", culture),
                sigma.ToString("0.000000", culture)
            };

            row.AddRange(best.GetGenes().Select(g =>
            {
                var str = g.ToString("0.000", culture);
                return g > 0 ? "+" + str : str;
            }));

            sb.AppendLine(string.Join(separator, row));
            File.AppendAllText(path, sb.ToString());
        }


        private static void ReplaceFileInFile(string text, string pattern, ref List<string> content)
        {
            var idx = content.IndexOf(content.First(x => x.Contains(pattern)));
            var sInd = content[idx].IndexOf('=') + 1;
            var txtReplace = content[idx].Substring(sInd);

            if (txtReplace.Length > 0)
                content[idx] = content[idx].Replace(txtReplace, text);
            else
                content[idx] = content[idx].Insert(sInd, text);
        }

        private static string FormatGene(double value)
        {
            var str = value.ToString("0.00", CultureInfo.InvariantCulture);
            return value > 0 ? "+" + str : str;
        }

        public static bool IsFileLocked(string path)
        {
            try
            {
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);
                return false; // accesso esclusivo ottenuto => non è bloccato
            }
            catch (IOException)
            {
                return true; // il file è bloccato (o non accessibile in modo esclusivo)
            }
        }

    }
}
