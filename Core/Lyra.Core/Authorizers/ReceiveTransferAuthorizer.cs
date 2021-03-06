﻿using System;
using System.Collections.Generic;
using Lyra.Core.Blocks;
using Lyra.Core.API;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Lyra.Core.Utils;
using Lyra.Core.Accounts;

namespace Lyra.Core.Authorizers
{
    public class ReceiveTransferAuthorizer: BaseAuthorizer
    {
        public ReceiveTransferAuthorizer()
        {
        }

        public override (APIResultCodes, AuthorizationSignature) Authorize<T>(T tblock)
        {
            var result = AuthorizeImpl(tblock);
            if (APIResultCodes.Success == result)
                return (APIResultCodes.Success, Sign(tblock));
            else
                return (result, (AuthorizationSignature)null);
        }
        private APIResultCodes AuthorizeImpl<T>(T tblock)
        {
            if (!(tblock is ReceiveTransferBlock))
                return APIResultCodes.InvalidBlockType;

            var block = tblock as ReceiveTransferBlock;

            // 1. check if the account already exists
            if (!BlockChain.Singleton.AccountExists(block.AccountID))
                return APIResultCodes.AccountDoesNotExist;

            TransactionBlock lastBlock = BlockChain.Singleton.FindLatestBlock(block.AccountID);
            if (lastBlock == null)
                return APIResultCodes.CouldNotFindLatestBlock;

            var result = VerifyBlock(block, lastBlock);
            if (result != APIResultCodes.Success)
                return result;

            result = VerifyTransactionBlock(block);
            if (result != APIResultCodes.Success)
                return result;

            if (!block.ValidateTransaction(lastBlock))
                return APIResultCodes.ReceiveTransactionValidationFailed;

            result = ValidateReceiveTransAmount(block, block.GetTransaction(lastBlock));
            if (result != APIResultCodes.Success)
                return result;

            result = ValidateNonFungible(block, lastBlock);
            if (result != APIResultCodes.Success)
                return result;

            // Check duplicate receives (kind of double spending up down)
            var duplicate_block = BlockChain.Singleton.FindBlockBySourceHash(block.SourceHash);
            if (duplicate_block != null)
                return APIResultCodes.DuplicateReceiveBlock;

            return APIResultCodes.Success;
        }

        protected override APIResultCodes ValidateFee(TransactionBlock block)
        {
            if (block.FeeType != AuthorizationFeeTypes.NoFee)
                return APIResultCodes.InvalidFeeAmount;

            if (block.Fee != 0)
                return APIResultCodes.InvalidFeeAmount;

            return APIResultCodes.Success;
        }


        protected APIResultCodes ValidateReceiveTransAmount(ReceiveTransferBlock block, TransactionInfo receiveTransaction)
        {
            //find the corresponding send block and validate the added transaction amount
            var sourceBlock = BlockChain.Singleton.FindBlockByHash(block.SourceHash);
            if (sourceBlock == null)
                return APIResultCodes.SourceSendBlockNotFound;


            // find the actual amount of transaction 
            TransactionInfo sendTransaction;
            if (block.BlockType == BlockTypes.ReceiveTransfer || block.BlockType == BlockTypes.OpenAccountWithReceiveTransfer)
            {
                if ((sourceBlock as SendTransferBlock).DestinationAccountId != block.AccountID)
                    return APIResultCodes.InvalidDestinationAccountId;

                TransactionBlock prevToSendBlock = BlockChain.Singleton.FindBlockByHash(sourceBlock.PreviousHash);
                if (prevToSendBlock == null)
                    return APIResultCodes.CouldNotTraceSendBlockChain;

                sendTransaction = sourceBlock.GetTransaction(prevToSendBlock);

                if (!sourceBlock.ValidateTransaction(prevToSendBlock))
                    return APIResultCodes.SendTransactionValidationFailed;
                //originallySentAmount = sendTransaction.Amount;
                //originallySentAmount = 
                //    prevToSendBlock.Balances[LyraGlobal.LYRA_TICKER_CODE] - sourceBlock.Balances[LyraGlobal.LYRA_TICKER_CODE] - (sourceBlock as IFeebleBlock).Fee;
            }
            else
            if (block.BlockType == BlockTypes.ReceiveFee || block.BlockType == BlockTypes.OpenAccountWithReceiveFee)
            {
                sendTransaction = new TransactionInfo() { TokenCode = LyraGlobal.LYRATICKERCODE, Amount = sourceBlock.Fee };
            }
            else
                return APIResultCodes.InvalidBlockType;

            if (sendTransaction.Amount != receiveTransaction.Amount)
                return APIResultCodes.TransactionAmountDoesNotMatch;

            if (sendTransaction.TokenCode != receiveTransaction.TokenCode)
                return APIResultCodes.TransactionTokenDoesNotMatch;

            return APIResultCodes.Success;
        }

        protected override APIResultCodes ValidateNonFungible(TransactionBlock send_or_receice_block, TransactionBlock previousBlock)
        {
            var result = base.ValidateNonFungible(send_or_receice_block, previousBlock);
            if (result != APIResultCodes.Success)
                return result;

            if (send_or_receice_block.NonFungibleToken == null)
                return APIResultCodes.Success;

            var originBlock = BlockChain.Singleton.FindBlockByHash((send_or_receice_block as ReceiveTransferBlock).SourceHash);
            if (originBlock == null)
                return APIResultCodes.OriginNonFungibleBlockNotFound;

            if (!originBlock.ContainsNonFungibleToken())
                return APIResultCodes.OriginNonFungibleBlockNotFound;

            if (originBlock.NonFungibleToken.Hash != send_or_receice_block.NonFungibleToken.Hash)
                return APIResultCodes.OriginNonFungibleBlockHashDoesNotMatch;

            return APIResultCodes.Success;
        }


    }
}
