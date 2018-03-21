using System;
using slx.tt;

namespace TTAPI_Sample_Console_PriceSubscription
{
    using TradingTechnologies.TTAPI;

    /// <summary>
    /// Main TT API class
    /// </summary>
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
                new ProductKey(MarketKey.Cme, ProductType.Future, "CL"), "Apr18");
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
                m_ps = new PriceSubscription(e.Instrument, Dispatcher.Current);
                m_ps.Settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);
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
                    Console.WriteLine("Market Data Snapshot:");

                    foreach (FieldId id in e.Fields.GetFieldIds())
                    {
                        Console.WriteLine("    {0} : {1}", id.ToString(), e.Fields[id].FormattedValue);
                    }
                }
                else
                {
                    // Only some fields have changed
                    Console.WriteLine("Market Data Update:");

                    foreach (FieldId id in e.Fields.GetChangedFieldIds())
                    {
                        Console.WriteLine("    {0} : {1}", id.ToString(), e.Fields[id].FormattedValue);
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
