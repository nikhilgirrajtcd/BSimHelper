using System.Threading.Tasks;
using Grpc.Core;

namespace BchainSimServices
{
    public class LogService : Log.LogBase
    {
        public override Task<EmptyLogResponse> Write(LogMessage request, ServerCallContext context)
        {
            return base.Write(request, context);
        }

    }
}
