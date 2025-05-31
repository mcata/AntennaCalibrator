namespace AntennaCalibrator.Utilis
{
    internal class Residue
    {
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public double Value { get; set; }
        public int Q { get; set; } // 0 = no data, 1 = float, 2 = fixed, 3 = hold
        public int ValidDataFlag { get; set; } // 0 = invalid data, 1 = valid data
    }
}
