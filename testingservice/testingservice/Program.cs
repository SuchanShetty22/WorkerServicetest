using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace testingservice
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using var cts = new CancellationTokenSource();

            Console.WriteLine("WorkerService starting...");

            // Handle Ctrl+C
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("Stopping WorkerService (CTRL+C)...");
                e.Cancel = true;
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            };

            // Handle SIGTERM / process exit
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Console.WriteLine("Stopping WorkerService (ProcessExit)...");
                if (!cts.IsCancellationRequested)
                    cts.Cancel();
            };

            var worker = new Worker();
            await worker.RunAsync(cts.Token);

            Console.WriteLine("WorkerService stopped.");
        }
    }

    public class Worker
    {
        private readonly int intervalSeconds = 10;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly string webhookUrl = "https://webhook.site/ad93c79a-ab56-4d76-bbca-1b5e36f7cfda";

        public async Task RunAsync(CancellationToken token)
        {
            Console.WriteLine("Worker started.");

            try
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        // GET request to webhook
                        var response = await _httpClient.GetAsync(webhookUrl, token);
                        Console.WriteLine($"Webhook GET status: {response.StatusCode}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WORKER ERROR] Webhook GET failed: {ex.Message}");
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
                        Console.WriteLine($"[WORKER ERROR] Delay exception: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL ERROR in RunAsync] {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Worker stopped.");
            }
        }
    }
}
