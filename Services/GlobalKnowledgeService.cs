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
            return Task.FromResult(chainProgress.Clone());
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

                            UnstallIfAny(chainProgress, currentBlock);

                            if (ShouldStall(chainProgress, currentBlock))
                                currentBlock.Stall = true;
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
                lock (lockObj)
                {
                    currentBlock.MinerRoundBlockProgress[request.MinerId] = request.Progress;
                }
            }

            return Task.FromResult(chainProgress.Clone());
        }

        private bool ShouldStall(ChainProgress chainProgress, BlockProgress currentBlock)
        {
            int lastBlockOrdinal = ConfigService.MiningParams.NParallelBlocks - 1;
            int otherDependentBlockOrdinal = currentBlock.BlockOrdinal == lastBlockOrdinal ? 0 : currentBlock.BlockOrdinal + 1;
            int otherDependentBlockIndex = currentBlock.BlockIndex - 1;
            if (chainProgress.BlockProgress[otherDependentBlockOrdinal].BlockIndex <= otherDependentBlockIndex)
            {
                return true;
            }

            if (chainProgress.BlockProgress[otherDependentBlockOrdinal].BlockIndex == otherDependentBlockIndex)
            {
                return chainProgress.BlockProgress[otherDependentBlockOrdinal].Stall;
            }

            return false;
        }

        private void UnstallIfAny(ChainProgress chainProgress, BlockProgress currentBlock)
        {
            int lastBlockOrdinal = ConfigService.MiningParams.NParallelBlocks - 1;
            int otherDependentBlockOrdinal = currentBlock.BlockOrdinal == 0 ? lastBlockOrdinal : currentBlock.BlockOrdinal - 1;
            int otherDependentBlockIndex = currentBlock.BlockIndex;
            if (chainProgress.BlockProgress[otherDependentBlockOrdinal].BlockIndex == otherDependentBlockIndex)
            {
                chainProgress.BlockProgress[otherDependentBlockOrdinal].Stall = false;
            }

            if (chainProgress.BlockProgress[otherDependentBlockOrdinal].BlockIndex == otherDependentBlockIndex)
            {
                chainProgress.BlockProgress[otherDependentBlockOrdinal].Stall = false;
            }
        }
    }
}
