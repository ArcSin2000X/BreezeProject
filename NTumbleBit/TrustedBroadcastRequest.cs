﻿using NBitcoin;
using NTumbleBit.PuzzleSolver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NTumbleBit
{
	public class TrustedBroadcastRequest
	{
		public static readonly byte[] PlaceholderSignature = new byte[71];

		public Script PreviousScriptPubKey
		{
			get; set;
		}
		public OutPoint SignedOutpoint
		{
			get;
			set;
		}

		public Script Signature
		{
			get; set;
		}
		public Transaction Transaction
		{
			get; set;
		}
		public Key Key
		{
			get; set;
		}
		public LockTime BroadcastAt
		{
			get;
			set;
		}
		public bool IsBroadcastableAt(int height)
		{
			return height >= BroadcastAt.Height && Transaction.IsFinal(DateTimeOffset.UtcNow, height + 1);
		}

		/// <summary>
		/// Use BroadcastAt and Transaction locktime to know what a transaction can be broadcasted
		/// </summary>
		public int BroadcastableHeight
		{
			get
			{
				if(!Transaction.LockTime.IsHeightLock)
					return BroadcastAt.IsHeightLock ? BroadcastAt.Height : 0;
				if(!BroadcastAt.IsHeightLock)
					return Transaction.LockTime.Height;
				return Math.Max(Transaction.LockTime.Height, BroadcastAt.Height);
			}
		}

		public Transaction ReSign(Coin coin)
		{
			bool a;
			return ReSign(coin, out a);
		}
		public Transaction ReSign(Coin coin, out bool cached)
		{
			var transaction = Transaction.Clone();
			if(coin.Outpoint == SignedOutpoint)
			{
				transaction.Inputs[0].ScriptSig = Signature;
				transaction.Inputs[0].PrevOut = SignedOutpoint;
				cached = true;
				return transaction;
			}
			transaction.Inputs[0].PrevOut = coin.Outpoint;
			var redeem = new Script(transaction.Inputs[0].ScriptSig.ToOps().Last().PushData);
			var scriptCoin = coin.ToScriptCoin(redeem);
			byte[] signature = transaction.SignInput(Key, scriptCoin).ToBytes();
			List<Op> resignedScriptSig = new List<Op>();
			foreach(var op in transaction.Inputs[0].ScriptSig.ToOps())
			{
				resignedScriptSig.Add(IsPlaceholder(op) ? Op.GetPushOp(signature) : op);
			}
			Signature = new Script(resignedScriptSig.ToArray());
			SignedOutpoint = coin.Outpoint;
			transaction.Inputs[0].ScriptSig = Signature;
			cached = false;
			return transaction;
		}

		private static bool IsPlaceholder(Op op)
		{
			return op.PushData != null && op.PushData.SequenceEqual(PlaceholderSignature);
		}
	}
}
