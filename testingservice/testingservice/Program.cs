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
        private readonly int intervalSeconds = 10;

        public async Task RunAsync(CancellationToken token)
        {
            WriteLog("Worker started.");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    WriteLog("I am working");
                    await Task.Delay(TimeSpan.FromSeconds(10), token); // 10 sec interval
                }
            }
            catch (TaskCanceledException)
            {
                WriteLog("Worker cancellation requested.");
            }
            catch (Exception ex)
            {
                WriteLog($"[FATAL ERROR] {ex}");
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
        }
    }
}
