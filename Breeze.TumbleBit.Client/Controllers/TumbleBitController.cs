﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Breeze.TumbleBit.Client;
using Breeze.TumbleBit.Models;
using NBitcoin;
using Stratis.Bitcoin.Utilities.JsonErrors;

namespace Breeze.TumbleBit.Controllers
{
    /// <summary>
    /// Controller providing TumbleBit operations.
    /// </summary>
    [Route("api/[controller]")]
    public class TumbleBitController : Controller
    {
        private readonly ITumbleBitManager tumbleBitManager;

        public TumbleBitController(ITumbleBitManager tumbleBitManager)
        {
            this.tumbleBitManager = tumbleBitManager;
        }

        /// <summary>
        /// Connect to a tumbler.
        /// </summary>
        [Route("connect")]
        [HttpGet]
        public async Task<IActionResult> ConnectAsync()
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            if (this.tumbleBitManager.IsConnected() || this.tumbleBitManager.IsTumbling())
            {
                // Do not want to connect again as it is unnecessary and can cause problems
                return this.Ok();
            }

            try
            {
                var tumblerParameters = await this.tumbleBitManager.ConnectToTumblerAsync();

                var parameterDictionary = new Dictionary<string, string>()
                {
                    ["tumbler"] = this.tumbleBitManager.TumblerAddress,
                    ["denomination"] = tumblerParameters.Denomination.ToString(),
                    ["fee"] = tumblerParameters.Fee.ToString(),
                    ["network"] = tumblerParameters.Network.Name,
                    ["estimate"] = "10080"
                };

                return this.Json(parameterDictionary);
            }
            catch (Exception e)
            {                
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, $"An error occured connecting to the tumbler with uri {this.tumbleBitManager.TumblerAddress}.", e.ToString());
            }
        }

        /// <summary>
        /// Connect to a tumbler.
        /// </summary>
        [Route("tumble")]
        [HttpPost]
        public async Task<IActionResult> TumbleAsync([FromBody] TumbleRequest request)
        {
            // checks the request is valid
            if (!this.ModelState.IsValid)
            {
                var errors = this.ModelState.Values.SelectMany(e => e.Errors.Select(m => m.ErrorMessage));
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Formatting error", string.Join(Environment.NewLine, errors));
            }

            if (this.tumbleBitManager.IsTumbling())
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "Already started tumbling", "");
            }

            try
            {
                await this.tumbleBitManager.TumbleAsync(request.OriginWalletName, request.DestinationWalletName, request.OriginWalletPassword);
                return this.Ok();
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "An error occured starting tumbling session.", e.ToString());
            }
        }

        /// <summary>
        /// Is tumbler tumbling.
        /// </summary>
        [Route("is_tumbling")]
        [HttpGet]
        public async Task<IActionResult> IsTumblingAsync()
        {
            try
            {
                bool result = await this.tumbleBitManager.IsTumblingAsync();
                return this.Json(result);
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "An error occured during IsTumbling request.", e.ToString());
            }
        }

        /// <summary>
        /// Stop the tumbler.
        /// </summary>
        [Route("stop")]
        [HttpGet]
        public async Task<IActionResult> StopAsync()
        {
            try
            {
                await this.tumbleBitManager.StopAsync();
                return this.Ok();
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, "An error occured during Stop request.", e.ToString());
            }
        }

        /// <summary>
        /// Connect to a tumbler.
        /// </summary>
        [Route("watchonly_balances")]
        [HttpPost]
        public async Task<IActionResult> GetWatchOnlyBalancesAsync()
        {
            try
            {
                var watchOnlyBalances = await this.tumbleBitManager.GetWatchOnlyBalances();

                var parameterDictionary = new Dictionary<string, string>()
                {
                    ["confirmed"] = watchOnlyBalances.Confirmed.ToString(),
                    ["unconfirmed"] = watchOnlyBalances.Unconfirmed.ToString()
                };
                return this.Json(parameterDictionary);
            }
            catch (Exception e)
            {
                return ErrorHelpers.BuildErrorResponse(HttpStatusCode.BadRequest, $"An error occured retrieving the watch only balances.", e.ToString());
            }
        }
    }
}
