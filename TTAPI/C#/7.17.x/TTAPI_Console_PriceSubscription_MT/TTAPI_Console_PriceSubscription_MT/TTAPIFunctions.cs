using System;
using System.Collections.Generic;
using System.Threading;

using slx.tt;

namespace TTAPI_Console_PriceSubscription_MT
{
    using TradingTechnologies.TTAPI;

    /// <summary>
    /// Main TT API class
    /// </summary>
    class TTAPIFunctions : TTAPIAdapter
    {
        public override void RunWorkers()
        {
            // Start Time & Sales subscriptions on a separate thread
            var lcd1 = new List<ContractDetails>
            {
                new ContractDetails(MarketKey.Cme, ProductType.Future, "ES", "Dec13"),
                new ContractDetails(MarketKey.Cme, ProductType.Future, "NQ", "Dec13")
            };

            var s1 = new Strategy1(tt_api, lcd1);
            var workerThread1 = new Thread(s1.Start) {Name = "Strategy 1 Thread"};
            workerThread1.Start();

            // Start more Time & Sales subscriptions on a separate thread
            var lcd2 = new List<ContractDetails>
            {
                new ContractDetails(MarketKey.Cbot, ProductType.Future, "ZB", "Dec13"),
                new ContractDetails(MarketKey.Cbot, ProductType.Future, "ZN", "Dec13")
            };

            var s2 = new Strategy2(tt_api, lcd2);
            var workerThread2 = new Thread(s2.Start) {Name = "Strategy 2 Thread"};
            workerThread2.Start();
        }

        public override void OnWorkResult(object sender, EventArgs e)
        {
           
        }
    }

    /// <summary>
    /// struct for encapsulating contract details
    /// </summary>
    public struct ContractDetails
    {
        public MarketKey MarketKey;
        public ProductType ProductType;
        public string Product;
        public string Contract;

        public ContractDetails(MarketKey mk, ProductType pt, string prod, string cont)
        {
            MarketKey = mk;
            ProductType = pt;
            Product = prod;
            Contract = cont;
        }
    }
}
