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

            try
            {
                // Create CTS inside try
                using var cts = new CancellationTokenSource();

                // Handle Ctrl+C
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                // Handle process exit (e.g., Kubernetes SIGTERM)
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();

                // Keep-alive: continuously run worker loop and restart if exceptions occur
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await worker.RunAsync(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Worker loop crashed, restarting: {ex}");
                        Console.Out.Flush();
                        // Optional: small delay before retry
                        await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in Main] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                Console.WriteLine("WorkerService stopped.");
                Console.Out.Flush();
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

            while (!token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"I am working: {DateTime.Now:G}");
                    Console.Out.Flush();
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
                    // Graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WORKER ERROR] Delay exception: {ex}");
                    Console.Out.Flush();
                }
            }

            Console.WriteLine("Worker stopped.");
            Console.Out.Flush();
        }
    }
}
