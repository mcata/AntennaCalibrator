using System.IO.Pipes;
using System.Text.Json;

namespace AntennaCalibrator.Utilis
{
    public class PipeSenderService : IDisposable
    {
        private readonly NamedPipeServerStream _pipeServer;
        private StreamWriter? _writer;
        private readonly CancellationTokenSource _cts = new();

        public PipeSenderService(string pipeName = "ChromosomePipe")
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous);

            // Avvia la connessione in modo asincrono, senza bloccare
            _ = WaitForConnectionAsync(_cts.Token);
        }

        private async Task WaitForConnectionAsync(CancellationToken cancellationToken)
        {
            try
            {
                await _pipeServer.WaitForConnectionAsync(cancellationToken);
                _writer = new StreamWriter(_pipeServer) { AutoFlush = true };
            }
            catch (OperationCanceledException)
            {
                // Cancellazione volontaria
            }
            catch (IOException)
            {
                // La connessione è fallita
            }
        }

        public async Task SendAsync<T>(T obj)
        {
            try
            {
                if (_pipeServer.IsConnected && _writer != null)
                {
                    var json = JsonSerializer.Serialize(obj);
                    await _writer.WriteLineAsync(json);
                }
            }
            catch (IOException)
            {
                // Il client si è disconnesso
            }
            catch (ObjectDisposedException)
            {
                // Pipe già chiusa
            }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _writer?.Dispose();
            _pipeServer.Dispose();
        }
    }
}