using Microsoft.Extensions.Configuration;

class Program
{
    static void Main(string[] args)
    {
        try
        {
            // Get the directory where the executable is located
            // Use AppContext.BaseDirectory for single-file applications
            string exeDirectory = AppContext.BaseDirectory;
            
            string appsettingsPath = Path.Combine(exeDirectory, "appsettings.json");
            
            // Check if appsettings.json exists in the same directory as the executable
            if (!File.Exists(appsettingsPath))
            {
                Console.WriteLine($"ERROR: appsettings.json file not found at: {appsettingsPath}");
                Console.WriteLine("Please ensure appsettings.json is in the same directory as the executable.");
                Environment.Exit(1);
            }

            // Build configuration from the external appsettings.json file
            var configuration = new ConfigurationBuilder()
                .SetBasePath(exeDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            // Echo the configuration values
            Console.WriteLine("=== Configuration Values ===");
            Console.WriteLine($"Application Name: {configuration["AppSettings:ApplicationName"]}");
            Console.WriteLine($"Version: {configuration["AppSettings:Version"]}");
            Console.WriteLine($"Description: {configuration["AppSettings:Description"]}");
            Console.WriteLine($"Environment: {configuration["AppSettings:Environment"]}");
            Console.WriteLine($"Logging Level: {configuration["Logging:Level"]}");
            Console.WriteLine($"Configuration loaded from: {appsettingsPath}");
            Console.WriteLine("=== Success ===");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
    }
}
