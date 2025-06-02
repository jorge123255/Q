using Quasar.Server.Forms;
using Quasar.Server.Utilities;
using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace Quasar.Server
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // enable TLS 1.2
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            
            // Set working directory appropriately for macOS app bundles
            if (PlatformHelper.IsMacOS)
            {
                InitializeMacOS();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new FrmMain());
        }
        
        /// <summary>
        /// Initialize macOS-specific settings and environment.
        /// </summary>
        private static void InitializeMacOS()
        {
            try
            {
                // For macOS app bundles, we need to set the working directory to the Resources folder
                string executablePath = Assembly.GetEntryAssembly().Location;
                string executableDir = Path.GetDirectoryName(executablePath);
                
                // Check if we're in a .app bundle (Resources directory)
                if (executableDir.Contains(".app/Contents/Resources") || 
                    executableDir.EndsWith("/Resources"))
                {
                    // Already in the right directory
                    return;
                }
                
                // Check if we're in a .app bundle but not in the Resources directory
                if (executableDir.Contains(".app"))
                {
                    string resourcesPath = Path.Combine(executableDir, "Resources");
                    if (Directory.Exists(resourcesPath))
                    {
                        Directory.SetCurrentDirectory(resourcesPath);
                    }
                }
                
                // Set DPI awareness for macOS Retina displays
                Environment.SetEnvironmentVariable("MONO_MWF_SCALING", "1");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing macOS environment: {ex.Message}", 
                    "Quasar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
