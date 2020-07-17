using System.Threading.Tasks;
using Grpc.Core;


namespace BchainSimServices
{
    public class ConfigService : Config.ConfigBase
    {
        public override Task<MinerConfig> GetMinerConfig(MinerInfo request, ServerCallContext context)
        {
            return base.GetMinerConfig(request, context);
        }
        public override Task<EmptyConfigResponse> UpdateMinerState(MinerStateInfo request, ServerCallContext context)
        {
            return base.UpdateMinerState(request, context);
        }
    }
}
