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
            STATIC_INTERNAL_RESPONSE = 15
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
            EQT
        }

        /// <summary>
        /// Enumeration for info messages
        /// </summary>
        public enum Info {
            APPLICATION_RESTARTED = 100,
            HEARTBEAT = 101,
            SHUTDOWN = 102,
            PRICE_FEED_DISRUPTED = 103        }

        /**
         * Enumeration for applications that are sending/receiving commands
         **/
        public enum Applications {
            TICK_PROGRAM = 1,
            TICK_WRITER = 2
        }
    }
}
