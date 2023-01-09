using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    /// <summary>
    /// Generic info message to be sent on message bus
    /// </summary>
    public class InfoMessage {

        /// <summary>
        /// Used to uniquely identify an info message on the network
        /// </summary>
        private string guid;

        /// <summary>
        /// The time (in ticks) in which this info message was created
        /// </summary>
        private long createdAtTickTime;

        public InfoMessage() {
            this.guid = System.Guid.NewGuid().ToString();
            this.createdAtTickTime = DateTime.Now.Ticks;
        }

        /// <summary>
        /// The data for this command
        /// </summary>
        [MaxLengthAttribute(2048)]
        public string Data {
            get;
            set;
        }

        /// <summary>
        /// Returns the max length of the data field
        /// </summary>
        /// <returns></returns>
        public int getMaxDataLength() {
            int maxLength = 500;
            var prop = this.GetType().GetProperty("Data");
            if (prop != null) {
                var attributes = prop.GetCustomAttributes(false);
                if (attributes != null) {
                    foreach (var attribute in attributes) {
                        if (attribute.GetType() == typeof(MaxLengthAttribute)) {
                            MaxLengthAttribute mla = (MaxLengthAttribute)attribute;
                            maxLength = mla.Length;
                        }
                    }
                }
            }
            return maxLength;
        }

        /// <summary>
        /// The info code, returns an enumeration value from CommandConstants.Info
        /// </summary>
        public int InfoCode {
            get;
            set;
        }

        /// <summary>
        /// The sending applications, returns an enumeration value from CommandConstants.Applications
        /// </summary>
        public int FromApp {
            get;
            set;
        }

        /// <summary>
        /// Returns the time in ticks in which tick was created
        /// </summary>
        public long CreatedAtTickTime {
            get {
                return this.createdAtTickTime;
            }
            set {
                this.createdAtTickTime = value;
            }
        }

        /// <summary>
        /// The global unique identifier for this command
        /// </summary>
        [MaxLengthAttribute(36)]
        public string ID {
            get {
                return this.guid;
            }
            // Used to correlate a command with a command response
            set {
                this.guid = value;
            }
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("InfoMessage = InfoCode = ").Append(((CommandConstants.Info)this.InfoCode).ToString())
              .Append(" : From = ").Append(((CommandConstants.Applications)this.FromApp).ToString())
              .Append(" : Data = ").Append(this.Data)
              .Append(" : ID = ").Append(this.ID);
            return sb.ToString();
        }

        /// <summary>
        /// The fields within the InfoMessage object
        /// </summary>
        public class InfoFields {
            public static readonly string Data = "Data";
            public static readonly string FromApp = "FromApp";
            public static readonly string InfoCode = "InfoCode";
            public static readonly string ID = "ID";
            public static readonly string CreatedAtTickTime = "CreatedAtTickTime";
        }

    }
}
