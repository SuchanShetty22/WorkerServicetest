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

            try
            {
                while (!token.IsCancellationRequested)
                {
                    WriteLog("I am working");
                    await Task.Delay(intervalSeconds * 1000, token);
                }
            }
            catch (TaskCanceledException)
            {
                WriteLog("Worker cancellation requested.");
            }
            finally
            {
                WriteLog("Worker stopped.");
            }
        }


        private void WriteLog(string message)
        {
            var logMessage = $"{DateTime.Now:G}: {message}";

            try
            {
                // Console logging
                Console.WriteLine(logMessage);
                Console.Out.Flush();  // <-- force immediate flush to Docker stdout

                // Optional file logging
                var dir = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);

                File.AppendAllText(logFilePath, logMessage + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGING ERROR] {ex.Message}");
                Console.Out.Flush();
            }
        }

    }
}
