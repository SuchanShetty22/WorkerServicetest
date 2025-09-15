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
                        // Run a single iteration instead of an infinite loop inside RunAsync
                        await worker.DoWorkOnceAsync(cts.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        // Expected on shutdown
                        Console.WriteLine("TaskCanceledException caught in Main loop (shutting down)...");
                        Console.Out.Flush();
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Worker crashed, restarting: {ex}");
                        Console.Out.Flush();
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(5), cts.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            Console.WriteLine("TaskCanceledException during restart delay (shutting down)...");
                            Console.Out.Flush();
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

        // Single iteration method for Linux/Docker reliability
        public async Task DoWorkOnceAsync(CancellationToken token)
        {
            Console.WriteLine("Worker started iteration.");
            Console.Out.Flush();

            try
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
                    Console.WriteLine("TaskCanceledException during delay (shutting down iteration)...");
                    Console.Out.Flush();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WORKER ERROR] Delay exception: {ex}");
                    Console.Out.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in DoWorkOnceAsync] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                Console.WriteLine("Worker iteration ended.");
                Console.Out.Flush();
            }
        }
    }
}
