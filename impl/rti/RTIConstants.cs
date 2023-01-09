using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.impl.rti {
    public class RTIConstants {

        public static class QOSLibraryNames {
            public static readonly string DEFAULT = "DefaultLibrary";
        }

        public static class QOSProfileNames {
            public static readonly string RELIABLE = "Reliable";
            public static readonly string HIGH_THROUGHPUT = "HighThroughput";
            public static readonly string LOSSY_NETWORK = "LossyNetwork";
            public static readonly string LOW_LATENCY = "LowLatency";
        }
    }
}
