﻿using System;
using System.Net;
using Cowboy.Sockets;
using Sangmado.Inka.Logging;

namespace Sangmado.Fida.ServiceModel
{
    public class ActorTransportConnector
    {
        private ILog _log = Logger.Get<ActorTransportConnector>();
        private TcpSocketClient _client;

        public ActorTransportConnector(IPEndPoint connectToEndPoint)
        {
            if (connectToEndPoint == null)
                throw new ArgumentNullException("connectToEndPoint");

            this.ConnectToEndPoint = connectToEndPoint;
        }

        public IPEndPoint ConnectToEndPoint { get; private set; }
        public bool IsConnected { get { return _client == null ? false : _client.State == TcpSocketConnectionState.Connected; } }

        public void Connect()
        {
            Connect(TimeSpan.FromSeconds(5));
        }

        public void Connect(TimeSpan timeout)
        {
            if (IsConnected)
                return;
            if (this.ConnectToEndPoint.Address.Equals(IPAddress.None)
                || this.ConnectToEndPoint.Address.Equals(IPAddress.IPv6None))
                return;

            try
            {
                var configuration = new TcpSocketClientConfiguration()
                {
                    ConnectTimeout = timeout,
                    SendTimeout = TimeSpan.FromMinutes(1),
                    ReceiveTimeout = TimeSpan.Zero,
                    KeepAlive = true,
                };
                _client = new TcpSocketClient(this.ConnectToEndPoint, configuration);
                _client.ServerConnected += OnServerConnected;
                _client.ServerDisconnected += OnServerDisconnected;
                _client.ServerDataReceived += OnServerDataReceived;

                _log.InfoFormat("TCP client is connecting to [{0}].", this.ConnectToEndPoint);
                _client.Connect();

                OnConnect();
            }
            catch
            {
                _client.ServerConnected -= OnServerConnected;
                _client.ServerDisconnected -= OnServerDisconnected;
                _client.ServerDataReceived -= OnServerDataReceived;
                _client.Close();
                _client = null;

                throw;
            }
        }

        public void Disconnect()
        {
            if (!IsConnected)
                return;
            if (this.ConnectToEndPoint.Address.Equals(IPAddress.None)
                || this.ConnectToEndPoint.Address.Equals(IPAddress.IPv6None))
                return;

            try
            {
                _client.ServerConnected -= OnServerConnected;
                _client.ServerDisconnected -= OnServerDisconnected;
                _client.ServerDataReceived -= OnServerDataReceived;
                _client.Close();
                _client = null;
            }
            catch { }
            finally
            {
                OnDisconnect();
            }
        }

        protected virtual void OnConnect()
        {
        }

        protected virtual void OnDisconnect()
        {
        }

        protected virtual void OnServerConnected(object sender, TcpServerConnectedEventArgs e)
        {
            _log.InfoFormat("TCP server [{0}] has connected.", e);

            if (Connected != null)
            {
                Connected(this, new ActorTransportConnectedEventArgs(this.ConnectToEndPoint.ToString()));
            }
        }

        protected virtual void OnServerDisconnected(object sender, TcpServerDisconnectedEventArgs e)
        {
            _log.InfoFormat("TCP server [{0}] has disconnected.", e);

            if (Disconnected != null)
            {
                Disconnected(this, new ActorTransportDisconnectedEventArgs(this.ConnectToEndPoint.ToString()));
            }
        }

        protected virtual void OnServerDataReceived(object sender, TcpServerDataReceivedEventArgs e)
        {
            if (DataReceived != null)
            {
                DataReceived(this, new ActorTransportDataReceivedEventArgs(this.ConnectToEndPoint.ToString(), e.Data, e.DataOffset, e.DataLength));
            }
        }

        public void Send(byte[] data)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The client has not connected to server.");

            _client.Send(data);
        }

        public void Send(byte[] data, int offset, int count)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The client has not connected to server.");

            _client.Send(data, offset, count);
        }

        public void SendAsync(byte[] data)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The client has not connected to server.");

            _client.BeginSend(data);
        }

        public void SendAsync(byte[] data, int offset, int count)
        {
            if (!IsConnected)
                throw new InvalidOperationException("The client has not connected to server.");

            _client.BeginSend(data, offset, count);
        }

        public event EventHandler<ActorTransportConnectedEventArgs> Connected;
        public event EventHandler<ActorTransportDisconnectedEventArgs> Disconnected;
        public event EventHandler<ActorTransportDataReceivedEventArgs> DataReceived;
    }
}
