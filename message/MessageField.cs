using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    public class MessageField {

        public string FieldName {
            get;
            set;
        }

        public Type Type {
            get;
            set;
        }

        public object Value {
            get;
            set;
        }

        /// <summary>
        /// For strings
        /// </summary>
        public int MaxLength {
            get;
            set;
        }

        public MessageField clone() {
            MessageField mf = new MessageField();
            mf.FieldName = this.FieldName;
            mf.MaxLength = this.MaxLength;
            mf.Type = this.Type;
            mf.Value = this.Value;
            return mf;
        }

    }
}
