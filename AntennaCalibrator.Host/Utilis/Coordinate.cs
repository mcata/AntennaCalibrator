namespace AntennaCalibrator.Utilis
{
    public class Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public int Q { get; set; }
    }

    public class Statistic()
    {
        public double[] Average { get; set; } = [];
        public double[] StandardDev { get; set; } = [];

        public string ToString(bool writeCoordinate = false)
        {
            return writeCoordinate
                ? $"coordinate: avg = [{Average[0]:F4}, {Average[1]:F4}, {Average[2]:F4}]; std = [{StandardDev[0]:F4}, {StandardDev[1]:F4}, {StandardDev[1]:F4}]"
                : $"coordinate: std = [{StandardDev[0]:F4}, {StandardDev[1]:F4}, {StandardDev[1]:F4}]";
        }
    }
}
