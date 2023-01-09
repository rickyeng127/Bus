using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus {
    
    public interface IMessageSubscriber {

        void registerMessageListener(IMessageListener messageListener);
        void shutdown();

        /// <summary>
        /// If a subscriber supports content based filtering, 
        /// this method allows addition of a single filter
        /// </summary>
        /// <param name="filter"></param>
        void appendMessageFilter(string filter);
        
        /// <summary>
        /// If a subscriber supports content based filtering, 
        /// this method allows removal of a single filter
        /// </summary>
        /// <param name="filter"></param>
        void removeMessageFilter(string filter);
    }
}
