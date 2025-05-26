using System.IO.Pipes;
using System.Text.Json;

namespace AntennaCalibrator.Utilis
{
    public class PipeSenderService : IDisposable
    {
        private readonly NamedPipeServerStream _pipeServer;
        private readonly StreamWriter _writer;

        public PipeSenderService(string pipeName = "ChromosomePipe")
        {
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.Out, 1, PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
            _pipeServer.WaitForConnection();

            _writer = new StreamWriter(_pipeServer) { AutoFlush = true };
        }

        public async Task SendAsync<T>(T obj)
        {
            var json = JsonSerializer.Serialize(obj);
            await _writer.WriteLineAsync(json);
        }

        public void Dispose()
        {
            _writer.Dispose();
            _pipeServer.Dispose();
        }
    }

}
