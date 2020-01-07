﻿using Lyra.Core.API;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lyra.Core.Decentralize
{
    public class BillBoard
    {
        public Dictionary<string, PosNode> AllNodes { get; private set; }

        public BillBoard()
        {
            AllNodes = new Dictionary<string, PosNode>();
        }

        public PosNode Add(string accountId)
        {
            PosNode node;
            if (AllNodes.ContainsKey(accountId))
                node = AllNodes[accountId];
            else
            {
                node = new PosNode(accountId);
                AllNodes.Add(accountId, node);
            }

            // lookup balance
            var block = BlockChain.Singleton.FindLatestBlock(node.AccountID);
            if (block != null && block.Balances.ContainsKey(LyraGlobal.LYRATICKERCODE))
            {
                node.Balance = block.Balances[LyraGlobal.LYRATICKERCODE];
            }

            return node;
        }
    }

    public class PosNode
    {
        public string AccountID { get; set; }
        public decimal Balance { get; set; }
        public DateTime LastStaking { get; set; }

        public PosNode(string accountId)
        {
            AccountID = accountId;
            LastStaking = DateTime.Now;
            Balance = 0;
        }
    }
}