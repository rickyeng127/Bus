using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    public interface IResponseListener {
        void NotifyNewResponse(Command command);
    }
}
