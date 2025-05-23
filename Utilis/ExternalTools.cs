using System.Diagnostics;

namespace AntennaCalibrator.Utilis
{
    internal class ExternalTools
    {
        public static bool ExecuteRTKLIB(string config, string obsRnx, string refRnx, string navRxn, string sp3File, string outFile)
        {
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = @".\ancillary\sw\rnx2rtkpAndroid.exe";
            startInfo.Arguments = string.Format("-o {0} -k {1} {2} {3} {4} {5}",
                                                outFile, config, obsRnx, refRnx, navRxn, sp3File);
            startInfo.CreateNoWindow = true;
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            Process process = Process.Start(startInfo);
            return process.WaitForExit(60000);
        }

        public static void LaunchGraphMananger(string filepath)
        {
            using (Process pProcess = new Process())
            {
                pProcess.StartInfo.FileName = @"python3";
                pProcess.StartInfo.Arguments = @".\ancillary\sw\ChartBuilder.py " + filepath;
                pProcess.StartInfo.UseShellExecute = false;
                pProcess.StartInfo.RedirectStandardOutput = false;
                pProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                pProcess.StartInfo.CreateNoWindow = true;
                pProcess.Start();
            }
        }
    }
}
