using System.IO.Pipes;

namespace AntennaCalibrator.View.Shared.Services
{
    public class PipeService
    {
        public event EventHandler<string>? DataReceived;

        private CancellationTokenSource? _cts;

        public void StartListening()
        {
            _cts = new CancellationTokenSource();

            Task.Run(() =>
            {
                try
                {
                    var pipeClient = new NamedPipeClientStream(".", "ChromosomePipe", PipeDirection.In);
                    pipeClient.Connect();

                    using (var reader = new StreamReader(pipeClient))
                    {
                        while (!_cts.Token.IsCancellationRequested)
                        {
                            string? data = reader.ReadLine();
                            if (data != null)
                            {
                                DataReceived?.Invoke(this, data);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Errore nella pipe: {ex.Message}");
                }
            });
        }

        public void StopListening()
        {
            _cts?.Cancel();
        }
    }
}
