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
            // Ensure console output is synchronized for Linux/Docker
            Console.SetOut(TextWriter.Synchronized(Console.Out));

            Console.WriteLine("WorkerService starting...");
            Console.Out.Flush();

            try
            {
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

                // Handle process exit (SIGTERM)
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    Console.Out.Flush();
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();

                // Start worker in background task
                var workerTask = Task.Run(() => worker.RunAsync(cts.Token));

                Console.WriteLine("Worker loop started.");
                Console.Out.Flush();

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
                Console.Out.Flush();
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
                        await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), token);
                    }
                    catch (TaskCanceledException)
                    {
                        Console.WriteLine("TaskCanceledException during delay (shutting down worker)...");
                        Console.Out.Flush();
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
