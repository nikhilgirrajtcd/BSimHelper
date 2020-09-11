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
        private object lockObj = new object();

        public GlobalKnowledgeService(IConfiguration configuration)
        {
            var width = configuration.GetValue<int>("MiningConfiguration:ParallelBlocks");
            var startingIndex = configuration.GetValue<int>("MiningConfiguration:StartingIndex");

            if (chainProgress == null)
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

            if (BlockWinners == null)
            {
                BlockWinners = new List<BlockProgress>[width];
                for (int i = 0; i < BlockWinners.Length; i++)
                {
                    BlockWinners[i] = new List<BlockProgress>();
                }
            }
        }

        public override Task<ChainProgress> GetChainProgress(GetChainProgressIn request, ServerCallContext context)
        {
            return Task.FromResult(chainProgress);
        }

        public override Task<ChainProgress> PutChainProgress(BlockProgressIn request, ServerCallContext context)
        {

            var currentBlock = chainProgress.BlockProgress[request.BlockOrdinal];
            if (currentBlock.MinerRoundBlockProgress.ContainsKey(request.MinerId))
            {
                if (currentBlock.MinerRoundBlockProgress[request.MinerId] + 1 >= ConfigService.MiningParams.NRoundBlocks)
                {
                    lock (lockObj)
                    {
                        if (request.BlockIndex == currentBlock.BlockIndex && currentBlock.MinerRoundBlockProgress[request.MinerId] + 1 >= ConfigService.MiningParams.NRoundBlocks)
                        {
                            // record that the miner won
                            currentBlock.PastMiners.Add(request.BlockIndex, request.MinerId);

                            currentBlock.BlockIndex = currentBlock.BlockIndex + 1;
                            currentBlock.MinerRoundBlockProgress.Clear();
                        }
                        else
                        {
                            // race is lost, do nothing
                        }
                    }
                }
                else
                {
                    currentBlock.MinerRoundBlockProgress[request.MinerId]++;
                }
            }
            else
            {
                currentBlock.MinerRoundBlockProgress[request.MinerId] = request.Progress;
            }

            return Task.FromResult(chainProgress);
        }
    }
}
