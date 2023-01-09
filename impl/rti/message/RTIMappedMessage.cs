using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using FORT.Bus.message;
using System.Globalization;

namespace FORT.Bus.impl.rti.message {
    
    /// <summary>
    /// Class acts as a bridge between the application generic MappedMessage class
    /// and an RTI message 
    /// </summary>
    public class RTIMappedMessage {

        private MappedMessage mappedMessage;
        private DDS.TypeCodeFactory factory;

        public RTIMappedMessage(MappedMessage mappedMessage) {
            this.mappedMessage = mappedMessage.clone();
        }

        public string TypeName {
            get {
                return this.mappedMessage.TypeName;    
            }
        }

        /// <summary>
        /// Maps the fields within the mappedMessage to the
        /// fields within an RTI type
        /// </summary>
        /// <returns></returns>
        public DDS.TypeCode createType() {

            /**
             * Initialize the Type code factory
             **/
            factory = DDS.TypeCodeFactory.get_instance();
            if (factory == null) {
                throw new Exception("Unable to get type code factory singleton");
            }

            // initialize set of member fields
            DDS.StructMemberSeq members = new DDS.StructMemberSeq();
            members.ensure_length(mappedMessage.GetFields().Count, mappedMessage.GetFields().Count);

            // initialize each member field
            int i = 0;
            foreach (MessageField mf in this.mappedMessage.GetFields().Values) {
                members.buffer[i] = new DDS.StructMember();
                members.buffer[i].name = mf.FieldName;
                members.buffer[i].is_pointer = false;
                members.buffer[i].bits = (short)-1;
                members.buffer[i].is_key = false;
                members.buffer[i].type = CreateTypeCode(mf);
                i++;
            }

            /**
             * Create and return the typeCode for this class
             **/
            DDS.TypeCode typeCode = null;
            try {
                typeCode = factory.create_struct_tc(this.mappedMessage.TypeName, members);
            } catch (DDS.ExceptionCode_BadParam err) {
                throw new Exception("Unable to create struct typecode: " + err.Message);
            }

            return typeCode;    
        }

        /// <summary>
        /// Returns the RTI TypeCode based on the C# Type of 
        /// the specified MessageField
        /// </summary>
        /// <param name="mf"></param>
        /// <returns></returns>
        private DDS.TypeCode CreateTypeCode(MessageField mf) {

            DDS.TypeCode tc = null;

            try {
                if (mf.Type == typeof(System.String))
                {
                    tc = factory.create_string_tc((uint)mf.MaxLength);
                }
                else if (mf.Type == typeof(System.Int16))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_LONG);
                }
                else if (mf.Type == typeof(System.Int32))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_LONG);
                }
                else if (mf.Type == typeof(System.Int64))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_LONGLONG);
                }
                else if (mf.Type == typeof(System.Double))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_DOUBLE);
                }
                else if (mf.Type == typeof(System.Single))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_FLOAT);
                }
                else if (mf.Type == typeof(System.Byte))
                {
                    throw new Exception("Unsupported type : " + mf.Type.ToString());
                }
                else if (mf.Type == typeof(System.Char))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_CHAR);
                }
                else if (mf.Type == typeof(System.Boolean))
                {
                    tc = factory.get_primitive_tc(DDS.TCKind.TK_BOOLEAN);
                    //daniel 20160427 need to implement the decimal type, will use a byte array
                    //} else if (mf.Type == typeof(System.Decimal)) {
                    //    throw new Exception("Unsupported type : " + mf.Type.ToString());
                }
                else if (mf.Type == typeof(System.Decimal))
                {
                    //tc = factory.get get_primitive_tc(DDS.TCKind.TK_LONGDOUBLE);
                    tc = factory.create_array_tc(16, DDS.TypeCode.TC_OCTET);
                    //tc = factory.get_primitive_tc(DDS.TCKind.TK_DOUBLE);
                }
                else if (mf.Type == typeof(int[]))
                {
                    //tc = factory.get get_primitive_tc(DDS.TCKind.TK_LONGDOUBLE);
                    uint arrSize = (uint)(((int[])mf.Value).Length);
                    tc = factory.create_array_tc(arrSize, DDS.TypeCode.TC_LONG);
                    //tc = factory.get_primitive_tc(DDS.TCKind.TK_DOUBLE);
                }
                else if (mf.Type == typeof(double[]))
                {
                    uint arrSize = (uint)(((double[])mf.Value).Length);
                    tc = factory.create_array_tc(arrSize, DDS.TypeCode.TC_DOUBLE);
                }
                else if (mf.Type == typeof(decimal[]))
                {
                    uint arrSize = (uint)(((decimal[])mf.Value).Length);
                    //tc = factory.get get_primitive_tc(DDS.TCKind.TK_LONGDOUBLE);
                    tc = factory.create_array_tc(arrSize, DDS.TypeCode.TC_LONGDOUBLE);
                    //tc = factory.get_primitive_tc(DDS.TCKind.TK_DOUBLE);
                }
                else if (mf.Type == typeof(byte[]))
                {
                    uint arrSize = (uint)(((byte[])mf.Value).Length);
                    tc = factory.create_array_tc(arrSize, DDS.TypeCode.TC_OCTET);
                }
                else
                {
                    throw new Exception("Unsupported type : " + mf.Type.ToString());
                }
            } catch (Exception e) {
                throw new Exception("Unable to create typecode for field = <" + mf.FieldName + "> type = <" + mf.Type.ToString() + ">" + e.Message);
            }

            return tc;

        }
        public static byte[] GetBytes(decimal dec){
            //Load four 32 bit integers from the Decimal.GetBits function
            Int32[] bits = decimal.GetBits(dec);
            //Create a temporary list to hold the bytes
            List<byte> bytes = new List<byte>();
            //iterate each 32 bit integer
            foreach (Int32 i in bits){
                //add the bytes of the current 32bit integer
                //to the bytes list
                bytes.AddRange(BitConverter.GetBytes(i));
            }
            //return the bytes list as an array
            return bytes.ToArray();
        }
        public static decimal ToDecimal(byte[] bytes){
            //check that it is even possible to convert the array
            if (bytes.Count() != 16)
                throw new Exception("A decimal must be created from exactly 16 bytes");
            //make an array to convert back to int32's
            Int32[] bits = new Int32[4];
            for (int i = 0; i <= 15; i += 4){
                //convert every 4 bytes into an int32
                bits[i / 4] = BitConverter.ToInt32(bytes, i);
            }
            //Use the decimal's new constructor to
            //create an instance of decimal
            return new decimal(bits);
        }

        public static DDS.LongDouble[] ToLongDoubleArray(decimal[] decimalArray){
            DDS.LongDouble[] ldArray = new DDS.LongDouble[decimalArray.Length];
            for (int i = 0; i < decimalArray.Length; i++)
            {
                ldArray[i] = new DDS.LongDouble((double)decimalArray[i]);
            }
            return ldArray;
        }

        public static decimal[] ToDecimalArray(DDS.LongDouble[] longDoubleArray)
        {
            decimal[] dArray = new decimal[longDoubleArray.Length];
            for (int i = 0; i < longDoubleArray.Length; i++){
                dArray[i] =  longDoubleArray[i].ToDecimal(CultureInfo.InvariantCulture.NumberFormat);                
            }
            return dArray;
        }

    }
}
