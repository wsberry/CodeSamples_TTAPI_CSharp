using System;

using slx.tt;

namespace TTAPI_Sample_Console_TimeAndSales
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
        private TimeAndSalesSubscription m_ts = null;

      
        public override void RunWorkers()
        {
            // lookup an instrument
            m_req = new InstrumentLookupSubscription(tt_api.Session, Dispatcher.Current,
                new ProductKey(MarketKey.Cme, ProductType.Future, "ES"),
                "Jun13");
            m_req.Update += new EventHandler<InstrumentLookupSubscriptionEventArgs>(m_req_Update);
            m_req.Start();
        }

        public override void OnWorkResult(object sender, EventArgs e)
        {
            throw new NotImplementedException();
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

                // Subscribe for Time & Sales Data
                m_ts = new TimeAndSalesSubscription(e.Instrument, Dispatcher.Current);
                m_ts.Update += new EventHandler<TimeAndSalesEventArgs>(m_ts_Update);
                m_ts.Start();
            }
            else if (e.IsFinal)
            {
                // Instrument was not found and TT API has given up looking for it
                Console.WriteLine("Cannot find instrument: {0}", e.Error.Message);
                Dispose();
            }
        }

        /// <summary>
        /// Event notification for Time & Sales update
        /// </summary>
        void m_ts_Update(object sender, TimeAndSalesEventArgs e)
        {
            if (e.Error == null)
            {
                // More than one LTP/LTQ may be received in a single event
                foreach (TimeAndSalesData tsData in e.Data)
                {
                    Price ltp = tsData.TradePrice;
                    Quantity ltq = tsData.TradeQuantity;
                    Console.WriteLine("LTP = {0} : LTQ = {1}", ltp.ToString(), ltq.ToInt());
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
                if (m_ts != null)
                {
                    m_ts.Update -= m_ts_Update;
                    m_ts.Dispose();
                    m_ts = null;
                }
                if (m_req != null)
                {
                    m_req.Update -= m_req_Update;
                    m_req.Dispose();
                    m_req = null;
                }

                m_disposed = true;
            }
        }

    }
}
