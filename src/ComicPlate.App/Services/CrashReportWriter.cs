using System.Text;

namespace ComicPlate.App.Services;

public static class CrashReportWriter
{
    public static void Write(Exception exception, string source)
    {
        try
        {
            var directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ComicPlate",
                "crash-reports");
            Directory.CreateDirectory(directory);

            var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd-HHmmss-fff");
            var path = Path.Combine(directory, $"crash-{timestamp}.txt");
            var report = new StringBuilder()
                .AppendLine("ComicPlate Crash Report")
                .AppendLine($"Time: {DateTimeOffset.Now:O}")
                .AppendLine($"Source: {source}")
                .AppendLine($"Version: {typeof(CrashReportWriter).Assembly.GetName().Version}")
                .AppendLine()
                .AppendLine(exception.ToString())
                .ToString();

            File.WriteAllText(path, report, Encoding.UTF8);
        }
        catch
        {
            // Crash reporting must never cause another crash.
        }
    }
}
