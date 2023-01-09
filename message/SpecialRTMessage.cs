using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;

namespace FORT.Bus.message
{
    public class SpecialRTMessage{
        
        private static ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, string> fields;

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpecialRTMessage(){
        }

        /// <summary>
        /// Added to create a SpecialRTMessage out of a pipe delimited string.
        /// </summary>
        /// <param name="pipeDelimetedSpecialRTMessage"></param>
        public SpecialRTMessage(string pipeDelimetedSpecialRTMessage){
            char[] pipe = {'|'};
            string[] buffer = pipeDelimetedSpecialRTMessage.Split(pipe);
            if(buffer.Length > 1){
                this.Contract = buffer[0];
                this.Data = buffer[1]; // e.g. this.Data = field1=value1|field2=value2|field3=value3
            }else{
                logger.ErrorFormat("Unable to construct the SpecialRTMessage object, not enough fields (needs 2 has only <{0}>) in the pipe delimited string : <{1}>", buffer.Length, pipeDelimetedSpecialRTMessage);
            }
        }

        /// <summary>
        /// e.g. ADM1, ADM2
        /// </summary>
        [MaxLengthAttribute(50)]
        public string Contract{
            get;
            set;
        }

        public void parseFields() {
            this.fields = new Dictionary<string, string>();
            // split the data into fields for convenience
            string[] toks = this.Data.Split('|');
            foreach (string tok in toks) { // e.g. tok = field1=value1
                if (tok != null && tok.Trim() != "") {
                    string[] nv = tok.Split('=');
                    this.fields.Add(nv[0], nv[1]);
                }
            }
        }

        /// <summary>
        /// Returns the field value with the specified key.
        /// null is return if field does not exist.
        /// </summary>
        /// <param name="field"></param>
        /// <returns></returns>
        public string getValue(string field) {
            string ret = null;
            this.fields.TryGetValue(field, out ret);
            return ret;
        }

        /// <summary>
        /// The data for this command
        /// </summary>
        [MaxLengthAttribute(2048)]
        public string Data{
            get;
            set;
        }

        /// <summary>
        /// Serialize the SpecialRTMessage object in a pipe delimited string object
        /// </summary>
        /// <returns>string</returns>
        public override string ToString(){
            return string.Format("{0}|{1}",
                this.Contract,
                this.Data);
        }

    }
    /// <summary>
    /// The fields within the SpecialRTMessage object
    /// </summary>
    public class SpecialRTMessageFields{
        public static readonly string Contract = "Contract";
        public static readonly string Data = "Data";
    }
}
