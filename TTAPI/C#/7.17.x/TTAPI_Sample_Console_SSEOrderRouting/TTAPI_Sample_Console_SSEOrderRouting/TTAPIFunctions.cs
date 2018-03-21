﻿using System;
using slx.tt;

namespace TTAPI_Sample_Console_SSEOrderRouting
{
    using TradingTechnologies.TTAPI;
    using TradingTechnologies.TTAPI.Tradebook;

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
        private InstrumentTradeSubscription m_ts = null;
        private string m_orderKey = "";
        private bool m_changed = false;

        public override void RunWorkers()
        {
            // lookup an instrument
            m_req = new InstrumentLookupSubscription(tt_api.Session, Dispatcher.Current,
                new ProductKey(MarketKey.Cme, ProductType.Future, "ES"),
                "Mar13");
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

                // Subscribe for Inside Market Data
                m_ps = new PriceSubscription(e.Instrument, Dispatcher.Current);
                m_ps.Settings = new PriceSubscriptionSettings(PriceSubscriptionType.InsideMarket);
                m_ps.FieldsUpdated += new FieldsUpdatedEventHandler(m_ps_FieldsUpdated);
                m_ps.Start();

                // Create a TradeSubscription to listen for order / fill events only for orders submitted through it
                m_ts = new InstrumentTradeSubscription(tt_api.Session, Dispatcher.Current, e.Instrument, true, true, false, false);
                m_ts.OrderUpdated += new EventHandler<OrderUpdatedEventArgs>(m_ts_OrderUpdated);
                m_ts.OrderAdded += new EventHandler<OrderAddedEventArgs>(m_ts_OrderAdded);
                m_ts.OrderDeleted += new EventHandler<OrderDeletedEventArgs>(m_ts_OrderDeleted);
                m_ts.OrderFilled += new EventHandler<OrderFilledEventArgs>(m_ts_OrderFilled);
                m_ts.OrderRejected += new EventHandler<OrderRejectedEventArgs>(m_ts_OrderRejected);
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
        /// Event notification for price update
        /// </summary>
        void m_ps_FieldsUpdated(object sender, FieldsUpdatedEventArgs e)
        {
            if (e.Error == null)
            {
                // Make sure that there is a valid bid
                if (e.Fields.GetBestBidPriceField().HasValidValue)
                {
                    if (m_orderKey == "")
                    {
                        // If there is no order working, submit one through the first valid order feed.
                        // You should use the order feed that is valid for your purposes.
                        OrderProfile op = new OrderProfile(e.Fields.Instrument.GetValidOrderFeeds()[0], e.Fields.Instrument);
                        op.BuySell = BuySell.Buy;
                        op.AccountName = "12345678";
                        op.AccountType = AccountType.A1;
                        op.OrderQuantity = Quantity.FromInt(e.Fields.Instrument, 100);
                        op.OrderType = OrderType.Limit;
                        op.LimitPrice = e.Fields.GetBestBidPriceField().Value;

                        op.SlicerType = SlicerType.TimeSliced;
                        op.DisclosedQuantity = Quantity.FromInt(e.Fields.Instrument, 10);
                        op.DisclosedQuantityMode = QuantityMode.Quantity;
                        op.InterSliceDelay = 10;
                        op.InterSliceDelayTimeUnits = TimeUnits.Sec;
                        op.LeftoverAction = LeftoverAction.Leave;
                        op.LeftoverActionTime = LeftoverActionTime.AtEnd;
                        op.PriceMode = PriceMode.Absolute;

                        if (!m_ts.SendOrder(op))
                        {
                            Console.WriteLine("Send new order failed.  {0}", op.RoutingStatus.Message);
                            Dispose();
                        }
                        else
                        {
                            m_orderKey = op.SiteOrderKey;
                            Console.WriteLine("Send new order succeeded.");
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
        /// Event notification for order rejected
        /// </summary>
        void m_ts_OrderRejected(object sender, OrderRejectedEventArgs e)
        {
            if (e.Order.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been rejected
                Console.WriteLine("Our parent order has been rejected: {0}", e.Message);
            }
            else if (e.Order.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been rejected
                Console.WriteLine("A child order of our parent order has been rejected: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event notification for order filled
        /// </summary>
        void m_ts_OrderFilled(object sender, OrderFilledEventArgs e)
        {
            if (e.Fill.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been filled
                Console.WriteLine("Our parent order has been " + (e.Fill.FillType == FillType.Full ? "fully" : "partially") + " filled");
            }
            else if (e.Fill.ParentKey == m_orderKey)
            {
                // A child order of our parent order has been filled
                Console.WriteLine("A child order of our parent order has been " + (e.Fill.FillType == FillType.Full ? "fully" : "partially") + " filled");
            }

            Console.WriteLine("Average Buy Price = {0} : Net Position = {1} : P&L = {2}", m_ts.ProfitLossStatistics.BuyAveragePrice,
                m_ts.ProfitLossStatistics.NetPosition, m_ts.ProfitLoss.AsPrimaryCurrency);
        }

        /// <summary>
        /// Event notification for order deleted
        /// </summary>
        void m_ts_OrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            if (e.DeletedUpdate.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been deleted
                Console.WriteLine("Our parent order has been deleted: {0}", e.Message);
            }
            else if (e.DeletedUpdate.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been deleted
                Console.WriteLine("A child order of our parent order has been deleted: {0}", e.Message);
            }
        }

        /// <summary>
        /// Event notification for order added
        /// </summary>
        void m_ts_OrderAdded(object sender, OrderAddedEventArgs e)
        {
            if (e.Order.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been added
                Console.WriteLine("Our parent order has been added: {0}", e.Message);
            }
            else if (e.Order.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been added
                Console.WriteLine("A child order of our parent order has been added: {0}", e.Message);


                // When half of the order quantity has been disclosed, reduce the price of all
                // subsequent child orders by 1 tick by reducing the parent order by 1 tick
                SseSyntheticOrder sseOrder = m_ts.Orders[m_orderKey] as SseSyntheticOrder;
                if (m_changed == false && sseOrder.UndisclosedQuantity <= (sseOrder.OrderQuantity / 2))
                {
                    OrderProfile op = sseOrder.GetOrderProfile() as OrderProfile;
                    op.LimitPrice--;
                    op.Action = OrderAction.Change;

                    if (!m_ts.SendOrder(op))
                    {
                        Console.WriteLine("Send change order failed.  {0}", op.RoutingStatus.Message);
                    }
                    else
                    {
                        Console.WriteLine("Send change order succeeded.");
                        m_changed = true;
                    }
                }
            }
        }

        /// <summary>
        /// Event notification for order update
        /// </summary>
        void m_ts_OrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            if (e.OldOrder.SiteOrderKey == m_orderKey)
            {
                // Our parent order has been updated
                Console.WriteLine("Our parent order has been updated: {0}", e.Message);
            }
            else if (e.OldOrder.SyntheticOrderKey == m_orderKey)
            {
                // A child order of our parent order has been updated
                Console.WriteLine("A child order of our parent order has been updated: {0}", e.Message);
            }
        }

        /// <summary>
        /// Shuts down the TT API
        /// </summary>
        public override void OnDispose()
        {
            lock(m_lock)
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
                if (m_ts != null)
                {
                    m_ts.OrderAdded -= m_ts_OrderAdded;
                    m_ts.OrderDeleted -= m_ts_OrderDeleted;
                    m_ts.OrderFilled -= m_ts_OrderFilled;
                    m_ts.OrderRejected -= m_ts_OrderRejected;
                    m_ts.OrderUpdated -= m_ts_OrderUpdated;
                    m_ts.Dispose();
                    m_ts = null;
                }

                m_disposed = true;
            }
        }

       
    }
}
