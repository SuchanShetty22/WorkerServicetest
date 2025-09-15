using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace testingservice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("WorkerService starting...");

            // Cancellation token for graceful shutdown
            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping WorkerService...");
                e.Cancel = true;
                cts.Cancel();
            };

            var worker = new Worker();
            await worker.RunAsync(cts.Token);

            Console.WriteLine("WorkerService stopped.");
        }
    }

    public class Worker
    {
        private readonly string logFilePath;
        private readonly int intervalSeconds = 10; // log every 10 seconds

        public Worker()
        {
            var logPath = Environment.GetEnvironmentVariable("WORKER_LOG") ?? "./logs/WorkerServiceLog.txt";
            logFilePath = Path.GetFullPath(logPath);
            Directory.CreateDirectory(Path.GetDirectoryName(logFilePath));
        }

        public async Task RunAsync(CancellationToken token)
        {
            WriteLog("Worker started.");

            while (!token.IsCancellationRequested)
            {
                WriteLog("I am working");
                try
                {
                    await Task.Delay(intervalSeconds * 1000, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }

            WriteLog("Worker stopped.");
        }

        private void WriteLog(string message)
        {
            var logMessage = $"{DateTime.Now:G}: {message}";

            // Always log to console (Kubernetes will capture this)
            Console.WriteLine(logMessage);

            try
            {
                var logPath = Environment.GetEnvironmentVariable("WORKER_LOG") ?? "./logs/WorkerServiceLog.txt";
                var dir = Path.GetDirectoryName(logPath);

                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.AppendAllText(logPath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // If file writing fails, log the error to console so it doesn't go unnoticed
                Console.WriteLine($"[LOGGING ERROR] Failed to write log to file: {ex.Message}");
                Console.WriteLine($"[LOGGING ERROR] Original log message was: {logMessage}");
            }
        }
    }
}
