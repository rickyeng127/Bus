using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using log4net;

using FORT.Bus.impl;
using FORT.Bus.message;
using FORT.Bus.impl.rti.message;
using FORT.Bus.impl.message;

namespace FORT.Bus.impl.rti {
    public class RTIMessageSubscriber : DDS.DataReaderListener, IMessageSubscriber {
        private static readonly int DEFAULT_NUM_DOM_LEVELS = FORT.Util.FortUtilDLLSettings.Get().NumberOfDomLevels;
        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected DDS.DynamicDataSeq dataSeq = new DDS.DynamicDataSeq();
        protected DDS.SampleInfoSeq infoSeq = new DDS.SampleInfoSeq();
        protected DDS.DynamicDataReader rtiDataReader;
        protected DDS.DynamicDataTypeSupport typeSupport;
        protected DDS.TypeCode type;
        protected DDS.ContentFilteredTopic contentFilteredTopic;

        protected string topicName;
        protected string typeName;        // the unique typeName this topic is configured to send 
        protected bool isFilteredTopic;   // whether this topic is a content filtered topic

        protected MappedMessage mappedMessage;

        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of a new price event
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyNewMessage(MappedMessage mappedMessage);
        public event NotifyNewMessage newMessageEvent;

        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of lost messages
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyLostMessage(LostMessageStatus lostMessageStatus);
        public event NotifyLostMessage lostMessageEvent;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="domainID">Domains allows creation of separate isolated communication networks within the same physical network</param>
        /// <param name="topicName"></param>
        /// <param name="mappedMessage"></param>
        /// <param name="filterFieldName"></param>
        /// <param name="initialFilters"></param>
        public virtual void initialize(int domainID, string topicName, MappedMessage mappedMessage,
            string filterFieldName, string initialFilters, DDS.DomainParticipant participant, DDS.Topic topic) {

            this.topicName = topicName;
            this.mappedMessage = mappedMessage.clone();

            // request to create a filtered topic?
            if (filterFieldName != null && !filterFieldName.Trim().Equals("")) {
                this.isFilteredTopic = true;
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
                            participant.create_datareader(contentFilteredTopic,
                            DDS.Subscriber.DATAREADER_QOS_DEFAULT,
                            this, DDS.StatusMask.STATUS_MASK_ALL);
            } else { // normal topic
                // Create the data reader
                rtiDataReader = (DDS.DynamicDataReader)
                            participant.create_datareader(
                            topic,
                            DDS.Subscriber.DATAREADER_QOS_DEFAULT,
                            this,
                            DDS.StatusMask.STATUS_MASK_ALL);
            }


            if (rtiDataReader == null) {
                throw new Exception("Unable to create DDS Data Reader");
            } else {
                log.InfoFormat("Created reader on topic = <{0}>", this.topicName);
            }
        }

