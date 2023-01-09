using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

using log4net;

using FORT.Bus.message;
using FORT.Bus.impl.rti.message;
using System.Runtime.ExceptionServices;

namespace FORT.Bus.impl.rti {
    public class RTIMessagePublisher : IMessagePublisher {

        protected static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        protected DDS.DynamicDataWriter rtiDataWriter;
        protected DDS.DynamicData rtiDataInstance;
        protected DDS.InstanceHandle_t instanceHandle = DDS.InstanceHandle_t.HANDLE_NIL;
        protected string topicName;
        protected DDS.DynamicDataTypeSupport typeSupport;
        protected DDS.TypeCode type;
        protected string typeName;
        protected bool goingDown;
        protected object lockThis = new Object();
        private static readonly int DEFAULT_NUM_DOM_LEVELS = FORT.Util.FortUtilDLLSettings.Get().NumberOfDomLevels;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="domainID">Domains allows creation of separate isolated communication networks within the same physical network</param>
        /// <param name="topicName"></param>
        /// <param name="mappedMessage"></param>
        public virtual void initialize(int domainID, string topicName, MappedMessage mappedMessage, DDS.DomainParticipant participant, DDS.Topic topic) {

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
            rtiDataWriter = (DDS.DynamicDataWriter) participant.create_datawriter(topic,
                                                                                    DDS.Publisher.DATAWRITER_QOS_DEFAULT,
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

        /// <summary>
        /// Sends the message over the message bus
        /// </summary>
        /// <param name="mappedMessage"></param>
        [HandleProcessCorruptedStateExceptions]
        public void send(MappedMessage mappedMessage) {
            lock (lockThis) {
                try {
                    if (!goingDown) {
                        // set the message fields
                        setRTIMessage(mappedMessage);
               
                        // send the data on the topic
                        rtiDataWriter.write(rtiDataInstance, ref instanceHandle);
                        //Console.Out.WriteLine("sent a message");
                    }
                } catch (Exception e) {
                    if (!goingDown) {
                        log.ErrorFormat("Error sending message on topic <{0}> - <{1}> - <{2}>", topicName, e.Message, e.StackTrace);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the values contained within the mappedMessage into
        /// an RTIMappedMessage in preparation for sending over the 
        /// message bus
        /// </summary>
        /// <param name="mappedMessage"></param>
        protected void setRTIMessage(MappedMessage mappedMessage) {
            foreach (MessageField mf in mappedMessage.GetFields().Values) {
                try {
                    if (mf.Value != null) {
                        if (mf.Type == typeof(System.String)) {
                            rtiDataInstance.set_string(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, mf.Value.ToString());
                        } else if (mf.Type == typeof(System.Int16)) {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        } else if (mf.Type == typeof(System.Int32)) {
                            rtiDataInstance.set_int(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToInt32(mf.Value));
                        } else if (mf.Type == typeof(System.Int64)) {
                            rtiDataInstance.set_long(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToInt64(mf.Value));
                        } else if (mf.Type == typeof(System.Double)) {
                            rtiDataInstance.set_double(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToDouble(mf.Value));
                        } else if (mf.Type == typeof(System.Single)) {
                            rtiDataInstance.set_float(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToSingle(mf.Value));
                        } else if (mf.Type == typeof(System.Byte)) {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        } else if (mf.Type == typeof(System.Char)) {
                            rtiDataInstance.set_char(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToChar(mf.Value));
                        } else if (mf.Type == typeof(System.Boolean)) {
                            rtiDataInstance.set_boolean(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToBoolean(mf.Value));
                        //daniel 20160427 need to implement the decimal type, will use a byte array
                        //} else if (mf.Type == typeof(System.Decimal)) {
                        //    throw new Exception("Unsupported type : " + mf.Type.ToString());
                        } else if (mf.Type == typeof(System.Decimal)) {
                            byte[] bytes = RTIMappedMessage.GetBytes(Convert.ToDecimal(mf.Value));
                            rtiDataInstance.set_byte_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, bytes);
                            //rtiDataInstance.set_double(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToDouble(mf.Value));
                        }
                        else if (mf.Type == typeof(decimal[]))
                        {
                            DDS.LongDouble[] logDoubleArray = RTIMappedMessage.ToLongDoubleArray((decimal[])mf.Value);
                            rtiDataInstance.set_longdouble_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, logDoubleArray);
                        }
                        else if (mf.Type == typeof(int[]))
                        {
                            int[] myData = (int[])mf.Value;
                            int[] intArray = new int[myData.Length];
                            
                            System.Array.Copy(myData, intArray, myData.Length);
                            rtiDataInstance.set_int_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, intArray);
                        }
                        else if (mf.Type == typeof(double[]))
                        {
                            double[] myData = (double[])mf.Value;
                            double[] doubleArray = new double[myData.Length];                                                       

                            System.Array.Copy(myData, doubleArray, myData.Length);
                            rtiDataInstance.set_double_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, doubleArray);
                        }
                        else if (mf.Type == typeof(byte[]))
                        {
                            DDS.ByteSeq byteSeq = new DDS.ByteSeq();
                            byteSeq.from_array((byte[])mf.Value);
                            rtiDataInstance.set_byte_seq(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, byteSeq);
                        }
                        else
                        {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        }
                    } else { // value is null
                        if (mf.Type == typeof(System.String))
                        {
                            rtiDataInstance.set_string(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, "");
                        }
                        else if (mf.Type == typeof(System.Int16))
                        {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        }
                        else if (mf.Type == typeof(System.Int32))
                        {
                            rtiDataInstance.set_int(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, 0);
                        }
                        else if (mf.Type == typeof(System.Int64))
                        {
                            rtiDataInstance.set_long(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, 0);
                        }
                        else if (mf.Type == typeof(System.Double))
                        {
                            rtiDataInstance.set_double(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, 0);
                        }
                        else if (mf.Type == typeof(System.Single))
                        {
                            rtiDataInstance.set_float(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, 0);
                        }
                        else if (mf.Type == typeof(System.Byte))
                        {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        }
                        else if (mf.Type == typeof(System.Char))
                        {
                            rtiDataInstance.set_char(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, ' ');
                        }
                        else if (mf.Type == typeof(System.Boolean))
                        {
                            rtiDataInstance.set_boolean(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, false);
                            //daniel 20160427 need to implement the decimal type, will use a byte array
                            //} else if (mf.Type == typeof(System.Decimal)) {
                            //    throw new Exception("Unsupported type : " + mf.Type.ToString());
                        }
                        else if (mf.Type == typeof(System.Decimal))
                        {
                            byte[] bytes = RTIMappedMessage.GetBytes(0);
                            rtiDataInstance.set_byte_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, bytes);
                            //rtiDataInstance.set_double(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, Convert.ToDouble(mf.Value));
                        }
                        else if (mf.Type == typeof(decimal[]))
                        {
                            DDS.LongDouble[] logDoubleArray = new DDS.LongDouble[DEFAULT_NUM_DOM_LEVELS];
                            rtiDataInstance.set_longdouble_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, logDoubleArray);
                        }
                        else if (mf.Type == typeof(int[]))
                        {
                            int[] intArray = new int[DEFAULT_NUM_DOM_LEVELS];
                            rtiDataInstance.set_int_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, intArray);
                        }
                        else if (mf.Type == typeof(double[]))
                        {
                            double[] doubleArray = new double[DEFAULT_NUM_DOM_LEVELS];
                            rtiDataInstance.set_double_array(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, doubleArray);
                        }
                        else if (mf.Type == typeof(byte[]))
                        {
                            DDS.ByteSeq byteSeq = new DDS.ByteSeq();
                            rtiDataInstance.set_byte_seq(mf.FieldName, DDS.DynamicData.MEMBER_ID_UNSPECIFIED, byteSeq);
                        }
                        else
                        {
                            throw new Exception("Unsupported type : " + mf.Type.ToString());
                        }
                    }
                } catch (Exception e) {
                    log.ErrorFormat("Unable to set field = <{0}> : type = <{1}> : message = <{2}> - stackTrace = <{3}>", mf.FieldName, mf.Type.ToString(), e.Message, e.StackTrace);
                    throw new Exception("Unable to set field = <" + mf.FieldName + "> type = <" + mf.Type.ToString() + ">" + e.Message);
                }
            }
        }


        public void shutdown() {
            try {
                goingDown = true;

                log.InfoFormat("shutting down publisher on topic - <{0}>", this.topicName);
                typeSupport.Dispose();

                if (type != null) {
                    DDS.TypeCodeFactory.get_instance().delete_tc(type);
                }
            } catch (Exception) {
                
            }
            log.InfoFormat("publisher shutdown on topic - <{0}>", this.topicName);
        }
    }
}
