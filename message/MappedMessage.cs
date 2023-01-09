using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    /// <summary>
    /// A generic object must be transformed into a mapped message before it can be 
    /// sent over the message bus
    /// </summary>
    public class MappedMessage {

        private Dictionary<string, MessageField> fields = new Dictionary<string, MessageField>();
        private static int MAX_STRING_SIZE = 25; // the default max string size
        public const string LengthField = "Length";

        public MappedMessage() {
        }

        /// <summary>
        /// Initializes a mapped message from the speciifed value object
        /// </summary>
        /// <param name="typeName">Unique name within the domain for this object</param>
        /// <param name="value">Generic domain object</param>
        public void initialize(string typeName, object value) {
            this.TypeName = typeName;
            if (this.TypeName == typeof(byte[]).Name) //reflection doesn't work well with byte arrays
            {
                SerializeByteArrayToMessageField((byte[])value);
                return;
            }
            else
            {
                reflectFields(value);
            }
        }

        private void SerializeByteArrayToMessageField(byte[] value)
        {
            Type byteArrayType = typeof(byte[]);
            MessageField mf = new MessageField();
            mf.FieldName = byteArrayType.Name;
            mf.Type = byteArrayType;
            mf.Value = value;
            fields.Add(mf.FieldName, mf);
        }

        /// <summary>
        /// Uses reflection to loop through all fields and build the fields dictionary
        /// </summary>
        /// <param name="value"></param>
        private void reflectFields(object value) {
            foreach (var prop in value.GetType().GetProperties()) { 
                MessageField mf = new MessageField();
                mf.FieldName = prop.Name;
                mf.Type = prop.PropertyType;
                mf.Value = prop.GetValue(value);

                // set the default max length if this is a string
                if (prop.PropertyType == typeof(System.String)) {
                    mf.MaxLength = MAX_STRING_SIZE;
                }
                fields.Add(mf.FieldName, mf);

                // retrieve the max length attribute if it exists
                var attributes = prop.GetCustomAttributes(false);
                foreach (var attribute in attributes) {
                    if (attribute.GetType() == typeof(MaxLengthAttribute)) {
                        MaxLengthAttribute mla = (MaxLengthAttribute)attribute;
                        mf.MaxLength = mla.Length;
                    }
                }
            }
        }

        /// <summary>
        /// Each type must have a unique name
        /// </summary>
        public string TypeName {
            set;
            get;
        }

        /// <summary>
        /// Used to set the max length of string fields
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="maxLength"></param>
        public void setMaxLength(string fieldName, int maxLength) {
            this.fields[fieldName].MaxLength = maxLength;
        }

        /// <summary>
        /// Resets all of the field values, in preparation for next send
        /// </summary>
        public void reset() {
            foreach (MessageField mf in this.fields.Values) {
                mf.Value = null;
            }
        }

        /// <summary>
        /// Sets the value of the field
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="value"></param>
        public void setValue(string fieldName, object value) {
            this.fields[fieldName].Value = value;
        }

        public Dictionary<string, MessageField> GetFields() {
            return this.fields;
        }

        /// <summary>
        /// Creates a copy of this object
        /// </summary>
        /// <returns></returns>
        public MappedMessage clone() {
            Dictionary<string, MessageField> fields = new Dictionary<string, MessageField>();

            foreach (MessageField mf in this.fields.Values) {
                fields.Add(mf.FieldName, mf.clone());
            }

            MappedMessage mm = new MappedMessage();
            mm.fields = fields;
            mm.TypeName = this.TypeName;

            return mm;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            foreach (MessageField mf in this.fields.Values) {
                sb.Append(mf.FieldName).Append(" = <").Append(mf.Value).Append("> : ");
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Custom attribute to designate the max length of string fields
    /// </summary>
    public class MaxLengthAttribute : Attribute {
        public int Length { get; private set; }
        public MaxLengthAttribute(int length) {
            this.Length = length;
        }
    }
}
