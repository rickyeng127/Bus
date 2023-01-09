using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FORT.Bus.message;

namespace FORT.Bus {
    public interface IMessagePublisher {

        void send(MappedMessage mappedMessage);
        void shutdown();
    }
}
