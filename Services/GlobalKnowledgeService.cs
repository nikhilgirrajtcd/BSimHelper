using System.Collections.Generic;
using System.Threading.Tasks;

using Grpc.Core;

using Microsoft.Extensions.Configuration;

namespace BchainSimServices.Services
{
    public class GlobalKnowledgeService : GlobalKnowledge.GlobalKnowledgeBase
    {
        public static List<BlockProgress>[] BlockWinners;
        static ChainProgress chainProgress;

        public GlobalKnowledgeService(IConfiguration configuration)
        {
            var width = configuration.GetValue<int>("MiningConfiguration:ParallelBlocks");
            var startingIndex = configuration.GetValue<int>("MiningConfiguration:StartingIndex");

            if(chainProgress == null)
            {
                chainProgress = new ChainProgress();
                chainProgress.BlockProgress.Clear();
                for (int i = 0; i < width; i++)
                {
                    chainProgress.BlockProgress.Add(new BlockProgress()
                    {
                        BlockIndex = startingIndex,
                        BlockOrdinal = i
                    });
                }
            }

            if(BlockWinners == null)
            {
                BlockWinners = new List<BlockProgress>[width];
                for (int i = 0; i < BlockWinners.Length; i++)
                {
                    BlockWinners[i] = new List<BlockProgress>();
                }
            }
        }


        public override Task<ChainProgress> GetChainProgress(NothingGk request, ServerCallContext context)
        {
            return Task.FromResult(chainProgress);
        }

        public override Task<ChainProgress> PutChainProgress(BlockProgressIn request, ServerCallContext context)
        {
            if (request.BlockProgress == ConfigService.MiningParams.NRoundBlocks) // block won
            {
                var currentBp = chainProgress.BlockProgress[request.BlockOrdinal];
                // verify old progress
                if (currentBp.MinerRoundBlockProgress[request.MinerId] + 1 == ConfigService.MiningParams.NRoundBlocks)
                {
                    chainProgress.BlockProgress[request.BlockOrdinal] = new BlockProgress
                    {
                        BlockIndex = currentBp.BlockIndex + 1,
                        BlockOrdinal = currentBp.BlockOrdinal
                    };
                }
            }
            return Task.FromResult(chainProgress);
        }
    }
}
