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
            // Ensure Console output auto-flushes for Kubernetes logs
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput())
            {
                AutoFlush = true
            });

            Console.WriteLine("WorkerService starting...");

            try
            {
                using var cts = new CancellationTokenSource();

                // Handle Ctrl+C
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                // Handle process exit (SIGTERM in Kubernetes)
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();

                // Start worker in background task
                var workerTask = Task.Run(() => worker.RunAsync(cts.Token));

                Console.WriteLine("Worker loop started.");

                // Keep Main alive until cancellation
                while (!cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(1000, cts.Token);
                }

                // Wait for worker to finish gracefully
                await workerTask;
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("TaskCanceledException caught in Main (shutting down)...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in Main] {ex}");
            }
            finally
            {
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

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        Console.WriteLine("I am working");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WORKER ERROR] Failed to log message: {ex}");
                    }

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("TaskCanceledException during delay (shutting down worker)...");
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WORKER ERROR] Delay exception: {ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in RunAsync] {ex}");
            }
            finally
            {
                Console.WriteLine("Worker stopped.");
            }
        }
    }
}
