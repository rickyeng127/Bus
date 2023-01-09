using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using System.Net;

namespace FORT.Bus.message
{
    public class HeartbeatService : IInfoListener{
        private static ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //private bool goingDown;

        private InfoService infoService = null;

        private Thread heartbeatSendingThread = null;
        private AutoResetEvent resetHeartbeatSendingThread = new AutoResetEvent(false);

        private int appId = -1;
        private int sendingInterval = -1;

        private string appLabel = null; 
        /// <summary>
        /// Event and eventhandler to notify interested listeners 
        /// of a new heartbeat
        /// </summary>
        /// <param name="p"></param>
        public delegate void NotifyNewHeartbeat(InfoMessage infoMessage);
        public event NotifyNewHeartbeat newHeartbeatEvent;

        public HeartbeatService(string applicationLabel, int domainId, int applicationId, int heartbeatSendingInterval)
            : this(applicationLabel, domainId, applicationId, heartbeatSendingInterval, true){
            
        }
        public HeartbeatService(string applicationLabel, int domainId, int applicationId, int heartbeatSendingInterval, bool startSendingHeartbeat){
            this.appLabel = string.Format("{0}_{1}", applicationLabel.ToUpper(), Dns.GetHostName());
            this.appId = applicationId;
            this.sendingInterval = heartbeatSendingInterval * 1000;

            this.infoService = new InfoService(domainId, Topics.HEARTBEAT);
            this.infoService.registerInfoListener(this);

            if(startSendingHeartbeat){
                this.startSendingHeartbeat();
            }
        }
        public void startSendingHeartbeat(){
            if (this.sendingInterval > 0){
                if (this.heartbeatSendingThread == null){
                    this.resetHeartbeatSendingThread.Reset();
                    // thread to handle sending of the heartbeats
                    this.heartbeatSendingThread = new Thread(new ThreadStart(this.sendingHeartbeat));
                    this.heartbeatSendingThread.IsBackground = true;
                    this.heartbeatSendingThread.Start();
                }
            }else{
                log.ErrorFormat("Unable to start sending heartbeats, the sending interval is negative <{0}>", this.sendingInterval);
            }
        }
        public void stopSendingHeartbeat(){
            this.resetHeartbeatSendingThread.Set();
        }
        private void sendingHeartbeat(){
            InfoMessage im = new InfoMessage();
            im.FromApp = this.appId;
            im.Data = this.appLabel;
            im.InfoCode = (int) CommandConstants.Info.HEARTBEAT;

            log.InfoFormat("Start sending heartbeat for the applicationId <{0}>", appId);
            do{
                this.infoService.sendInfoMessage(im);
            } while (!(this.resetHeartbeatSendingThread.WaitOne(this.sendingInterval, false)));
            this.heartbeatSendingThread = null;
            log.InfoFormat("Stop sending heartbeat for the applicationId <{0}>", appId);
        }

        public void sendAShutdownMessage(){
            InfoMessage im = new InfoMessage();
            im.FromApp = this.appId;
            im.Data = this.appLabel;
            im.InfoCode = (int)CommandConstants.Info.SHUTDOWN;

            log.InfoFormat("Sending a shutdown message for the applicationId <{0}>", appId);
            this.infoService.sendInfoMessage(im);
        }

        public void stopService(){
            //this.goingDown = true;
            try{
                this.infoService.stopService();
                this.stopSendingHeartbeat();
            }catch (Exception e){
                log.Error(e);
            }
        }

        /// <summary>
        /// Registers a new listener for heartbeat
        /// </summary>
        /// <param name="listener"></param>
        public void registerHeartbeatListener(IHeartbeatListener listener){
            this.newHeartbeatEvent += listener.NotifyNewHeartbeat;
        }

        public void removeHeartbeatListener(IHeartbeatListener listener){
            this.newHeartbeatEvent -= listener.NotifyNewHeartbeat;
        }

        /// <summary>
        /// Callback method from message queue that a new info message event has arrived
        /// </summary>
        /// <param name="im"></param>
        public void NotifyNewInfo(InfoMessage im){
            try {
                if(im.FromApp != this.appId){
                    if(this.newHeartbeatEvent != null){
                        this.newHeartbeatEvent(im);
                    }
                }
            } catch (Exception e) {
                log.Error(e);
            }
        }

    }
}
