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
                // Declare and create CTS inside the try block
                CancellationTokenSource? cts = new CancellationTokenSource();

                // Handle Ctrl+C
                Console.CancelKeyPress += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                    e.Cancel = true;
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                // Handle process exit
                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                    if (!cts.IsCancellationRequested)
                        cts.Cancel();
                };

                var worker = new Worker();

                try
                {
                    // Keep Main alive while RunAsync is looping
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
                finally
                {
                    cts.Dispose();
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
                        break; // graceful exit
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
