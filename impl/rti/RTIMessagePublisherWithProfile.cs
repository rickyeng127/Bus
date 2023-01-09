using FORT.Bus.impl.rti.message;
using FORT.Bus.message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.impl.rti {

    /// <summary>
    /// Extends the RTIMessagePublisher with the ability to 
    /// create message bus entites with custom profiles
    /// </summary>
    public class RTIMessagePublisherWithProfile : RTIMessagePublisher {

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

        /// <summary>
        /// Initializes the RTI entities using configured QOS library name and profile name
        /// </summary>
        /// <param name="domainID">Domains allows creation of separate isolated communication networks within the same physical network</param>
        /// <param name="topicName"></param>
        /// <param name="mappedMessage"></param>
        public override void initialize(int domainID, string topicName, MappedMessage mappedMessage, DDS.DomainParticipant participant, DDS.Topic topic) {

            this.topicName = topicName;

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
                log.InfoFormat("Created dynamic type for = <{0}>", typeName);
            }

            /**
             * Creates the data writer for the domain participant and topic 
             **/
            rtiDataWriter = (DDS.DynamicDataWriter)participant.create_datawriter_with_profile(topic,
                                                                                    QOSLibraryName,
                                                                                    QOSProfileName,
                                                                                    null, /* Listener */
                                                                                    DDS.StatusMask.STATUS_MASK_NONE);

            if (rtiDataWriter == null) {
                throw new Exception("Unable to create DDS data writer for topic : " + topicName);
            } else {
                log.InfoFormat("Created data writer for topic = <{0}>", topicName);
            }

            /* Creates an instance of the data we are about to send
             */
            rtiDataInstance = new DDS.DynamicData(type, DDS.DynamicData.DYNAMIC_DATA_PROPERTY_DEFAULT);
        }
    }
}
