﻿using NBitcoin;
using System;
using Microsoft.Extensions.Logging;
using NTumbleBit.Logging;

namespace NTumbleBit
{
    public abstract class EscrowReceiver : IEscrow
    {
		public class State
		{
			public ScriptCoin EscrowedCoin
			{
				get;
				set;
			}
			public Key EscrowKey
			{
				get;
				set;
			}

			/// <summary>
			/// Identify the channel to the tumbler
			/// </summary>
			public uint160 ChannelId
			{
				get;
				set;
			}
		}

		protected State InternalState
		{
			get; set;
		}

		public uint160 Id
		{
			get
			{
				return InternalState.ChannelId;
			}
		}

		public void SetChannelId(uint160 channelId)
		{
            InternalState.ChannelId = channelId ?? throw new ArgumentNullException(nameof(channelId));
		}
		public virtual void ConfigureEscrowedCoin(ScriptCoin escrowedCoin, Key escrowKey)
		{
            InternalState.EscrowKey = escrowKey ?? throw new ArgumentNullException(nameof(escrowKey));
			InternalState.EscrowedCoin = escrowedCoin ?? throw new ArgumentNullException(nameof(escrowedCoin));

		    Logs.Tumbler.LogDebug($"EscrowKey : { InternalState.EscrowKey }, EscrowedCoin : {InternalState.EscrowedCoin}");
        }

		public ScriptCoin EscrowedCoin
		{
			get
			{
				return InternalState.EscrowedCoin;
			}
		}
	}
}
