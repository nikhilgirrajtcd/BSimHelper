using System.Collections.Concurrent;
using System.Threading.Tasks;
using Grpc.Core;


namespace BchainSimServices
{
    public class ConfigService : Config.ConfigBase
    {
        static ConcurrentDictionary<string, MinerInfo> miners;
        static ConcurrentDictionary<MinerInfo, MinerStateInfo> minerStates;
        static MiningParams miningParams;

        static ConfigService()
        {
            miners = new ConcurrentDictionary<string, MinerInfo>();
            minerStates = new ConcurrentDictionary<MinerInfo, MinerStateInfo>();

            // this needs to be updateable
            miningParams = new MiningParams
            {
                NRoundBlocks = 5,
                RoundBlockChallengeSize = 5,
                TransactionBlockChallengeSize = 0
            };
        }

        public override Task<MiningParams> GetMiningParams(MinerInfo request, ServerCallContext context)
        {
            // maybe return mining params based on the minerInfo
            return Task.FromResult(miningParams);
        }
        public override Task<EmptyConfigResponse> UpdateMinerState(MinerStateInfo request, ServerCallContext context)
        {
            if (miners.TryGetValue(request?.MinerInfo?.MinerId, out var minerInfo))
            {
                minerStates.TryRemove(minerInfo, out _);
                minerStates.TryAdd(minerInfo, request);
            }
            return base.UpdateMinerState(request, context);
        }

        public override Task<MiningParams> Register(MinerInfo request, ServerCallContext context)
        {
            miners.TryAdd(request.MinerId, request);
            return Task.FromResult(miningParams);
        }
    }
}
