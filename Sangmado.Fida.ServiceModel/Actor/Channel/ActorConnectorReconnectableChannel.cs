﻿using System;
using System.Threading;
using Sangmado.Inka.Logging;

namespace Sangmado.Fida.ServiceModel
{
    public class ActorConnectorReconnectableChannel : ActorConnectorChannel
    {
        private ILog _log = Logger.Get<ActorConnectorReconnectableChannel>();
        private System.Threading.Timer _retryTimer = null;
        private readonly object _retryLock = new object();

        public ActorConnectorReconnectableChannel(
            ActorDescription localActor, ActorTransportConnector remoteConnector,
            IActorMessageEncoder encoder, IActorMessageDecoder decoder)
            : base(localActor, remoteConnector, encoder, decoder)
        {
            this.RetryPeriod = TimeSpan.FromSeconds(15);
        }

        public TimeSpan RetryPeriod { get; private set; }

        protected override void OnClose()
        {
            base.OnClose();
            CloseRetryTimer();
        }

        protected override void OnConnected(object sender, ActorTransportConnectedEventArgs e)
        {
            base.OnConnected(sender, e);
            CloseRetryTimer();
        }

        protected override void OnDisconnected(object sender, ActorTransportDisconnectedEventArgs e)
        {
            base.OnDisconnected(sender, e);
            SetupRetryTimer();
        }

        private void SetupRetryTimer()
        {
            lock (_retryLock)
            {
                _log.InfoFormat(string.Format("SetupRetryTimer, setup retry timer [{0}] when connect to [{1}].",
                    this.RetryPeriod, this.ConnectToEndPoint));

                if (_retryTimer == null)
                {
                    _retryTimer = new System.Threading.Timer(
                      (s) =>
                      {
                          try
                          {
                              Open();
                          }
                          catch (Exception ex)
                          {
                              _log.Error(ex.Message, ex);
                          }
                      },
                      null, this.RetryPeriod, this.RetryPeriod);
                }
            }
        }

        private void CloseRetryTimer()
        {
            lock (_retryLock)
            {
                _log.InfoFormat(string.Format("CloseRetryTimer, close retry timer [{0}] to connect [{1}].",
                    this.RetryPeriod, this.ConnectToEndPoint));

                if (_retryTimer != null)
                {
                    _retryTimer.Change(Timeout.Infinite, Timeout.Infinite);
                    _retryTimer.Dispose();
                    _retryTimer = null;
                }
            }
        }
    }
}
