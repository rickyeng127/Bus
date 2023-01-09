using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FORT.Bus.message;
using FORT.Bus.impl.message;

namespace FORT.Bus {
    public interface IMessageListener {

        void NotifyNewMessage(MappedMessage mappedMessage);
        void NotifyMessageLost(LostMessageStatus lostMessageStatus);
    }
}
