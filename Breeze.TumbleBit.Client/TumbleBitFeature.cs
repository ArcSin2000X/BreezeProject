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
using Stratis.Bitcoin.Features.Wallet;
using Stratis.Bitcoin.Features.LightWallet;

namespace Breeze.TumbleBit
{
    public class TumbleBitFeature : FullNodeFeature
    {
        private readonly ITumbleBitManager tumbleBitManager;

        public TumbleBitFeature(ITumbleBitManager tumbleBitManager)
        {
            this.tumbleBitManager = tumbleBitManager;
        }

        public override void Start()
        {
            this.tumbleBitManager.Initialize();
        }

        public override void Stop()
        {
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
