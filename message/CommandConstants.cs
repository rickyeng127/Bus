using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    public class CommandConstants {
        
        /**
         * Enumeration for commands and command responses messages
         **/
        public enum Commands {
            REGISTER_SECURITY_PRICE_FEED = 1,
            REGISTER_SECURITY_PRICE_FEED_RESPONSE = 2,
            UNREGISTER_SECURITY_PRICE_FEED = 3,
            UNREGISTER_SECURITY_PRICE_FEED_RESPONSE = 4,
            REQUEST_PRICE_SNAPSHOT = 5,
            REQUEST_PRICE_SNAPSHOT_RESPONSE = 6,
            PLACE_ORDERSET = 7,
            PLACE_ORDERSET_RESPONSE = 8,
            READ_NEW_FILLS = 9,
            READ_NEW_FILLS_RESPONSE = 10,
            REGISTER_CQG_REALTIME_PRICE = 11,
            UNREGISTER_CQG_REALTIME_PRICE = 12,
            REQUEST_CQG_HISTO_PRICE = 13,
            STATIC_INTERNAL_REQUEST = 14,
            STATIC_INTERNAL_RESPONSE = 15,
            REGISTER_ORDER_LIST = 16,    // RENG:1/12/16:Used to request remaining, pending or all market orders from AutoTrade 2.0
            REGISTER_ORDER_LIST_RESPONSE = 17,
            GET_REGION_SECTOR_LIST = 18,
            GET_REGION_SECTOR_LIST_RESPONSE = 19,
            GET_SECURITY_LIST_FOR_REGION_SECTOR = 20,
            GET_SECURITY_LIST_FOR_REGION_SECTOR_RESPONSE = 21,
            GET_CURRENT_CNTR_SIGNALS = 22,
            GET_CURRENT_CNTR_SIGNALS_RESPONSE = 23,
            REGISTER_SPECIAL_REALTIME_REQUEST = 24,
            UNREGISTER_SPECIAL_REALTIME_REQUEST = 25,
            REGISTER_SPECIAL_REALTIME_REQUEST_RESPONSE = 26,
            UNREGISTER_SPECIAL_REALTIME_REQUEST_RESPONSE = 27,
            GET_NEW_LIMIT_ORDER_SET = 28,
            GET_NEW_LIMIT_ORDER_SET_RESPONSE = 29,
            GET_NEW_MR1_ORDER_SET = 30,
            GET_NEW_MR1_ORDER_SET_RESPONSE = 31,
            BROKER_CHANGE = 32,          // Daniel:8/10/16: added for HAI's trades scheduler app
            COPY_EQUITY_ALLOC = 33,
            COPY_EQUITY_ALLOC_RESPONSE = 34,
            REGISTER_SECURITY_DOM_FEED = 35,
            REGISTER_SECURITY_DOM_FEED_RESPONSE = 36,
            UNREGISTER_SECURITY_DOM_FEED = 37,
            UNREGISTER_SECURITY_DOM_FEED_RESPONSE = 38,
            REGISTER_SECURITY_INDICATIVE_OPEN_FEED = 39,
            REGISTER_SECURITY_INDICATIVE_OPEN_FEED_RESPONSE = 40,
            UNREGISTER_SECURITY_INDICATIVE_OPEN_FEED = 41,
            UNREGISTER_SECURITY_INDICATIVE_OPEN_FEED_RESPONSE = 42,
            CHECK_ORDER = 43, // RENG:8/29/17:To enabling order checking within Equity QFIX
            CHECK_ORDER_RESPONSE = 44,
        }

        /// <summary>
        /// Enumeration for OrderSet categories
        /// </summary>
        public enum OrderSetCategory
        {
            NEW_LIMIT,
            NEW_FX_LIMIT,
            NEW_MR1,
            NEW_TRND,
            NEW_TRND_STRIP,
            NEW_CNTR_STRIP,
            RBL_AND_POST_RBL_LIMIT,
            STOP_AND_POST_STOP_LIMIT,
            HILO_AND_POST_HILO_LIMIT,
            ROLL,
            UPDATE_ORDERS,
            EQT,
            MAN_RBL_POST_LIMIT, // RENG:11/16/16:added new code for post rebalance limit orders following a manual rebalance!
            RBL,                // RENG:3/30/17:AT2-225 - Creating Hedging Strategy For UCITS Cash Balances
            NEW_MR2,           // RENG:10/31/17:AT2-271 - Add MR3 and MR2 Short Term Mean Reversion Strategies
            NEW_MR3            // RENG:10/31/17:AT2-271 - Add MR3 and MR2 Short Term Mean Reversion Strategies
        }

        /// <summary>
        /// Enumeration for info messages
        /// </summary>
        public enum Info {
            APPLICATION_RESTARTED = 100,
            HEARTBEAT = 101,
            SHUTDOWN = 102,
            PRICE_FEED_DISRUPTED = 103,
            UPCOMING_MARKET_ORDER_ALERT = 104,  // RENG:1/12/16:Info message to alert applications (Trades Watcher Satellite) of an upcoming market order
            MARKET_ORDER_SIGNALED = 105,        // RENG:1/12/16:Info message to alert applications (Trades Watcher Satellite) that a market order has been sent to QFIX
            MARKET_ORDER_RESCHEDULED = 106,     // RENG:1/12/16:Info message to alert applications (Trades Watcher Satellite) that an upcoming market order has been rescueduled (due to order aggreation logic)
            REMAINING_ORDER_LIST = 107,         // RENG:1/12/16:Info message current remaining orders in Trades Watcher
            PENDING_ORDER_LIST = 108,           // RENG:1/12/16:Info message current pending orders in Trades Watcher
            ALL_ORDER_LIST = 109,               // RENG:1/12/16:Info message for all signaled orders in Trades Watcher
            ALL_ORDER_LIST_INCREMENT = 110,     // RENG:1/12/16:Info message for incremental signaled orders in Trades Watcher
            CURRENT_CNTR_SIGNALS = 111,         // RENG:2/26/16:Info message for AT2's current contrarian signals
            UNMATCHED_HILO = 112,               // Daniel:7/9/16:Info message for sending unmatched hilo to autotrade
            TRIGGER_CREATED = 113               // RENG:
        }

        /**
         * Enumeration for applications that are sending/receiving commands
         **/
        public enum Applications {
            TICK_PROGRAM = 1,
            TICK_WRITER = 2,
            AUTO_TRADE = 3,
            QFIX = 4,
            AUTOTRADE_SIMULATOR = 5,
            ROLL_PROGRAM = 6,
            PRICE_CHART = 7,
            CQGFeed = 8,
            TP_TESTER = 9,
            EQUITY_QFIX = 10,
            TRADES_WATCHER_SATELLITE = 11,  // RENG:1/12/16:Remote view of Trades Watcher screen in AutoTrade 2.0
            PNLAPP = 12,                    // RENG:2/25/16:App used to monitor PNL 
            FP = 13,                        // Daniel:3/10/16:forward point app 
            EQM = 14,                       // Daniel:3/10/16:equity manager app 
            EQUITY_COV = 15,                // Daniel:5/10/16:equity coverage app
            HILO_RECORDER = 16,             // Daniel:7/9/16:hilo recorder app
            TRADES_SCHEDULER = 17,          // Daniel:8/10/16: added for HAI's trades scheduler app
            EQUITY_MATCHING_ENGINE = 18,    // RENG:3/9/17: Added test help test Equity Manager
            EQUITY_AIDE                     // RENG:3/12/18: Equity AI Decision Engine
        }
    }
}
