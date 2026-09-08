namespace CommercialMagDb.Api.Infrastructure;

public static class ApiStartupLogger
{
    public static void Write(string contentRootPath, string message, Exception? exception = null)
    {
        try
        {
            var logDir = Path.Combine(contentRootPath, "logs");
            Directory.CreateDirectory(logDir);
            var path = Path.Combine(logDir, $"api-startup-{DateTime.UtcNow:yyyyMMdd}.log");
            var lines = new List<string>
            {
                $"{DateTime.UtcNow:O} {message}"
            };

            if (exception is not null)
            {
                lines.Add($"{exception.GetType().FullName}: {exception.Message}");
                lines.Add(exception.StackTrace ?? string.Empty);
            }

            File.AppendAllLines(path, lines);
        }
        catch
        {
            // Startup diagnostics must never prevent the API from reporting the original failure.
        }
    }
}
