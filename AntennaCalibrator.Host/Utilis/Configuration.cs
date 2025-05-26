using System.Xml.Serialization;

namespace AntennaCalibrator.Utilis
{
    [XmlRoot("Configuration")]
    public class Configuration
    {
        [XmlElement("Generation")]
        public Generation Generation { get; set; }

        [XmlElement("PopulationSize")]
        public int PopulationSize { get; set; }

        [XmlElement("Crossover")]
        public Crossover Crossover { get; set; }

        [XmlElement("Mutation")]
        public Mutation Mutation { get; set; }

        [XmlElement("RoverRinex")]
        public FileContainer RoverRinex { get; set; }

        [XmlElement("ReferenceRinex")]
        public FileContainer ReferenceRinex { get; set; }

        [XmlElement("NavigationRinex")]
        public FileContainer NavigationRinex { get; set; }

        [XmlElement("Sp3File")]
        public FileContainer Sp3File { get; set; }

        [XmlElement("ErpFile")]
        public FileContainer ErpFile { get; set; }

        [XmlElement("ReferencePosition")]
        public Position ReferencePosition { get; set; }

        [XmlElement("RoverAntenna")]
        public string RoverAntenna { get; set; }

        [XmlElement("ReferenceAntenna")]
        public string ReferenceAntenna { get; set; }

        [XmlElement("MeanStdValues")]
        public MeanStdValues MeanStdValues { get; set; }

        [XmlElement("StartValues")]
        public StartValues StartValues { get; set; }
    }

    public class Generation
    {
        [XmlElement("Number")]
        public int Number { get; set; }

        [XmlElement("StagnantNumber")]
        public int StagnantNumber { get; set; }
    }

    public class Crossover
    {
        [XmlElement("Probability")]
        public double Probability { get; set; }

        [XmlElement("DistributionIndex")]
        public double DistributionIndex { get; set; }
    }

    public class Mutation
    {
        [XmlElement("Probability")]
        public double Probability { get; set; }

        [XmlElement("Noise")]
        public double Noise { get; set; }
    }

    public class FileContainer
    {
        [XmlElement("File")]
        public List<string> Files { get; set; }
    }

    public class Position
    {
        [XmlElement("X")]
        public string X { get; set; }

        [XmlElement("Y")]
        public string Y { get; set; }

        [XmlElement("Z")]
        public string Z { get; set; }

        public string[] GetPosition()
        {
            return new string[] { X, Y, Z };
        }
    }

    public class MeanStdValues
    {
        [XmlElement("Mean")]
        public double Mean { get; set; }

        [XmlElement("Std")]
        public double Std { get; set; }
    }

    public class StartValues
    {
        [XmlElement("Value")]
        public List<double> Values { get; set; }
    }

    // Classe di utilità per deserializzare il file XML
    public static class ConfigurationLoader
    {
        public static Configuration? LoadFromFile(string filePath)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                using (var reader = new System.IO.StreamReader(filePath))
                {
                    return (Configuration)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                // Gestione appropriata dell'errore
                Console.WriteLine($"Errore durante la deserializzazione: {ex.Message}");
                return null;
            }
        }

        public static Configuration? LoadFromString(string xmlContent)
        {
            try
            {
                XmlSerializer serializer = new XmlSerializer(typeof(Configuration));
                using (var reader = new System.IO.StringReader(xmlContent))
                {
                    return (Configuration)serializer.Deserialize(reader);
                }
            }
            catch (Exception ex)
            {
                // Gestione appropriata dell'errore
                Console.WriteLine($"Errore durante la deserializzazione: {ex.Message}");
                return null;
            }
        }
    }
}
