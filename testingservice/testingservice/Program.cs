using System;
using System.Threading;
using System.Threading.Tasks;

namespace testingservice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("WorkerService starting...");

            // Create CTS inside try, dispose only after worker completes
            CancellationTokenSource? cts = null;

            try
            {
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

                // Await worker RunAsync indefinitely until token cancellation
                await worker.RunAsync(cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in Main] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                // Dispose only after RunAsync exits
                cts?.Dispose();
                Console.WriteLine("WorkerService stopped.");
            }
        }

    }

    public class Worker
    {
        private readonly int intervalSeconds = 10;

        public async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine("Worker started.");
            Console.Out.Flush();

            try
            {
                while (!token.IsCancellationRequested)
                {
                    Console.WriteLine($"I am working: {DateTime.Now:G}");
                    Console.Out.Flush();

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(10), token);
                    }
                    catch (TaskCanceledException)
                    {
                        break; // exit loop on cancellation
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in RunAsync] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                Console.WriteLine("Worker stopped.");
                Console.Out.Flush();
            }
        }



        private void WriteLog(string message)
        {
            try
            {
                var logMessage = $"{DateTime.Now:G}: {message}";
                Console.WriteLine(logMessage);
                Console.Out.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LOGGING ERROR] {ex}");
                Console.Out.Flush();
            }
        }
    }
}
