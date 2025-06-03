using System;
using System.Threading.Tasks;

namespace Quasar.Relay.Tests.LoadTesting
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Quasar Relay Load Testing Tool");
            Console.WriteLine("==============================");
            
            string relayUrl = "wss://your-domain.com";
            int maxConnections = 100;
            int connectionRate = 10;
            bool useEncryption = true;
            string password = "LoadTestPassword123!";
            int testDuration = 60;
            
            // Parse command line arguments
            if (args.Length > 0)
            {
                relayUrl = args[0];
                
                if (args.Length > 1) int.TryParse(args[1], out maxConnections);
                if (args.Length > 2) int.TryParse(args[2], out connectionRate);
                if (args.Length > 3) bool.TryParse(args[3], out useEncryption);
                if (args.Length > 4) password = args[4];
                if (args.Length > 5) int.TryParse(args[5], out testDuration);
            }
            else
            {
                Console.WriteLine("\nUsage: LoadTesting.exe <relay-url> [max-connections] [conn-rate] [use-encryption] [password] [test-duration]");
                Console.WriteLine("Example: LoadTesting.exe wss://relay.example.com 200 20 true MySecurePassword 120");
                
                // Prompt for relay URL if not provided
                Console.Write("\nEnter relay server URL (wss://your-domain.com): ");
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    relayUrl = input;
                }
                
                Console.Write($"Maximum concurrent connections [{maxConnections}]: ");
                input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out int value))
                {
                    maxConnections = value;
                }
                
                Console.Write($"Connection rate per second [{connectionRate}]: ");
                input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out value))
                {
                    connectionRate = value;
                }
                
                Console.Write($"Use encryption (true/false) [{useEncryption}]: ");
                input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input) && bool.TryParse(input, out bool boolValue))
                {
                    useEncryption = boolValue;
                }
                
                if (useEncryption)
                {
                    Console.Write($"Encryption password [{password}]: ");
                    input = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(input))
                    {
                        password = input;
                    }
                }
                
                Console.Write($"Test duration in seconds [{testDuration}]: ");
                input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input) && int.TryParse(input, out value))
                {
                    testDuration = value;
                }
            }
            
            Console.WriteLine("\nStarting load test with the following parameters:");
            Console.WriteLine($"Relay URL: {relayUrl}");
            Console.WriteLine($"Max Connections: {maxConnections}");
            Console.WriteLine($"Connection Rate: {connectionRate}/second");
            Console.WriteLine($"Using Encryption: {useEncryption}");
            Console.WriteLine($"Test Duration: {testDuration} seconds");
            Console.WriteLine("\nPress Enter to begin the test or Ctrl+C to cancel...");
            Console.ReadLine();
            
            // Create and run the load test
            var loadTest = new RelayLoadTest(
                relayServerUrl: relayUrl,
                maxConnections: maxConnections,
                connectionRate: connectionRate,
                useEncryption: useEncryption,
                password: password,
                testDurationSeconds: testDuration
            );
            
            await loadTest.RunTest();
            
            Console.WriteLine("\nTest completed. Press Enter to exit...");
            Console.ReadLine();
        }
    }
}
