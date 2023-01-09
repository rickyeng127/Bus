using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.impl.message {
    public class LostMessageStatus {

        public int TotalMessagesLost {
            get;
            set;
        }

        public int NewMessagesLost {
            get;
            set;
        }

        public string LostMessageReason {
            get;
            set;
        }
    }
}
