using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using FORT.Bus.message;
using FORT.TickData.CQGData;
using FORT.Bus.impl.rti.message;

namespace FORT.Bus.impl.rti {

    public class RTIMessageBusFactory : IMessageBusFactory {

        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static RTIMessageBusFactory instance;

        /// <summary>
        /// DDS.DomainParticipant objects are heavy weight objects are should only be used to partition domains.
        /// These Domain participants are created with profiles
        /// </summary>
        private Dictionary<string, DDS.DomainParticipant> domainParticipantsWithProfile = new Dictionary<string, DDS.DomainParticipant>();

        /// <summary>
        /// DDS.DomainParticipant objects are heavy weight objects are should only be used to partition domains
        /// These Domain participants are created withou profiles
        /// </summary>
        private Dictionary<int, DDS.DomainParticipant> domainParticipants = new Dictionary<int, DDS.DomainParticipant>();

        private Dictionary<string, DDS.Topic> topics = new Dictionary<string, DDS.Topic>();

        private void initialize() {
            //DDS.DomainParticipantFactory fac = DDS.DomainParticipantFactory.get_instance();
            //DDS.DomainParticipantFactoryQos qos = new DDS.DomainParticipantFactoryQos();
            //fac.get_qos(qos);
            //int mot = qos.resource_limits.max_objects_per_thread;
            //qos.resource_limits.max_objects_per_thread = 4096;
            //mot = qos.resource_limits.max_objects_per_thread;
        }

        private RTIMessageBusFactory() {
        }

        public static RTIMessageBusFactory getInstance() {
            if (RTIMessageBusFactory.instance == null) {
                RTIMessageBusFactory.instance = new RTIMessageBusFactory();
                RTIMessageBusFactory.instance.initialize();
            }
            return RTIMessageBusFactory.instance;
        }

        private DDS.DomainParticipant getDomainParticipantWithProfile(int domainID, string qosLibraryName, string qosProfileName) {
            DDS.DomainParticipant ret = null;
            string key = String.Format("{0}-{1}-{2}", domainID, qosLibraryName, qosProfileName);
            if (this.domainParticipantsWithProfile.ContainsKey(key)) {
                ret = this.domainParticipantsWithProfile[key];
            } else {
                ret = DDS.DomainParticipantFactory.get_instance().create_participant_with_profile(
                                domainID,
                                qosLibraryName,
                                qosProfileName,
                                null, /* Listener */
                                DDS.StatusMask.STATUS_MASK_NONE);

                log.InfoFormat("Created domain participant on domainID = <{0}> : libraryName = <{1}> : profileName = {2}", domainID, qosLibraryName, qosProfileName);

                if (ret == null) {
                    throw new Exception("Unable to create domain participant");
                }

                this.domainParticipantsWithProfile.Add(key, ret);
            }
            return ret;
        }

        private DDS.DomainParticipant getDomainParticipant(int domainID) {
            DDS.DomainParticipant ret = null;
            if (this.domainParticipants.ContainsKey(domainID)) {
                ret = this.domainParticipants[domainID];
            } else {
                ret = DDS.DomainParticipantFactory.get_instance().create_participant(
                                domainID,
                                DDS.DomainParticipantFactory.PARTICIPANT_QOS_DEFAULT,
                                null, /* Listener */
                                DDS.StatusMask.STATUS_MASK_NONE);

                log.InfoFormat("Created domain participant on domainID = <{0}>", domainID);

                if (ret == null) {
                    throw new Exception("Unable to create domain participant");
                }

                this.domainParticipants.Add(domainID, ret);
            }
            return ret;
        }

        private DDS.Topic getTopic(DDS.DomainParticipant participant, string topicName, MappedMessage mappedMessage) {
            DDS.Topic ret = null;
            if (this.topics.ContainsKey(topicName)) {
                ret = this.topics[topicName];
            } else {
                /**
                 * To enable dynamic type support:
                 *      1. Create a TypeCode object from the the underlying class
                 *      2. Create a DynamicDataTypeSupport object based on the TypeCode of the underlying class
                 *      3. Register the TypeCode with the participant
                 **/

                /* Create the Dynamic data type support object */
                RTIMappedMessage rm = new RTIMappedMessage(mappedMessage);
                DDS.TypeCode type = rm.createType();

                DDS.DynamicDataTypeSupport typeSupport = new DDS.DynamicDataTypeSupport(type, DDS.DynamicDataTypeProperty_t.DYNAMIC_DATA_TYPE_PROPERTY_DEFAULT);

                /* Register type before creating topic */
                typeSupport.register_type(participant, rm.TypeName);

                ret = participant.create_topic(topicName, rm.TypeName, DDS.DomainParticipant.TOPIC_QOS_DEFAULT,
                                                       null, /* Listener */
                                                       DDS.StatusMask.STATUS_MASK_NONE);
                if (ret == null) {
                    throw new Exception("Unable to create topic : " + topicName);
                } else {
                    log.InfoFormat("Created topic = <{0}>", topicName);
                }
                this.topics.Add(topicName, ret);
            }
            return ret;
        }


