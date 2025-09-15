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
                CancellationTokenSource? cts = null;
                // Create CTS with defensive guard
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

                try
                {
                    // Run the worker loop and keep Main alive until cancellation
                    await Task.WhenAny(
                        worker.RunAsync(cts.Token),
                        Task.Delay(Timeout.Infinite, cts.Token)
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[FATAL ERROR] Exception in worker loop: {ex}");
                    Console.Out.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in Main] {ex}");
                Console.Out.Flush();
            }
            finally
            {
                //cts?.Dispose();
                Console.WriteLine("WorkerService stopped.");
            }
        }
    }

    public class Worker
    {
        private readonly int intervalSeconds = 10;

        public async Task RunAsync(CancellationToken token)
        {
            WriteLog("Worker started.");

            try
            {
                while (!token.IsCancellationRequested)
                {
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
                WriteLog($"[FATAL ERROR in RunAsync] {ex}");
            }
            finally
            {
                WriteLog("Worker stopped.");
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
