﻿using Lyra.Core.API;
using Lyra.Core.Blocks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;
using Lyra.Core.Utils;
using Lyra.Core.Accounts;

namespace Lyra.Core.Decentralize
{
    public class NodeAPI : INodeAPI
    {
        public NodeAPI()
        {
        }

        public Task<GetSyncStateAPIResult> GetSyncState()
        {
            var result = new GetSyncStateAPIResult
            {
                ResultCode = APIResultCodes.Success,
                Mode = BlockChain.Singleton.InSyncing ? ConsensusWorkingMode.OutofSyncWaiting : ConsensusWorkingMode.Normal,
                NewestBlockUIndex = BlockChain.Singleton.GetNewestBlockUIndex()
            };
            return Task.FromResult(result);
        }

        public Task<BlockAPIResult> GetBlockByUIndex(long uindex)
        {
            BlockAPIResult result;
            var block = BlockChain.Singleton.GetBlockByUIndex(uindex);
            if(block == null)
            {
                result = new BlockAPIResult { ResultCode = APIResultCodes.BlockNotFound };
            }
            else
            {
                result = new BlockAPIResult
                {
                    BlockData = Json(block),
                    ResultBlockType = block.BlockType,
                    ResultCode = APIResultCodes.Success
                };
            }

            return Task.FromResult(result);
        }

        public Task<GetVersionAPIResult> GetVersion(int apiVersion, string appName, string appVersion)
        {
            var result = new GetVersionAPIResult()
            {
                ResultCode = APIResultCodes.Success,
                ApiVersion = LyraGlobal.ProtocolVersion,
                NodeVersion = LyraGlobal.NodeAppName,
                UpgradeNeeded = false,
                MustUpgradeToConnect = apiVersion < LyraGlobal.ProtocolVersion
            };
            return Task.FromResult(result);
        }

        public Task<AccountHeightAPIResult> GetSyncHeight()
        {
            var result = new AccountHeightAPIResult();
            try
            {
                var last_sync_block = BlockChain.Singleton.GetSyncBlock();
                if(last_sync_block == null)
                {
                    // empty database. 
                    throw new Exception("Database empty.");
                }
                result.Height = last_sync_block.Index;
                result.SyncHash = last_sync_block.Hash;
                result.NetworkId = Neo.Settings.Default.LyraNode.Lyra.NetworkId;
                result.ResultCode = APIResultCodes.Success;
            }
            catch (Exception e)
            {
                result.ResultCode = APIResultCodes.UnknownError;
            }
            return Task.FromResult(result);
        }

