using System.Collections.Concurrent;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.Extensions.Configuration;

namespace BchainSimServices
{
    public class ConfigService : Config.ConfigBase
    {
        static ConcurrentDictionary<string, MinerInfo> miners;
        static ConcurrentDictionary<MinerInfo, MinerStateInfo> minerStates;
        public static MiningParams MiningParams { get;  private set; }

        static ConfigService()
        {
            miners = new ConcurrentDictionary<string, MinerInfo>();
            minerStates = new ConcurrentDictionary<MinerInfo, MinerStateInfo>();

            // this needs to be updateable
            MiningParams = new MiningParams
            {
                NRoundBlocks = 5,
                RoundBlockChallengeSize = 5,
                TransactionBlockChallengeSize = 0
            };
        }

        public ConfigService(IConfiguration configuration) : base()
        {
            if(MiningParams == null)
            {
                MiningParams = new MiningParams
                {
                    NRoundBlocks = 5,
                    RoundBlockChallengeSize = 5,
                    TransactionBlockChallengeSize = 0,
                    NParallelBlocks = configuration.GetValue<int>("MiningConfiguration:ParallelBlocks")
                };
            }
        }

        public override Task<MiningParams> GetMiningParams(MinerInfo request, ServerCallContext context)
        {
            // maybe return mining params based on the minerInfo
            return Task.FromResult(MiningParams);
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
            return Task.FromResult(MiningParams);
        }

        public override Task<MiningParamsUpdateOut> UpdateMiningParams(MiningParamsUpdateIn request, ServerCallContext context)
        {
            if (request.NRoundBlocks > 0)
                MiningParams.NRoundBlocks = request.NRoundBlocks;

            if (request.RoundBlockChallengeSize > 0)
                MiningParams.RoundBlockChallengeSize = request.RoundBlockChallengeSize;

            if (request.TransactionBlockChallengeSize > 0)
                MiningParams.TransactionBlockChallengeSize = request.TransactionBlockChallengeSize;

            return Task.FromResult(new MiningParamsUpdateOut());
        }
    }
}
