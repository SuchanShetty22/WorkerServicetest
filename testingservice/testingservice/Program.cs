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

            CancellationTokenSource? cts = null;

            try
            {
                // Defensive guard in case CTS creation itself fails
                cts = new CancellationTokenSource();

                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();
                await worker.RunAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in Main] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                cts?.Dispose(); // safe cleanup if created
                Console.WriteLine("WorkerService stopped.");
            }
        }

    }

    public class Worker
    {
        private readonly string logFilePath;
        private readonly int intervalSeconds = 10;

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
                while (true)
                {
                    if (token.IsCancellationRequested)
                        break;

                    try
                    {
                        WriteLog("I am working");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WORKER ERROR] Failed to log message: {ex}");
                        Console.Out.Flush();
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        // expected when shutting down
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLog($"[FATAL ERROR in RunAsync] {ex}");
            }
            finally
            {
                WriteLog("Worker stopped.");
            }
        }

        private void WriteLog(string message)
        {
            var logMessage = $"{DateTime.Now:G}: {message}";
            Console.WriteLine(logMessage);
            Console.Out.Flush();

            try
            {
                var dir = Path.GetDirectoryName(logFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }
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