        public Task<GetTokenNamesAPIResult> GetTokenNames(string AccountId, string Signature, string keyword)
        {
            var result = new GetTokenNamesAPIResult();

            try
            {
                //if (!BlockChain.Singleton.AccountExists(AccountId))
                //    result.ResultCode = APIResultCodes.AccountDoesNotExist;

                var blocks = BlockChain.Singleton.FindTokenGenesisBlocks(keyword == "(null)" ? null : keyword);
                if (blocks != null)
                {
                    result.TokenNames = blocks.Select(a => a.Ticker).ToList();
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                    result.ResultCode = APIResultCodes.TokenGenesisBlockNotFound;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetTokenNames: " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<AccountHeightAPIResult> GetAccountHeight(string AccountId, string Signature)
        {
            var result = new AccountHeightAPIResult();
            try
            {
                if (BlockChain.Singleton.AccountExists(AccountId))
                {
                    result.Height = BlockChain.Singleton.FindLatestBlock(AccountId).Index;
                    result.NetworkId = Neo.Settings.Default.LyraNode.Lyra.NetworkId;
                    result.SyncHash = BlockChain.Singleton.GetSyncBlock().Hash;
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                {
                    result.ResultCode = APIResultCodes.AccountDoesNotExist;
                }
            }
            catch (Exception e)
            {
                result.ResultCode = APIResultCodes.UnknownError;
            }
            return Task.FromResult(result);
        }

        public Task<BlockAPIResult> GetBlockByIndex(string AccountId, long Index, string Signature)
        {
            var result = new BlockAPIResult();

            try
            {
                if (BlockChain.Singleton.AccountExists(AccountId))
                {
                    var block = BlockChain.Singleton.FindBlockByIndex(AccountId, Index);
                    if (block != null)
                    {
                        result.BlockData = Json(block);
                        result.ResultBlockType = block.BlockType;
                        result.ResultCode = APIResultCodes.Success;
                    }
                    else
                        result.ResultCode = APIResultCodes.BlockNotFound;
                }
                else
                    result.ResultCode = APIResultCodes.AccountDoesNotExist;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetBlock: " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<BlockAPIResult> GetBlockByHash(string AccountId, string Hash, string Signature)
        {
            var result = new BlockAPIResult();

            try
            {
                if (!BlockChain.Singleton.AccountExists(AccountId))
                    result.ResultCode = APIResultCodes.AccountDoesNotExist;

                var block = BlockChain.Singleton.FindBlockByHash(AccountId, Hash);
                if (block != null)
                {
                    result.BlockData = Json(block);
                    result.ResultBlockType = block.BlockType;
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                    result.ResultCode = APIResultCodes.BlockNotFound;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetBlock(Hash): " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<NonFungibleListAPIResult> GetNonFungibleTokens(string AccountId, string Signature)
        {
            var result = new NonFungibleListAPIResult();

            try
            {
                if (!BlockChain.Singleton.AccountExists(AccountId))
                    result.ResultCode = APIResultCodes.AccountDoesNotExist;

                var list = BlockChain.Singleton.GetNonFungibleTokens(AccountId);
                if (list != null)
                {
                    result.ListDataSerialized = Json(list);
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                    result.ResultCode = APIResultCodes.NoNonFungibleTokensFound;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetNonFungibleTokens: " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<BlockAPIResult> GetTokenGenesisBlock(string AccountId, string TokenTicker, string Signature)
        {
            var result = new BlockAPIResult();

            try
            {
                //if (!BlockChain.Singleton.AccountExists(AccountId))
                //    result.ResultCode = APIResultCodes.AccountDoesNotExist;

                var block = BlockChain.Singleton.FindTokenGenesisBlock(null, TokenTicker);
                if (block != null)
                {
                    result.BlockData = Json(block);
                    result.ResultBlockType = block.BlockType;
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                    result.ResultCode = APIResultCodes.TokenGenesisBlockNotFound;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetTokenTokenGenesisBlock: " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<BlockAPIResult> GetLastServiceBlock(string AccountId, string Signature)
        {
            var result = new BlockAPIResult();

            try
            {
                if (!BlockChain.Singleton.AccountExists(AccountId))
                    result.ResultCode = APIResultCodes.AccountDoesNotExist;

                var block = BlockChain.Singleton.GetLastServiceBlock();
                if (block != null)
                {
                    result.BlockData = Json(block);
                    result.ResultBlockType = block.BlockType;
                    result.ResultCode = APIResultCodes.Success;
                }
                else
                    result.ResultCode = APIResultCodes.ServiceBlockNotFound;
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in GetLastServiceBlock: " + e.Message);
                result.ResultCode = APIResultCodes.UnknownError;
            }

            return Task.FromResult(result);
        }

        public Task<NewTransferAPIResult> LookForNewTransfer(string AccountId, string Signature)
        {
            NewTransferAPIResult transfer_info = new NewTransferAPIResult();
            try
            {
                SendTransferBlock sendBlock = BlockChain.Singleton.FindUnsettledSendBlock(AccountId);

                if (sendBlock != null)
                {
                    TransactionBlock previousBlock = BlockChain.Singleton.FindBlockByHash(sendBlock.PreviousHash);
                    if (previousBlock == null)
                        transfer_info.ResultCode = APIResultCodes.CouldNotTraceSendBlockChain;
                    else
                    {
                        transfer_info.Transfer = sendBlock.GetTransaction(previousBlock); //CalculateTransaction(sendBlock, previousSendBlock);
                        transfer_info.SourceHash = sendBlock.Hash;
                        transfer_info.NonFungibleToken = sendBlock.NonFungibleToken;
                        transfer_info.ResultCode = APIResultCodes.Success;
                    }
                }
                else
                    transfer_info.ResultCode = APIResultCodes.NoNewTransferFound;
            }
            catch (Exception e)
            {
                transfer_info.ResultCode = APIResultCodes.UnknownError;
            }
            return Task.FromResult(transfer_info);
        }

        // util 
        private T FromJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }
        private string Json(object o)
        {
            return JsonConvert.SerializeObject(o);
        }

    }
}
