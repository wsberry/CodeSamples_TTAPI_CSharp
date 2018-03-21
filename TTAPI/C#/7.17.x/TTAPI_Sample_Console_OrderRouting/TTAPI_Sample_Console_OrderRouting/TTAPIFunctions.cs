// **********************************************************************************************************************
//
//	Copyright © 2005-2013 Trading Technologies International, Inc.
//	All Rights Reserved Worldwide
//
// 	* * * S T R I C T L Y   P R O P R I E T A R Y * * *
//
//  WARNING: This file and all related programs (including any computer programs, example programs, and all source code) 
//  are the exclusive property of Trading Technologies International, Inc. (“TT”), are protected by copyright law and 
//  international treaties, and are for use only by those with the express written permission from TT.  Unauthorized 
//  possession, reproduction, distribution, use or disclosure of this file and any related program (or document) derived 
//  from it is prohibited by State and Federal law, and by local law outside of the U.S. and may result in severe civil 
//  and criminal penalties.
//
// ************************************************************************************************************************

using System;
using slx.tt;

namespace TTAPI_Sample_Console_OrderRouting
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
                m_ts = new InstrumentTradeSubscription(tt_api.Session, Dispatcher.Current, e.Instrument, true, true,
                    false, false);
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
                        OrderProfile op = new OrderProfile(e.Fields.Instrument.GetValidOrderFeeds()[0],
                            e.Fields.Instrument);
                        op.BuySell = BuySell.Buy;
                        op.AccountName = "12345678";
                        op.AccountType = AccountType.A1;
                        op.OrderQuantity = Quantity.FromInt(e.Fields.Instrument, 10);
                        op.OrderType = OrderType.Limit;
                        op.LimitPrice = e.Fields.GetBestBidPriceField().Value;

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
                    else if (m_ts.Orders.ContainsKey(m_orderKey) &&
                             m_ts.Orders[m_orderKey].LimitPrice != e.Fields.GetBestBidPriceField().Value)
                    {
                        // If there is a working order, reprice it if its price is not the same as the bid
                        OrderProfileBase op = m_ts.Orders[m_orderKey].GetOrderProfile();
                        op.LimitPrice = e.Fields.GetBestBidPriceField().Value;
                        op.Action = OrderAction.Change;

                        if (!m_ts.SendOrder(op))
                        {
                            Console.WriteLine("Send change order failed.  {0}", op.RoutingStatus.Message);
                        }
                        else
                        {
                            Console.WriteLine("Send change order succeeded.");
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
            Console.WriteLine("Order was rejected.");
        }

        /// <summary>
        /// Event notification for order filled
        /// </summary>
        void m_ts_OrderFilled(object sender, OrderFilledEventArgs e)
        {
            if (e.FillType == FillType.Full)
            {
                Console.WriteLine("Order was fully filled for {0} at {1}.", e.Fill.Quantity, e.Fill.MatchPrice);
            }
            else
            {
                Console.WriteLine("Order was partially filled for {0} at {1}.", e.Fill.Quantity, e.Fill.MatchPrice);
            }

            Console.WriteLine("Average Buy Price = {0} : Net Position = {1} : P&L = {2}",
                m_ts.ProfitLossStatistics.BuyAveragePrice,
                m_ts.ProfitLossStatistics.NetPosition, m_ts.ProfitLoss.AsPrimaryCurrency);
        }

        /// <summary>
        /// Event notification for order deleted
        /// </summary>
        void m_ts_OrderDeleted(object sender, OrderDeletedEventArgs e)
        {
            Console.WriteLine("Order was deleted.");
        }

        /// <summary>
        /// Event notification for order added
        /// </summary>
        void m_ts_OrderAdded(object sender, OrderAddedEventArgs e)
        {
            Console.WriteLine("Order was added with price of {0}.", e.Order.LimitPrice);
        }

        /// <summary>
        /// Event notification for order update
        /// </summary>
        void m_ts_OrderUpdated(object sender, OrderUpdatedEventArgs e)
        {
            Console.WriteLine("Order was updated with price of {0}.", e.NewOrder.LimitPrice);
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

                if (m_ts != null)
                {
                    m_ts.OrderUpdated -= m_ts_OrderUpdated;
                    m_ts.OrderAdded -= m_ts_OrderAdded;
                    m_ts.OrderDeleted -= m_ts_OrderDeleted;
                    m_ts.OrderFilled -= m_ts_OrderFilled;
                    m_ts.OrderRejected -= m_ts_OrderRejected;
                    m_ts.Dispose();
                    m_ts = null;
                }

                m_disposed = true;
            }
        }

    }
}
