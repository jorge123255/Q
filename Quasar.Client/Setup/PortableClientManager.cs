using Quasar.Client.Config;
using System;
using System.IO;
using System.Windows.Forms;

namespace Quasar.Client.Setup
{
    /// <summary>
    /// Provides functionality to run the client as a portable application without installation.
    /// </summary>
    public class PortableClientManager
    {
        private static readonly string PortableSettingsPath = Path.Combine(
            Path.GetDirectoryName(Application.ExecutablePath) ?? string.Empty, 
            "data");

        /// <summary>
        /// Determines if the client is running in portable mode.
        /// </summary>
        /// <returns>True if the client should run in portable mode.</returns>
        public static bool IsPortableMode()
        {
            // Client is in portable mode by default unless it's in a typical install path
            string exePath = Application.ExecutablePath;
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            
            // If the client is in a typical installation directory, it's not in portable mode
            return !exePath.StartsWith(appDataPath) && 
                   !exePath.StartsWith(programFilesPath) && 
                   !exePath.StartsWith(programFilesX86Path);
        }

        /// <summary>
        /// Sets up the client for portable operation.
        /// </summary>
        public static void SetupPortableMode()
        {
            try
            {
                // Create data directory if it doesn't exist
                if (!Directory.Exists(PortableSettingsPath))
                    Directory.CreateDirectory(PortableSettingsPath);
                
                // Create logs directory if it doesn't exist
                string logsPath = Path.Combine(PortableSettingsPath, "logs");
                if (!Directory.Exists(logsPath))
                    Directory.CreateDirectory(logsPath);
                
                // Set portable flag
                Settings.PORTABLE = true;
                
                // Update paths to use portable locations
                Settings.DIRECTORY = Path.GetDirectoryName(Application.ExecutablePath) ?? 
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                Settings.LOGSPATH = logsPath;
                Settings.INSTALL = false; // Never install in portable mode
                Settings.STARTUP = false; // Never add to startup in portable mode
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error setting up portable mode: " + ex.Message);
            }
        }
    }
}
