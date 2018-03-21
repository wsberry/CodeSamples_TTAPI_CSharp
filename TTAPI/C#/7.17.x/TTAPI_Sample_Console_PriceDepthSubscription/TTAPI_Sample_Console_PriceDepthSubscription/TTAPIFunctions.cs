using System;
using slx.tt;

namespace TTAPI_Sample_Console_PriceDepthSubscription
{
    using TradingTechnologies.TTAPI;

    class TTAPIFunctions : TTAPIAdapter
    {
        /// <summary>
        /// Declare the TTAPI objects
        /// </summary>
        private bool m_disposed = false;

        private object m_lock = new object();
        private InstrumentLookupSubscription m_req = null;
        private PriceSubscription m_ps = null;

        public override void RunWorkers()
        {
            // lookup an instrument
            m_req = new InstrumentLookupSubscription(tt_api.Session, Dispatcher.Current,
                new ProductKey(MarketKey.Cbot, ProductType.Future, "ZN"),
                "Sep13");
            m_req.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(m_req_Update);
            m_req.Start();
        }

        public override void OnWorkResult(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// Event notification for instrument lookup
        /// </summary>
        void m_req_Update(object sender, InstrumentLookupSubscriptionEventArgs e)
        {
            if (e.Instrument != null && e.Error == null)
            {
                // Instrument was found
                Console.WriteLine("Found: {0}", e.Instrument.Name);

                // Subscribe for Inside Market Data
                m_ps = new PriceSubscription(e.Instrument, Dispatcher.Current)
                {
                    Settings = new PriceSubscriptionSettings(PriceSubscriptionType.MarketDepth)
                };
                m_ps.FieldsUpdated += new FieldsUpdatedEventHandler(m_ps_FieldsUpdated);
                m_ps.Start();
            }
            else if (e.IsFinal)
            {
                // Instrument was not found and TT API has given up looking for it
                Console.WriteLine("Cannot find instrument: {0}", e.Error.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for price update
        /// </summary>
        void m_ps_FieldsUpdated(object sender, FieldsUpdatedEventArgs e)
        {
            if (e.Error == null)
            {
                if (e.UpdateType == UpdateType.Snapshot)
                {
                    // Received a market data snapshot

                    Console.WriteLine(@"Ask Depth Snapshot");
                    int askDepthLevels = e.Fields.GetLargestCurrentDepthLevel(FieldId.BestAskPrice);
                    for (int i = 0; i < askDepthLevels; i++)
                    {
                        Console.WriteLine("    Level={0} Qty={1} Price={2}", i,
                            e.Fields.GetBestAskQuantityField(i).Value.ToInt(),
                            e.Fields.GetBestAskPriceField(i).Value.ToString());
                    }

                    Console.WriteLine("Bid Depth Snapshot");
                    int bidDepthLevels = e.Fields.GetLargestCurrentDepthLevel(FieldId.BestBidPrice);
                    for (var i = 0; i < bidDepthLevels; i++)
                    {
                        Console.WriteLine("    Level={0} Qty={1} Price={2}", i,
                            e.Fields.GetBestBidQuantityField(i).Value.ToInt(),
                            e.Fields.GetBestBidPriceField(i).Value.ToString());
                    }
                }
                else
                {
                    // Only some fields have changed

                    Console.WriteLine("Depth Updates");
                    int depthLevels = e.Fields.GetLargestCurrentDepthLevel();
                    for (int i = 0; i < depthLevels; i++)
                    {
                        if (e.Fields.GetChangedFieldIds(i).Length > 0)
                        {
                            Console.WriteLine("Level={0}", i);
                            foreach (FieldId id in e.Fields.GetChangedFieldIds(i))
                            {
                                Console.WriteLine("    {0} : {1}", id.ToString(), e.Fields[id, i].FormattedValue);
                            }
                        }
                    }
                }
            }
            else
            {
                if (e.Error.IsRecoverableError == false)
                {
                    Console.WriteLine("Unrecoverable price subscription error: {0}", e.Error.Message);
                    Dispose();
                }
            }
        }

        /// <summary>
        /// Shuts down the TT API
        /// </summary>
        public override void OnDispose()
        {
            lock (m_lock)
            {
                if (m_disposed) return;

                // Unattached callbacks and dispose of all subscriptions
                if (m_req != null)
                {
                    m_req.Update -= m_req_Update;
                    m_req.Dispose();
                    m_req = null;
                }

                if (m_ps != null)
                {
                    m_ps.FieldsUpdated -= m_ps_FieldsUpdated;
                    m_ps.Dispose();
                    m_ps = null;
                }

                m_disposed = true;
            }
        }
    }
}
