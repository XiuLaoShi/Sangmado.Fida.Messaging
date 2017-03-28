﻿using System;
using System.Collections.Concurrent;
using System.Linq;
using Sangmado.Inka.Extensions;
using Sangmado.Inka.Logging;

namespace Sangmado.Fida.ServiceModel
{
    public class ActorListenerChannel : IActorChannel
    {
        private ILog _log = Logger.Get<ActorListenerChannel>();
        private ActorDescription _localActor = null;
        private ActorTransportListener _listener = null;
        private IActorMessageEncoder _encoder;
        private IActorMessageDecoder _decoder;
        private ConcurrentDictionary<string, ActorDescription> _remoteActors = new ConcurrentDictionary<string, ActorDescription>(); // SessionKey -> Actor
        private ConcurrentDictionary<string, string> _actorKeys = new ConcurrentDictionary<string, string>(); // ActorKey -> SessionKey

        public ActorListenerChannel(
            ActorDescription localActor, ActorTransportListener localListener,
            IActorMessageEncoder encoder, IActorMessageDecoder decoder)
        {
            if (localActor == null)
                throw new ArgumentNullException("localActor");
            if (localListener == null)
                throw new ArgumentNullException("localListener");
            if (encoder == null)
                throw new ArgumentNullException("encoder");
            if (decoder == null)
                throw new ArgumentNullException("decoder");

            _localActor = localActor;
            _listener = localListener;
            _encoder = encoder;
            _decoder = decoder;
        }

        public bool Active
        {
            get
            {
                if (_listener == null)
                    return false;
                else
                    return _listener.IsListening;
            }
        }

        public void Open()
        {
            if (_listener.IsListening)
                return;

            _listener.Connected += OnConnected;
            _listener.Disconnected += OnDisconnected;
            _listener.DataReceived += OnDataReceived;

            _listener.Start();
        }

        public void Close()
        {
            _listener.Connected -= OnConnected;
            _listener.Disconnected -= OnDisconnected;
            _listener.DataReceived -= OnDataReceived;

            _listener.Stop();

            _remoteActors.Clear();
            _actorKeys.Clear();
        }

        private void Handshake(ActorTransportDataReceivedEventArgs e)
        {
            var actorHandshakeRequest = _decoder.Decode<ActorHandshakeRequest>(e.Data, e.DataOffset, e.DataLength);
            var remoteActor = actorHandshakeRequest.ActorDescription;
            if (remoteActor == null)
            {
                _log.ErrorFormat("Handshake with remote [{0}] failed, invalid actor description.", e.SessionKey);
                _listener.CloseSession(e.SessionKey);
            }
            else
            {
                var actorHandshakeResponse = new ActorHandshakeResponse()
                {
                    ActorDescription = _localActor,
                };
                var actorHandshakeResponseBuffer = _encoder.Encode(actorHandshakeResponse);
                _listener.SendToAsync(e.SessionKey, actorHandshakeResponseBuffer);

                _log.InfoFormat("Handshake with remote [{0}] successfully, SessionKey[{1}].", remoteActor, e.SessionKey);
                _remoteActors.Add(e.SessionKey, remoteActor);
                _actorKeys.Add(remoteActor.GetKey(), e.SessionKey);

                if (Connected != null)
                {
                    Connected(this, new ActorConnectedEventArgs(e.SessionKey, remoteActor));
                }
            }
        }

        private void OnConnected(object sender, ActorTransportConnectedEventArgs e)
        {
        }

        private void OnDisconnected(object sender, ActorTransportDisconnectedEventArgs e)
        {
            ActorDescription remoteActor = null;
            if (_remoteActors.TryRemove(e.SessionKey, out remoteActor))
            {
                _actorKeys.Remove(remoteActor.GetKey());
                _log.InfoFormat("Disconnected with remote [{0}], SessionKey[{1}].", remoteActor, e.SessionKey);

                if (Disconnected != null)
                {
                    Disconnected(this, new ActorDisconnectedEventArgs(e.SessionKey, remoteActor));
                }
            }
        }

        private void OnDataReceived(object sender, ActorTransportDataReceivedEventArgs e)
        {
            var remoteActor = _remoteActors.Get(e.SessionKey);
            if (remoteActor != null)
            {
                if (DataReceived != null)
                {
                    DataReceived(this, new ActorDataReceivedEventArgs(e.SessionKey, remoteActor, e.Data, e.DataOffset, e.DataLength));
                }
            }
            else
            {
                Handshake(e);
            }
        }

        public event EventHandler<ActorConnectedEventArgs> Connected;
        public event EventHandler<ActorDisconnectedEventArgs> Disconnected;
        public event EventHandler<ActorDataReceivedEventArgs> DataReceived;

        public void Send(string actorType, string actorName, byte[] data)
        {
            var actorKey = ActorDescription.GetKey(actorType, actorName);
            var sessionKey = _actorKeys.Get(actorKey);
            if (!string.IsNullOrEmpty(sessionKey))
            {
                _listener.SendTo(sessionKey, data);
            }
        }

        public void Send(string actorType, string actorName, byte[] data, int offset, int count)
        {
            var actorKey = ActorDescription.GetKey(actorType, actorName);
            var sessionKey = _actorKeys.Get(actorKey);
            if (!string.IsNullOrEmpty(sessionKey))
            {
                _listener.SendTo(sessionKey, data, offset, count);
            }
        }

        public void SendAsync(string actorType, string actorName, byte[] data)
        {
            var actorKey = ActorDescription.GetKey(actorType, actorName);
            var sessionKey = _actorKeys.Get(actorKey);
            if (!string.IsNullOrEmpty(sessionKey))
            {
                _listener.SendToAsync(sessionKey, data);
            }
        }

        public void SendAsync(string actorType, string actorName, byte[] data, int offset, int count)
        {
            var actorKey = ActorDescription.GetKey(actorType, actorName);
            var sessionKey = _actorKeys.Get(actorKey);
            if (!string.IsNullOrEmpty(sessionKey))
            {
                _listener.SendToAsync(sessionKey, data, offset, count);
            }
        }

        public void Send(string actorType, byte[] data)
        {
            var actor = _remoteActors.Values.Where(a => a.Type == actorType).OrderBy(t => Guid.NewGuid()).FirstOrDefault();
            if (actor != null)
            {
                var sessionKey = _actorKeys.Get(actor.GetKey());
                _listener.SendTo(sessionKey, data);
            }
        }

        public void Send(string actorType, byte[] data, int offset, int count)
        {
            var actor = _remoteActors.Values.Where(a => a.Type == actorType).OrderBy(t => Guid.NewGuid()).FirstOrDefault();
            if (actor != null)
            {
                var sessionKey = _actorKeys.Get(actor.GetKey());
                _listener.SendTo(sessionKey, data, offset, count);
            }
        }

        public void SendAsync(string actorType, byte[] data)
        {
            var actor = _remoteActors.Values.Where(a => a.Type == actorType).OrderBy(t => Guid.NewGuid()).FirstOrDefault();
            if (actor != null)
            {
                var sessionKey = _actorKeys.Get(actor.GetKey());
                _listener.SendToAsync(sessionKey, data);
            }
        }

        public void SendAsync(string actorType, byte[] data, int offset, int count)
        {
            var actor = _remoteActors.Values.Where(a => a.Type == actorType).OrderBy(t => Guid.NewGuid()).FirstOrDefault();
            if (actor != null)
            {
                var sessionKey = _actorKeys.Get(actor.GetKey());
                _listener.SendToAsync(sessionKey, data, offset, count);
            }
        }
    }
}