        /// <summary>
        /// Called for every valid sample received from DDS. Method copies data 
        /// from the instance to the MappedData instance object in this class
        /// </summary>
        /// <param name="instance"></param>
        protected void processData(DDS.DynamicData instance) {

            // reset the values in the fields
            this.mappedMessage.reset();
            
            // loop through all the fields and retrieve the values
            foreach (MessageField mf in this.mappedMessage.GetFields().Values) {
                try {
                    if (mf.Type == typeof(System.String)) {
                        mf.Value = instance.get_string(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Int16)) {
                        throw new Exception("Unsupported type : " + mf.Type.ToString());
                    } else if (mf.Type == typeof(System.Int32)) {
                        mf.Value = instance.get_int(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Int64)) {
                        mf.Value = instance.get_long(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Double)) {
                        mf.Value = instance.get_double(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Single)) {
                        mf.Value = instance.get_float(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Byte)) {
                        throw new Exception("Unsupported type : " + mf.Type.ToString());
                    } else if (mf.Type == typeof(System.Char)) {
                        mf.Value = instance.get_char(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    } else if (mf.Type == typeof(System.Boolean)) {
                        mf.Value = instance.get_boolean(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                    //daniel 20160427 need to implement the decimal type, will use a byte array
                    //} else if (mf.Type == typeof(System.Decimal)) {
                    //    throw new Exception("Unsupported type : " + mf.Type.ToString());
                    }else if (mf.Type == typeof(System.Decimal)){
                        byte[] bytes = new byte[16];
                        instance.get_byte_array(bytes, mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                        mf.Value = RTIMappedMessage.ToDecimal(bytes);
                    }else if (mf.Type == typeof(decimal[])){
                        DDS.LongDouble[] longDoubleArray = new DDS.LongDouble[DEFAULT_NUM_DOM_LEVELS];
                        instance.get_longdouble_array(longDoubleArray, mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                        mf.Value = RTIMappedMessage.ToDecimalArray(longDoubleArray);
                    }
                    else if (mf.Type == typeof(double[]))
                    {
                        double[] doubleArray = new double[DEFAULT_NUM_DOM_LEVELS];
                        instance.get_double_array(doubleArray, mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                        mf.Value = doubleArray;
                    }
                    else if (mf.Type == typeof(int[]))
                    {
                        int[] intArray = new int[DEFAULT_NUM_DOM_LEVELS];
                        instance.get_int_array(intArray, mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                        mf.Value = intArray;
                    }
                    else if (mf.Type == typeof(byte[]))
                    {
                        DDS.ByteSeq byteSeq = new DDS.ByteSeq();
                        instance.get_byte_seq(byteSeq, mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED);
                        mf.Value = byteSeq.buffer;
                    }
                    else
                    {
                        throw new Exception("Unsupported type : " + mf.Type.ToString());
                    }
                } catch (Exception e) {
                    throw new Exception("Unable to read field = <" + mf.FieldName + "> type = <" + mf.Type.ToString() + ">" + e.Message);
                }
            }

            if (newMessageEvent != null)
            {
                newMessageEvent(this.mappedMessage.clone());
            }
        }
        
        /// <summary>
        /// Callback method when data is available to be read
        /// </summary>
        /// <param name="reader"></param>
        public override void on_data_available(DDS.DataReader reader) {

            /**
             * Cast the reader to a dynamic data reader
             **/
            DDS.DynamicDataReader ddr = (DDS.DynamicDataReader) reader;

            try {

                ddr.take(this.dataSeq, this.infoSeq,
                        DDS.ResourceLimitsQosPolicy.LENGTH_UNLIMITED,
                        DDS.SampleStateKind.ANY_SAMPLE_STATE,
                        DDS.ViewStateKind.ANY_VIEW_STATE,
                        DDS.InstanceStateKind.ANY_INSTANCE_STATE);

                // Loop through each instance of the data, only processing valid_data  
                for (int i = 0; i < this.dataSeq.length; ++i) {
                    // RENG:info is an instance of data 
                    DDS.SampleInfo info = (DDS.SampleInfo)this.infoSeq.get_at(i);

                    // only process if it is valid
                    if (info.valid_data) {
                        processData((DDS.DynamicData) this.dataSeq.get_at(i));
                    }
                }
            } catch (DDS.Retcode_NoData) {
                // do nothing
            } finally {
                // Return the memory obtained in the take() method call
                ddr.return_loan(this.dataSeq, this.infoSeq);
            }
        }

        public void on_sample_lost(DDS.DataReader reader, DDS.SampleLostStatus status) {
            log.Debug("Callback: sample lost.");
            LostMessageStatus s = new LostMessageStatus();
            s.TotalMessagesLost = status.total_count;
            s.NewMessagesLost = status.total_count_change;
            s.LostMessageReason = status.last_reason.ToString();
            lostMessageEvent(s);
        }

        public void on_requested_deadline_missed(DDS.DataReader reader, DDS.RequestedDeadlineMissedStatus status) {
            log.Debug("->Callback: requested deadline missed.");
        }

        public override void on_requested_incompatible_qos(DDS.DataReader reader, DDS.RequestedIncompatibleQosStatus status) {
            log.Debug("->Callback: requested incompatible Qos.");
        }

        public void on_sample_rejected(DDS.DataReader reader, DDS.SampleRejectedStatus status) {
            log.Debug("->Callback: sample rejected.");
        }

        public void on_liveliness_changed(DDS.DataReader reader, DDS.LivelinessChangedStatus status) {
            log.Debug("->Callback: liveliness changed.");
        }

        public void on_subscription_matched(DDS.DataReader reader, DDS.SubscriptionMatchedStatus status) {
            log.Debug("->Callback: subscription matched.");
        }


        // registers a listener for new events
        public void registerMessageListener(IMessageListener messageListener) {
            this.newMessageEvent += messageListener.NotifyNewMessage;
            this.lostMessageEvent += messageListener.NotifyMessageLost;
        }

        public void shutdown() {
            try {

                log.InfoFormat("shutting down subscriber on topic - <{0}>", this.topicName);

                typeSupport.Dispose();

                if (type != null) {
                    DDS.TypeCodeFactory.get_instance().delete_tc(type);
                }
            } catch (Exception) {
                
            }
            log.InfoFormat("subscriber shutdown on topic - <{0}>", this.topicName);
        }

        /// <summary>
        /// Appends an additional content based filter
        /// </summary>
        /// <param name="filter"></param>
        public void appendMessageFilter(string filter) {
            if (this.isFilteredTopic) {
                contentFilteredTopic.append_to_expression_parameter(0, filter);
                log.InfoFormat("adding a filter = <{0}> to topic = <{1}>", filter, this.topicName);
            } else {
                throw new Exception("topic " + this.topicName + " is not a filtered topic");
            }
        }

        /// <summary>
        /// Removes a content based filter.
        /// Note: Removing all filters will cause topic to not return any data (i.e. filter all data)
        /// Adding back a single filter restores the expected filtering behavior
        /// </summary>
        /// <param name="filter"></param>
        public void removeMessageFilter(string filter) {
            if (this.isFilteredTopic) {
                contentFilteredTopic.remove_from_expression_parameter(0, filter);
                log.InfoFormat("removing a filter = <{0}> from topic = <{1}>", filter, this.topicName);
            } else {
                throw new Exception("topic " + this.topicName + " is not a filtered topic");
            }
        }
    }
}
