﻿using Sangmado.Fida.ServiceModel;

namespace Sangmado.Fida.Messaging
{
    public class ActorMessageEncoder : IActorMessageEncoder
    {
        private IMessageEncoder _encoder;

        public ActorMessageEncoder(IMessageEncoder encoder)
        {
            _encoder = encoder;
        }

        public byte[] EncodeMessage<T>(T messageData)
        {
            return _encoder.EncodeMessage(messageData);
        }

        public byte[] Encode<T>(T messageData)
        {
            var message = new ActorMessageEnvelope()
            {
                MessageType = typeof(T).Name,
                MessageData = EncodeMessage(messageData),
            };
            return _encoder.EncodeMessage(message);
        }
    }
}
