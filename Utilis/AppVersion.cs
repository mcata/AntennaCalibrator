using System.Reflection;

namespace AntennaCalibrator.Utilis
{
    internal class AppVersion
    {
        public static string GetApplicationInfo()
        {
            var version = GetVersionFromAssembly();
            var compilationDate = GetCompilationDate();
            return $"Version: {version}, Compiled on: {compilationDate:yyyy-MM-dd HH:mm:ss}";
        }

        public static string GetApplicationLocation()
        {
            return Assembly.GetExecutingAssembly().Location;
        }

        private static string GetVersionFromAssembly()
        {
            string strVersion = default!;
            var versionAttribute = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (versionAttribute != null)
            {
                var version = versionAttribute.InformationalVersion;
                var plusIndex = version.IndexOf('+');
                if (plusIndex >= 0 && plusIndex + 9 < version.Length)
                {
                    strVersion = version[..(plusIndex + 9)];
                }
                else
                {
                    strVersion = version;
                }
            }

            return strVersion;
        }

        private static DateTime GetCompilationDate()
        {
            var filePath = Assembly.GetExecutingAssembly().Location;
            return File.GetLastWriteTime(filePath);
        }
    }
}