        /// <summary>
        /// Creates a rti message publisher 
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="mappedMessage"></param>
        /// <returns></returns>
        public IMessagePublisher createPublisher(int domainID, string topicName, MappedMessage mappedMessage) {
            // RENG:6/16/15::Only high volume topics high throughput, all others should use reliable
            RTIMessagePublisherWithProfile mp = new RTIMessagePublisherWithProfile();
            //if (topicName == Topics.TICK_DATA_FULL || topicName == Topics.TICK_DATA_STREAM || topicName == CQGDataCollector.CQG_HISTO_TICK || topicName == CQGDataCollector.CQG_REALTIME_TICK)
            //if (topicName == Topics.TICK_DATA_STREAM || topicName == CQGDataCollectorOld.CQG_HISTO_TICK || topicName == CQGDataCollectorOld.CQG_REALTIME_TICK) //daniel 20160510 remove the TICK_DATA_FULL topic
            if (topicName == Topics.TICK_DATA_STREAM || topicName == Topics.TICK_DOM_STREAM || topicName == Topics.TICK_INDICATIVE_OPEN_STREAM) //daniel 20170310 remove the CQG topics and add the 2 new topics
            //if (topicName == Topics.TICK_DATA_STREAM) //daniel 20170310 remove the CQG topics and add the 2 new topics
            {
                mp.QOSLibraryName = RTIConstants.QOSLibraryNames.DEFAULT;
                mp.QOSProfileName = RTIConstants.QOSProfileNames.HIGH_THROUGHPUT;
            } else {
                mp.QOSLibraryName = RTIConstants.QOSLibraryNames.DEFAULT;
                mp.QOSProfileName = RTIConstants.QOSProfileNames.RELIABLE;
            }

            DDS.DomainParticipant participant = this.getDomainParticipantWithProfile(domainID, mp.QOSLibraryName, mp.QOSProfileName);
            DDS.Topic topic = getTopic(participant, topicName, mappedMessage);

            mp.initialize(domainID, topicName, mappedMessage, participant, topic);

            log.InfoFormat("Created publisher on topic = <{0}> with QOSLibrary = <{1}> : QOSProfileName = <{2}>", topicName, mp.QOSLibraryName, mp.QOSProfileName);

            return mp;
        }

        /// <summary>
        /// Creates a rti message subscriber
        /// </summary>
        /// <param name="topicName"></param>
        /// <param name="mappedMessage"></param>
        /// <returns></returns>
        public IMessageSubscriber createSubscriber(int domainID, string topicName, MappedMessage mappedMessage) {
            // RENG:6/16/15::Only high volume topics high throughput, all others should use reliable
            RTIMessageSubscriberWithProfile ms = new RTIMessageSubscriberWithProfile();
            //if (topicName == Topics.TICK_DATA_FULL || topicName == Topics.TICK_DATA_STREAM || topicName == CQGDataCollector.CQG_HISTO_TICK || topicName == CQGDataCollector.CQG_REALTIME_TICK)
            //if (topicName == Topics.TICK_DATA_STREAM || topicName == CQGDataCollectorOld.CQG_HISTO_TICK || topicName == CQGDataCollectorOld.CQG_REALTIME_TICK) //daniel 20160510 remove the TICK_DATA_FULL topic
            if (topicName == Topics.TICK_DATA_STREAM || topicName == Topics.TICK_DOM_STREAM || topicName == Topics.TICK_INDICATIVE_OPEN_STREAM) //daniel 20170310 remove the CQG topics and add the 2 new topics
            //if (topicName == Topics.TICK_DATA_STREAM) //daniel 20170310 remove the CQG topics and add the 2 new topics
            {
                ms.QOSLibraryName = RTIConstants.QOSLibraryNames.DEFAULT;
                ms.QOSProfileName = RTIConstants.QOSProfileNames.HIGH_THROUGHPUT;
            } else {
                ms.QOSLibraryName = RTIConstants.QOSLibraryNames.DEFAULT;
                ms.QOSProfileName = RTIConstants.QOSProfileNames.RELIABLE;
            }
            DDS.DomainParticipant participant = this.getDomainParticipantWithProfile(domainID, ms.QOSLibraryName, ms.QOSProfileName);
            DDS.Topic topic = getTopic(participant, topicName, mappedMessage);

            ms.initialize(domainID, topicName, mappedMessage, null, null, participant, topic);

            log.InfoFormat("Created subscriber on topic = <{0}> with QOSLibrary = <{1}> : QOSProfileName = <{2}>", topicName, ms.QOSLibraryName, ms.QOSProfileName);
            return ms;
        }

        public IMessageSubscriber createSubscriber(int domainID, string topicName, MappedMessage mappedMessage, string filterFieldName, string initialFilters) {
            RTIMessageSubscriber ms = new RTIMessageSubscriber();

            DDS.DomainParticipant participant = this.getDomainParticipant(domainID);
            DDS.Topic topic = getTopic(participant, topicName,mappedMessage);
            ms.initialize(domainID, topicName, mappedMessage, filterFieldName, initialFilters, participant, topic);
            return ms;
        }

        public void shutdown() {
            try {
                foreach (DDS.DomainParticipant participant in this.domainParticipantsWithProfile.Values) {
                    shutdownDomainParticipant(participant);
                }
                foreach (DDS.DomainParticipant participant in this.domainParticipants.Values) {
                    shutdownDomainParticipant(participant);
                }
            } catch (Exception e) {
                log.Error(e);
            }
        }

        private void shutdownDomainParticipant(DDS.DomainParticipant participant) {
            if (participant != null) {
                log.Info("shutting down participant");
                participant.delete_contained_entities();
                DDS.DomainParticipantFactory.get_instance().delete_participant(ref participant);
                log.Info("participant shut down");
            }
        }
    }
}
