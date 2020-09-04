using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace BchainSimServices
{
    public class LogService : Log.LogBase
    {
        private readonly ILogger<LogService> logger;
        public LogService(ILogger<LogService> logger)
        {
            this.logger = logger;
        }

        public override Task<EmptyLogResponse> Write(LogMessage request, ServerCallContext context)
        {
            logger.LogInformation($"{request.Timestamp}\t{request.MinerId}\t{request.Message}");
            return Task.FromResult(new EmptyLogResponse());
        }

    }
}
