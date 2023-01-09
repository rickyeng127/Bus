using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net;

using FORT.Bus;
using FORT.Bus.message;
using System.Threading;
using FORT.Collection;

namespace FORT.Bus.message{
    
    /// <summary>
    /// Singleton service that sends commands and listens for responses.  Commands are optionally
    /// resent if they are not received within a period of time.
    /// 
    /// Class can be used bi-directionally: e.g.
    /// 
    ///     AutoTrade   - CommandService.getInstance(publish on --> commandTopic, receive on --> commandResponseTopic);
    ///     TickProgram - CommandService.getInstance(publish on --> commandResponseTopic, receive on --> commandTopic);
    /// </summary>
    public class CommandService : IMessageListener {

        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //private static CommandService instance; //daniel 20150909 remove the instance, will create a new object each time

        private IMessagePublisher messagePublisher;
        private IMessageSubscriber messageSubscriber;
        private MappedMessage commandMappedMessage;

        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of a new command
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyNewCommand(Command command);
        public event NotifyNewCommand newCommandEvent;

        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of a new command response
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyNewCommandResponse(Command command);
        public event NotifyNewCommandResponse newCommandResponseEvent;

        /// <summary>
        /// The amount of time between resends of a failed message
        /// </summary>
        private const int FAILED_RESEND_TIME_MILLIS = 10000;

        /// <summary>
        /// The sleep time between attempts to resend a persistent message
        /// </summary>
        private const int RETRY_SLEEP_MILLIS = 1000;

        private object lockThis = new object();
        private bool goingDown;

        /// <summary>
        /// Commands that require a command response.  If a command response is not detected, it is resent
        /// </summary>
        private Dictionary<string, Command> persistentCommands = new Dictionary<string, Command>();

        /// <summary>
        /// Thread for sending persistent retries
        /// </summary>
        private Thread retryThread;

        /// <summary>
        /// Thread for processing inbound messages/responses
        /// </summary>
        private Thread messageThread;

        /// <summary>
        /// queue for inbound messages/respones
        /// </summary>
        private BlockingQueue messageQueue = new BlockingQueue();


        public CommandService(int domainID, string sendTopic, string listenTopic){
            this.initialize(domainID, sendTopic, listenTopic);
        }

        //daniel 20150909 remove the getInstance function
        //public static CommandService getInstance(int domainID, string sendTopic, string listenTopic) {
        //    //if (CommandService.instance == null) {
        //        CommandService.instance = new CommandService();
        //        CommandService.instance.initialize(domainID, sendTopic, listenTopic);
        //    //}
        //    return CommandService.instance;
        //}

        /// <summary>
        /// Initializes the message publisher for commands the the message subscriber for command responses
        /// </summary>
        /// <param name="domainID"></param>
        /// <param name="sendTopic"></param>
        /// <param name="listenTopic"></param>
        private void initialize(int domainID, string sendTopic, string listenTopic) {
            
            // thread to handle inbound messages/responses
            this.messageThread = new Thread(new ThreadStart(this.processMessages));
            this.messageThread.IsBackground = true;
            this.messageThread.Start();
            
            // start the thread for retries
            this.retryThread = new Thread(new ThreadStart(this.retrySend));
            this.retryThread.IsBackground = true;
            this.retryThread.Start();
            
            // obtain handle to the message bus factory
            MessageBusFactory mbf = MessageBusFactory.getInstance();

            // create a message publisher for commands
            this.commandMappedMessage = new MappedMessage();
            Command c = new Command();
            this.commandMappedMessage.initialize(c.GetType().Name, c);
            this.messagePublisher = mbf.createPublisher(domainID, sendTopic, this.commandMappedMessage);

            log.InfoFormat("Created publisher on domain = <{0}> : topic = <{1}>", domainID, sendTopic);

            // create a message subscriber for respones
            this.messageSubscriber = mbf.createSubscriber(domainID, listenTopic, this.commandMappedMessage);
            this.messageSubscriber.registerMessageListener(this);

            log.InfoFormat("Created subscriber on domain = <{0}> : topic = <{1}>", domainID, listenTopic);
        }

        /// <summary>
        /// Registers a new listener for commands
        /// </summary>
        /// <param name="listener"></param>
        public void registerCommandListener(ICommandListener listener) {
            this.newCommandEvent += listener.NotifyNewCommand;
        }

        public void removeCommandListener(ICommandListener listener) {
            this.newCommandEvent -= listener.NotifyNewCommand;
        }

        public void registerResponseListener(IResponseListener listener) {
            this.newCommandResponseEvent += listener.NotifyNewResponse;
        }

        public void removeResponseListener(IResponseListener listener) {
            this.newCommandResponseEvent -= listener.NotifyNewResponse;
        }


