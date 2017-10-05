﻿using System;
using System.Collections.Generic;
using Breeze.TumbleBit.Client;
using Breeze.TumbleBit.Controllers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stratis.Bitcoin.Features.Wallet.JsonConverters;
using NTumbleBit.JsonConverters;

using Stratis.Bitcoin.BlockPulling;
using Stratis.Bitcoin.Builder;
using Stratis.Bitcoin.Builder.Feature;
using Stratis.Bitcoin.Configuration;
using Stratis.Bitcoin.Connection;
using Stratis.Bitcoin.Features.BlockStore;
using Stratis.Bitcoin.Utilities;
using Stratis.Bitcoin.Signals;
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.LightWallet;
using NBitcoin;

namespace Breeze.TumbleBit
{
    public class TumbleBitFeature : FullNodeFeature
    {
        private readonly ITumbleBitManager tumbleBitManager;
        private readonly ConcurrentChain chain;
        private readonly Signals signals;

        private IDisposable blockSubscriberdDisposable;
        //private IDisposable transactionSubscriberdDisposable;

        public TumbleBitFeature(ITumbleBitManager tumbleBitManager, ConcurrentChain chain, Signals signals)
        {
            this.tumbleBitManager = tumbleBitManager;
            this.chain = chain;
            this.signals = signals;
        }

        public override void Start()
        {
            // subscribe to receiving blocks and transactions
            this.blockSubscriberdDisposable = this.signals.SubscribeForBlocks(new BlockObserver(this.chain, this.tumbleBitManager));
            //this.transactionSubscriberdDisposable = this.signals.SubscribeForTransactions(new TransactionObserver(this.tumbleBitManager));

            this.tumbleBitManager.Initialize();
        }

        public override void Stop()
        {
            this.blockSubscriberdDisposable.Dispose();
            //this.transactionSubscriberdDisposable.Dispose();

            this.tumbleBitManager?.Dispose();
        }
    }

    public static class TumbleBitFeatureExtension
    {
        public static IFullNodeBuilder UseTumbleBit(this IFullNodeBuilder fullNodeBuilder)
        {
            fullNodeBuilder.ConfigureFeature(features =>
            {
                features
                .AddFeature<TumbleBitFeature>()
                .FeatureServices(services =>
                    {
                        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                        {
                            Formatting = Formatting.Indented,                            
                            ContractResolver = new CamelCasePropertyNamesContractResolver(),
                            Converters = new List<JsonConverter>
                            {
                                new NetworkConverter(),
                                new PermutationTestProofConverter(),
                                new PoupardSternProofConverter(),
                                new RsaPubKeyConverter()
                            }
                        };

                        services.AddSingleton<ITumbleBitManager, TumbleBitManager> ();
                        services.AddSingleton<TumbleBitController>();
                        services.AddSingleton<IWalletFeePolicy, LightWalletFeePolicy>();
                    });
            });

            return fullNodeBuilder;
        }
    }
}