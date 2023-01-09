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

        public override void initialize(int domainID, string topicName, Bus.message.MappedMessage mappedMessage,
            string filterFieldName, string initialFilters, DDS.DomainParticipant participant, DDS.Topic topic) {
            
            this.topicName = topicName;
            this.mappedMessage = mappedMessage.clone();

            // request to create a filtered topic?
            if (filterFieldName != null && !filterFieldName.Trim().Equals("")) {
                this.isFilteredTopic = true;
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
