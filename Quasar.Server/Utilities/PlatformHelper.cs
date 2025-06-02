using System;

namespace Quasar.Server.Utilities
{
    /// <summary>
    /// Helper class to detect and manage platform-specific functionality.
    /// </summary>
    public static class PlatformHelper
    {
        /// <summary>
        /// Gets a value indicating whether the current platform is Windows.
        /// </summary>
        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;

        /// <summary>
        /// Gets a value indicating whether the current platform is macOS.
        /// </summary>
        public static bool IsMacOS
        {
            get
            {
                // Check both legacy and newer ways to identify macOS
                return Environment.OSVersion.Platform == PlatformID.MacOSX ||
                       (Environment.OSVersion.Platform == PlatformID.Unix &&
                        GetUname().Contains("Darwin"));
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current platform is Linux.
        /// </summary>
        public static bool IsLinux
        {
            get
            {
                return Environment.OSVersion.Platform == PlatformID.Unix &&
                       !GetUname().Contains("Darwin");
            }
        }
        
        /// <summary>
        /// Gets the current platform name as a string.
        /// </summary>
        public static string PlatformName
        {
            get
            {
                if (IsWindows) return "Windows";
                if (IsMacOS) return "macOS";
                if (IsLinux) return "Linux";
                return "Unknown";
            }
        }

        /// <summary>
        /// Gets the output of the 'uname' command used to identify Unix/macOS systems.
        /// </summary>
        private static string GetUname()
        {
            try
            {
                var process = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "uname",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                return output;
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Returns the appropriate path separator for the current platform.
        /// </summary>
        public static char DirectorySeparatorChar => 
            IsWindows ? '\\' : '/';
    }
}
