using FORT.Bus.impl.rti.message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.impl.rti {
    class RTIMessageSubscriberWithProfile : RTIMessageSubscriber {

        /// <summary>
        /// The name of the RTI library that will be loaded for all entity creation
        /// </summary>
        public string QOSLibraryName {
            set;
            get;
        }

        /// <summary>
        /// The name of the RTI profile that will be loaded for all entity creation
        /// </summary>
        public string QOSProfileName {
            set;
            get;
        }

        public override void initialize(int domainID, string topicName, Bus.message.MappedMessage mappedMessage, string filterFieldName, string initialFilters) {
            this.topicName = topicName;
            this.mappedMessage = mappedMessage.clone();

            // request to create a filtered topic?
            if (filterFieldName != null && !filterFieldName.Trim().Equals("")) {
                this.isFilteredTopic = true;
            }

            participant = DDS.DomainParticipantFactory.get_instance().create_participant_with_profile(
                                domainID,
                                QOSLibraryName,
                                QOSProfileName,
                                null, /* Listener */
                                DDS.StatusMask.STATUS_MASK_NONE);

            log.InfoFormat("Created domain participant on domainID = <{0}>", domainID);

            if (participant == null) {
                throw new Exception("Unable to create domain participant");
            }

            /**
             * To enable dynamic type support:
             *      1. Create a TypeCode object from the the underlying class
             *      2. Create a DynamicDataTypeSupport object based on the TypeCode of the underlying class
             *      3. Register the TypeCode with the participant
             **/

            // Create an RTI mapped message from the mappedMessage
            RTIMappedMessage rm = new RTIMappedMessage(mappedMessage);

            type = rm.createType();
            typeName = mappedMessage.TypeName;
            if (type == null) {
                throw new Exception("Unable to create dynamic type code for type = " + mappedMessage.TypeName);
            } else {
                log.InfoFormat("Created dynamic type = <{0}>", typeName);
            }

            /* Create the Dynamic data type support object */
            typeSupport = new DDS.DynamicDataTypeSupport(type, DDS.DynamicDataTypeProperty_t.DYNAMIC_DATA_TYPE_PROPERTY_DEFAULT);

            /* Register type before creating topic */
            typeSupport.register_type(participant, rm.TypeName);

            /**
             * This createa a topic to send objects of RTIMappedMessage
             **/
            DDS.Topic topic = participant.create_topic_with_profile(topicName, 
                                                            rm.TypeName,
                                                            QOSLibraryName,
                                                            QOSProfileName,
                                                            null, /* Listener */
                                                            DDS.StatusMask.STATUS_MASK_NONE);

            if (topic == null) {
                throw new Exception("Unable to create topic : " + topicName);
            } else {
                log.InfoFormat("Created topic = <{0}>", topicName);
            }

            // request for a content based filter
            if (isFilteredTopic) {
                // For this filter we only allow 1 parameter
                DDS.StringSeq parameters = new DDS.StringSeq(1);
                string fex = filterFieldName + " MATCH '" + initialFilters + "'";
                log.InfoFormat("creating filtered topic using filter = <{0}>", fex);
                contentFilteredTopic = participant.create_contentfilteredtopic_with_filter("Filtered" + topicName, topic, fex, parameters, DDS.DomainParticipant.STRINGMATCHFILTER_NAME);

                if (contentFilteredTopic == null) {
                    throw new Exception("Unable to create filter topic : " + topicName);
                }
            }

            if (isFilteredTopic) {
                // content filtered topic
                rtiDataReader = (DDS.DynamicDataReader)
                            participant.create_datareader_with_profile(contentFilteredTopic,
                            QOSLibraryName,
                            QOSProfileName,
                            this, DDS.StatusMask.STATUS_MASK_ALL);
            } else { // normal topic
                // Create the data reader
                rtiDataReader = (DDS.DynamicDataReader)
                            participant.create_datareader_with_profile(
                            topic,
                            QOSLibraryName,
                            QOSProfileName,
                            this,
                            DDS.StatusMask.STATUS_MASK_ALL);
            }


            if (rtiDataReader == null) {
                throw new Exception("Unable to create DDS Data Reader");
            } else {
                log.InfoFormat("Created reader on topic = <{0}>", this.topicName);
            }
        }
    }
}
