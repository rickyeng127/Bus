using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FORT.Bus.message;

namespace FORT.Bus {
    
    public interface IMessageBusFactory {

        IMessagePublisher createPublisher(int domainID, string topic, MappedMessage mappedMessage);
        IMessageSubscriber createSubscriber(int domainID, string topic, MappedMessage mappedMessage);

        /// <summary>
        /// Creates a content based filter using the fieldName and inital set of comma delimited filters
        /// Only a single field name is supported
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="mappedMessage"></param>
        /// <param name="filterFieldName">A field within the mappedMessage</param>
        /// <param name="initialFilters">comma delimited list of values to filter on</param>
        /// <returns></returns>
        IMessageSubscriber createSubscriber(int domainID, string topic, MappedMessage mappedMessage, string filterFieldName, string initialFilters);
    }
}