        /// <summary>
        /// Sends a non-persistent command
        /// </summary>
        /// <param name="c"></param>
        public void sendCommand(Command c) {
            try {
                this.commandMappedMessage.reset();
                this.commandMappedMessage.setValue(CommandFields.CommandCode, c.CommandCode);
                this.commandMappedMessage.setValue(CommandFields.CreatedAtTickTime, c.CreatedAtTickTime);
                this.commandMappedMessage.setValue(CommandFields.Data, c.Data);
                this.commandMappedMessage.setValue(CommandFields.FromApp, c.FromApp);
                this.commandMappedMessage.setValue(CommandFields.ID, c.ID);
                this.commandMappedMessage.setValue(CommandFields.ToApp, c.ToApp);
                
                // set the time in which message was sent
                c.SentAtTickTime = DateTime.Now.Ticks;
                this.commandMappedMessage.setValue(CommandFields.SentAtTickTime, c.SentAtTickTime);

                this.messagePublisher.send(this.commandMappedMessage);

                log.DebugFormat("Sent a command = <{0}>", c.ToString());

                //Thread.Sleep(5); // sleep prevents overwhelming the message bus
            } catch (Exception e) {
                log.Error(e);
            }
        }

        /// <summary>
        /// Thread is used to resend persistent commands where we have not received 
        /// </summary>
        private void retrySend() {
            while (!this.goingDown) {
                try {
                    lock (this.lockThis) {
                        List<string> ids = this.persistentCommands.Keys.ToList();
                        foreach (string id in ids) {
                            if (this.persistentCommands.ContainsKey(id)) {
                                Command c = this.persistentCommands[id];

                                // check to see if the elapsed time between retries has been exceeded before resending
                                if (c.millisSinceLastSend() > FAILED_RESEND_TIME_MILLIS) {
                                    c.SendCount++;
                                    sendCommand(c);

                                    // if we've reached the max send count, remove
                                    if (c.SendCount >= c.MaxSendCount) {
                                        this.persistentCommands.Remove(id);
                                    }
                                }
                            }
                        }
                    }
                } catch (Exception e) {
                    log.Error(e);
                } finally {
                    Thread.Sleep(RETRY_SLEEP_MILLIS);
                }
            }
        }

        public void stopService() {
            this.goingDown = true;
            this.messageQueue.Clear();
            this.messageQueue.unblock();
            try {
                this.messagePublisher.shutdown();
                this.messageSubscriber.shutdown();
            } catch (Exception e) {
                log.Error(e);
            }
        }

        /// <summary>
        /// Sends a persistent command
        /// </summary>
        /// <param name="c"></param>
        public void sendPersistent(Command c, int maxSendCount) {
            lock (this.lockThis) {
                // cache the retry parameters
                c.MaxSendCount = maxSendCount;
                c.SendCount = 1;
                // send message
                sendCommand(c);
                // if we have not reached retry limit, cache for resending
                if (c.SendCount < c.MaxSendCount) {
                    if (this.persistentCommands.ContainsKey(c.ID)) {
                        this.persistentCommands[c.ID] = c;
                    } else {
                        this.persistentCommands.Add(c.ID, c);
                    }
                }
            }
        }

        /// <summary>
        /// Inbound messages are notified here.  
        /// 
        /// This method is dual purposed:
        ///     1. For command senders, command responses are received here.
        ///        If the message ID is contained within the persistent messages, resend
        ///     2. For command listeners, commands are received here.
        ///        It will notify any listeners of the command
        /// </summary>
        /// <param name="mappedMessage"></param>
        public void NotifyNewMessage(MappedMessage mappedMessage) {
            try {
                this.messageQueue.Enqueue(mappedMessage);
                log.Debug("received a new message");
            } catch (Exception e) {
                log.Error(e);
            }
        }



        public void processMessages() {
            while (!goingDown) {
                try {
                    MappedMessage mappedMessage = (MappedMessage)this.messageQueue.Dequeue();

                    if(mappedMessage != null){
                        // for command senders, remove the original command if it exists
                        lock (this.lockThis) {
                            string id = (string)mappedMessage.GetFields()[CommandFields.ID].Value;
                            if (id!=null && this.persistentCommands.ContainsKey(id)) {
                                this.persistentCommands.Remove(id);
                                log.DebugFormat("persistentCommand count = {0}", this.persistentCommands.Count);
                            }
                        }

                        // for command listeners, parse the command and notify listeners
                        Command c = new Command();

                        Dictionary<string, MessageField> f = mappedMessage.GetFields();
                        c.CommandCode = (int)f[CommandFields.CommandCode].Value;
                        c.CreatedAtTickTime = (long) f[CommandFields.CreatedAtTickTime].Value;
                        c.Data = (string) f[CommandFields.Data].Value;
                        c.FromApp = (int) f[CommandFields.FromApp].Value;
                        c.ID = (string) f[CommandFields.ID].Value;
                        c.ToApp = (int) f[CommandFields.ToApp].Value;
                        c.SendCount = (int) f[CommandFields.SendCount].Value;
                        c.MaxSendCount = (int) f[CommandFields.MaxSendCount].Value;

                        log.DebugFormat("Received a command/response = <{0}>", c.ToString());

                        // notify any listeners of the command
                        if (newCommandEvent != null) {
                            newCommandEvent(c);
                        }

                        // notify any listeners of the command response
                        if (newCommandResponseEvent != null) {
                            newCommandResponseEvent(c);
                        }
                    }
                } catch (Exception e) {
                    log.Error(e.Message, e);
                }
            }
        }

        public void NotifyMessageLost(FORT.Bus.impl.message.LostMessageStatus lostMessageStatus) {
            log.Error(lostMessageStatus.LostMessageReason);
        }
    }
}
