using System.Diagnostics;
using System.Text;

namespace ConsoleApp.IntegrationTests;

public class ConsoleAppIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _appsettingsContent;

    public ConsoleAppIntegrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        
        _appsettingsContent = """
        {
          "AppSettings": {
            "ApplicationName": "Test Console App",
            "Version": "1.0.0-test",
            "Description": "Test configuration",
            "Environment": "Test"
          },
          "Logging": {
            "Level": "Debug"
          }
        }
        """;
    }

    [Fact]
    public void ConsoleApp_WithAppsettingsJsonPresent_ShouldEchoConfiguration()
    {
        // Arrange
        string appsettingsPath = Path.Combine(_tempDir, "appsettings.json");
        File.WriteAllText(appsettingsPath, _appsettingsContent);

        // Build the console app to the temp directory
        BuildConsoleAppToDirectory(_tempDir);
        
        string executablePath = GetExecutablePath(_tempDir);

        // Act
        var result = RunConsoleApp(executablePath);

        // Assert
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("=== Configuration Values ===", result.Output);
        Assert.Contains("Application Name: Test Console App", result.Output);
        Assert.Contains("Version: 1.0.0-test", result.Output);
        Assert.Contains("Description: Test configuration", result.Output);
        Assert.Contains("Environment: Test", result.Output);
        Assert.Contains("Logging Level: Debug", result.Output);
        Assert.Contains("=== Success ===", result.Output);
        Assert.Contains($"Configuration loaded from: {appsettingsPath}", result.Output);
    }

    [Fact]
    public void ConsoleApp_WithoutAppsettingsJson_ShouldShowError()
    {
        // Arrange
        BuildConsoleAppToDirectory(_tempDir);
        string executablePath = GetExecutablePath(_tempDir);

        // Act
        var result = RunConsoleApp(executablePath);

        // Assert
        Assert.Equal(1, result.ExitCode);
        Assert.Contains("ERROR: appsettings.json file not found", result.Output);
        Assert.Contains("Please ensure appsettings.json is in the same directory as the executable", result.Output);
    }

    private void BuildConsoleAppToDirectory(string outputDir)
    {
        var projectFile = GetProjectFilePath();
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = $"publish \"{projectFile}\" -c Release --self-contained false -p:PublishSingleFile=true -r linux-x64 -o \"{outputDir}\"",
                WorkingDirectory = Path.GetDirectoryName(projectFile),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Failed to build console app. Exit code: {process.ExitCode}\nOutput: {output}\nError: {error}");
        }
    }

    private string GetExecutablePath(string directory)
    {
        // On Linux/Mac, the executable doesn't have an extension
        string executableName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "ConsoleApp.exe" : "ConsoleApp";
        return Path.Combine(directory, executableName);
    }

    private string GetProjectFilePath()
    {
        // Find the project file relative to the test assembly
        string currentDir = Directory.GetCurrentDirectory();
        string projectFile = "ConsoleApp.csproj";
        
        // Go up directories until we find the project file
        DirectoryInfo? dir = new DirectoryInfo(currentDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, projectFile)))
        {
            dir = dir.Parent;
        }
        
        if (dir == null)
        {
            throw new FileNotFoundException($"Could not find {projectFile}");
        }
        
        return Path.Combine(dir.FullName, projectFile);
    }

    private (int ExitCode, string Output) RunConsoleApp(string executablePath)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = executablePath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            }
        };

        var output = new StringBuilder();
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (e.Data != null)
                output.AppendLine(e.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return (process.ExitCode, output.ToString());
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, true);
        }
    }
}