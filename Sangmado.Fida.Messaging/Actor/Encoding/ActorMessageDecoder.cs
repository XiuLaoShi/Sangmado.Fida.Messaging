﻿using Sangmado.Fida.ServiceModel;

namespace Sangmado.Fida.Messaging
{
    public class ActorMessageDecoder : IActorMessageDecoder
    {
        private IMessageDecoder _decoder;

        public ActorMessageDecoder(IMessageDecoder decoder)
        {
            _decoder = decoder;
        }

        public IActorMessageEnvelope DecodeEnvelope(byte[] data, int offset, int count)
        {
            return _decoder.DecodeMessage<ActorMessageEnvelope>(data, offset, count);
        }

        public T DecodeMessage<T>(byte[] data, int offset, int count)
        {
            return _decoder.DecodeMessage<T>(data, offset, count);
        }

        public T Decode<T>(byte[] data, int offset, int count)
        {
            var envelope = DecodeEnvelope(data, offset, count);
            return DecodeMessage<T>(envelope.MessageData, 0, envelope.MessageData.Length);
        }
    }
}
