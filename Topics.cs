using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus {

    /// <summary>
    /// The supported topics on the message bus
    /// </summary>
    public class Topics {

        public static readonly string TICK_DATA_STREAM = "TickDataStream";
        public static readonly string TICK_DATA_SNAP = "TickDataSnap";
        public static readonly string TICK_DOM_STREAM = "TickDomStream"; //daniel 20170310 added
        public static readonly string TICK_INDICATIVE_OPEN_STREAM = "TickIndicativeOpenStream"; //daniel 20170310 added
        //public static readonly string TICK_DATA_FULL = "TickDataFull"; //daniel 20160510 remove the TICK_DATA_FULL topic
        public static readonly string COMMAND = "Command";
        public static readonly string COMMAND_RESPONSE = "CommandResponse";
        public static readonly string INFO = "Info";
        public static readonly string HEARTBEAT = "Heartbeat";
        public static readonly string SPECIAL_RT_DATA = "SpecialRTData";
    }
}
