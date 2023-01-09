using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FORT.Bus.impl.rti;
using FORT.Bus;
using FORT.Bus.message;

namespace FORT.Bus {

    public class MessageBusFactory : IMessageBusFactory {

        private RTIMessageBusFactory rti;
        private static MessageBusFactory instance;

        public static MessageBusFactory getInstance() {
            if (MessageBusFactory.instance == null) {
                MessageBusFactory.instance = new MessageBusFactory();
                MessageBusFactory.instance.initialize();
            }
            return MessageBusFactory.instance;
        }

        private MessageBusFactory() {
        }

        private void initialize() {
            this.rti = RTIMessageBusFactory.getInstance();
        }

        public IMessagePublisher createPublisher(int domainID, string topic, MappedMessage mappedMessage) {
            return rti.createPublisher(domainID, topic, mappedMessage);
        }

        public IMessageSubscriber createSubscriber(int domainID, string topic, MappedMessage mappedMessage) {
            return rti.createSubscriber(domainID, topic, mappedMessage);
        }

        public IMessageSubscriber createSubscriber(int domainID, string topic, MappedMessage mappedMessage, string filterFieldName, string initialFilters) {
            return rti.createSubscriber(domainID, topic, mappedMessage, filterFieldName, initialFilters);
        }

        public void shutdown() {
            MessageBusFactory.instance.rti.shutdown();
        }
    }
}
