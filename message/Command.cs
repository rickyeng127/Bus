using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    /// <summary>
    /// Command object used to serialize a command to another application
    /// over the message bus.  A command and a command response use this same class.
    /// </summary>
    public class Command {

        /// <summary>
        /// Used to uniquely identify a command on the network
        /// </summary>
        private string guid;

        /// <summary>
        /// The time (in ticks) in which this command was created
        /// </summary>
        private long createdAtTickTime;

        public Command() {
            this.guid = System.Guid.NewGuid().ToString();
            this.createdAtTickTime = DateTime.Now.Ticks;
        }

        /// <summary>
        /// The data for this command
        /// </summary>
        [MaxLengthAttribute(1024)]
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
        /// The sending applications, returns an enumeration value from CommandConstants.Applications
        /// </summary>
        public int FromApp {
            get;
            set;
        }

        /// <summary>
        /// The target application, returns an enumeration value from CommandConstants.Applications
        /// </summary>
        public int ToApp {
            get;
            set;
        }

        /// <summary>
        /// The command, returns an enumeration value from CommandConstants.Commands
        /// </summary>
        public int CommandCode {
            get;
            set;
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
        /// The time that the message was sent over the message bus
        /// </summary>
        public long SentAtTickTime {
            get;
            set;
        }

        /// <summary>
        /// Returns the total milliseconds since the last send
        /// </summary>
        /// <returns></returns>
        public double millisSinceLastSend() {
            TimeSpan ts = new TimeSpan(DateTime.Now.Ticks - SentAtTickTime);
            return ts.TotalMilliseconds;
        }

        /// <summary>
        /// To support persistent command, retries
        /// </summary>
        public int MaxSendCount {
            get;
            set;
        }

        public int SendCount {
            get;
            set;
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("CommandCode = ").Append(((CommandConstants.Commands)this.CommandCode).ToString())
              .Append(" : From = ").Append(((CommandConstants.Applications)this.FromApp).ToString())
              .Append(" : To = ").Append(((CommandConstants.Applications)this.ToApp).ToString())
              .Append(" : Data = <").Append(this.Data).Append(">")
              .Append(" : MaxSendCount = ").Append(this.MaxSendCount)
              .Append(" : SendCount = ").Append(this.SendCount)
              .Append(" : ID = ").Append(this.ID);
            return sb.ToString();
        }
    }

    /// <summary>
    /// The fields within the SlimTick object
    /// </summary>
    public class CommandFields {
        public static readonly string Data = "Data";
        public static readonly string FromApp = "FromApp";
        public static readonly string ToApp = "ToApp";
        public static readonly string CommandCode = "CommandCode";
        public static readonly string ID = "ID";
        public static readonly string CreatedAtTickTime = "CreatedAtTickTime";
        public static readonly string SentAtTickTime = "SentAtTickTime";
        public static readonly string MaxSendCount = "MaxSendCount";
        public static readonly string SendCount = "SendCount";
    }
}
