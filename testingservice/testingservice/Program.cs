using System;
using System.Threading;
using System.Threading.Tasks;

namespace testingservice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Ensure console output is synchronized for Linux/Docker
            Console.SetOut(TextWriter.Synchronized(Console.Out));

            Console.WriteLine("WorkerService starting...");
            Console.Out.Flush();

            try
            {
                // Create CancellationTokenSource inside try/finally
                using var cts = new CancellationTokenSource();

                // Handle Ctrl+C
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                    Console.Out.Flush();
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                // Handle process exit (e.g., SIGTERM in Kubernetes)
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    Console.Out.Flush();
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();

                // Keep-alive loop: continuously run worker until canceled
                while (!cts.Token.IsCancellationRequested)
                {
                    try
                    {
                        await worker.RunAsync(cts.Token);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Worker crashed, restarting: {ex}");
                        Console.Out.Flush();
                        // Small delay before restarting
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            // Expected on shutdown
                            break;
                        }
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

            try
            {
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
                        // Delay 10 seconds or until token cancellation
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected when shutting down
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WORKER ERROR] Delay exception: {ex}");
                        Console.Out.Flush();
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
    }
}
