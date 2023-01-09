using FORT.Collection;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FORT.Bus.message {
    
    /// <summary>
    /// Class that listens on the info topic and notifies listeners of a new info messages
    /// </summary>
    public class InfoService : IMessageListener {

        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        //private static InfoService instance; //daniel 20150909 remove the instance, will create a new object each time

        private bool goingDown;

        /// <summary>
        /// Thread for processing inbound messages/responses
        /// </summary>
        private Thread messageThread;

        /// <summary>
        /// queue for inbound messages/respones
        /// </summary>
        private BlockingQueue messageQueue = new BlockingQueue();

        private IMessagePublisher messagePublisher;     //daniel 20150909 add a publisher to send info message for the given topic
        private IMessageSubscriber messageSubscriber;
        private MappedMessage infoMappedMessage;

        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of a new info message
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyNewInfo(InfoMessage infoMessage);
        public event NotifyNewInfo newInfoEvent;

        //private InfoService() { //daniel 20150909
        public InfoService(int domainID, string topic){ //daniel 20150909
            this.initialize(domainID, topic);
        }
        //daniel 20150909 remove the getInstance function
        //public static InfoService getInstance(int domainID, string topic) {
        //    if (InfoService.instance == null) {
        //        InfoService.instance = new InfoService();
        //        InfoService.instance.initialize(domainID, listenTopic);
        //    }
        //    return InfoService.instance;
        //}

        public void processMessages() {
            while (!goingDown) {
                try {
                    MappedMessage mappedMessage = (MappedMessage)this.messageQueue.Dequeue();

                    if(mappedMessage != null){
                        // for command listeners, parse the command and notify listeners
                        InfoMessage im = new InfoMessage();

                        Dictionary<string, MessageField> f = mappedMessage.GetFields();
                        im.ID = (string) f[InfoMessage.InfoFields.ID].Value;
                        im.CreatedAtTickTime = (long)f[InfoMessage.InfoFields.CreatedAtTickTime].Value;
                        im.Data = (string)f[InfoMessage.InfoFields.Data].Value;
                        im.FromApp = (int)f[InfoMessage.InfoFields.FromApp].Value;
                        im.InfoCode = (int)f[InfoMessage.InfoFields.InfoCode].Value;
                    
                        log.DebugFormat("Received an info message = <{0}>", im.ToString());

                        // notify any listeners of the info message
                        if (newInfoEvent != null) {
                            newInfoEvent(im);
                        }
                    }
                } catch (Exception e) {
                    log.Error(e.Message, e);
                }
            }
        }

        /// <summary>
        /// Initializes the message listeners for new info messages
        /// </summary>
        /// <param name="domainID"></param>
        /// <param name="listenTopic"></param>
        private void initialize(int domainID, string topic) {

            // thread to handle inbound messages/responses
            this.messageThread = new Thread(new ThreadStart(this.processMessages));
            this.messageThread.IsBackground = true;
            this.messageThread.Start();

            // obtain handle to the message bus factory
            //MessageBusFactory mbf = new MessageBusFactory();       //daniel 20150909
            MessageBusFactory mbf = MessageBusFactory.getInstance(); //daniel 20150909

            // create a mapped message for the info messages
            this.infoMappedMessage = new MappedMessage();
            InfoMessage c = new InfoMessage();
            this.infoMappedMessage.initialize(c.GetType().Name, c);

            // create a message publisher for info messages
            this.messagePublisher = mbf.createPublisher(domainID, topic, this.infoMappedMessage);

            // create a message subscriber
            this.messageSubscriber = mbf.createSubscriber(domainID, topic, this.infoMappedMessage);
            this.messageSubscriber.registerMessageListener(this);

            log.InfoFormat("Created subscriber on domain = <{0}> : topic = <{1}>", domainID, topic);
        }

        /// <summary>
        /// Sends a info message
        /// </summary>
        /// <param name="im"></param>
        public void sendInfoMessage(InfoMessage im){
            try{
                this.infoMappedMessage.reset();
                this.infoMappedMessage.setValue(InfoMessage.InfoFields.InfoCode, im.InfoCode);
                this.infoMappedMessage.setValue(InfoMessage.InfoFields.Data, im.Data);
                this.infoMappedMessage.setValue(InfoMessage.InfoFields.FromApp, im.FromApp);
                this.infoMappedMessage.setValue(InfoMessage.InfoFields.CreatedAtTickTime, im.CreatedAtTickTime);
                this.infoMappedMessage.setValue(InfoMessage.InfoFields.ID, im.ID);
                log.DebugFormat("Sending a info message = <{0}>", im.ToString());
                this.messagePublisher.send(this.infoMappedMessage);
                log.DebugFormat("Sent a info message = <{0}>", im.ToString());
            }
            catch (Exception e)
            {
                log.Error(e);
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
        /// Registers a new listener for commands
        /// </summary>
        /// <param name="listener"></param>
        public void registerInfoListener(IInfoListener listener) {
            this.newInfoEvent += listener.NotifyNewInfo;
        }


        public void NotifyNewMessage(MappedMessage mappedMessage) {
            try {
                this.messageQueue.Enqueue(mappedMessage);
                log.Debug("received a new message");
            } catch (Exception e) {
                log.Error(e);
            }
        }

        public void NotifyMessageLost(impl.message.LostMessageStatus lostMessageStatus) {
            log.Error(lostMessageStatus.LostMessageReason);
        }
    }
}
