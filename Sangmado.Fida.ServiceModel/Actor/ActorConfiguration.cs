﻿using System;
using Sangmado.Inka.Configuration;

namespace Sangmado.Fida.ServiceModel
{
    public class ActorConfiguration
    {
        public const string ActorCenterTypeKey = @"ActorCenterType";
        public const string ActorCenterNameKey = @"ActorCenterName";
        public const string ActorCenterAddressKey = @"ActorCenterAddress";
        public const string ActorCenterPortKey = @"ActorCenterPort";
        public const string ActorTypeKey = @"ActorType";
        public const string ActorNameKey = @"ActorName";
        public const string ActorAddressKey = @"ActorAddress";
        public const string ActorPortKey = @"ActorPort";

        private IAppSetting _appSetting = AppSetting.Singleton();
        private IActorMessageEncoder _encoder;
        private IActorMessageDecoder _decoder;
        private ActorDescription _centerActor;
        private ActorDescription _localActor;

        public ActorConfiguration(IActorMessageEncoder encoder, IActorMessageDecoder decoder)
        {
            if (encoder == null)
                throw new ArgumentNullException("encoder");
            if (decoder == null)
                throw new ArgumentNullException("decoder");

            _encoder = encoder;
            _decoder = decoder;
        }

        public IActorMessageEncoder Encoder { get { return _encoder; } }
        public IActorMessageDecoder Decoder { get { return _decoder; } }
        public ActorDescription CenterActor { get { return _centerActor; } }
        public ActorDescription LocalActor { get { return _localActor; } }

        public void Build()
        {
            _centerActor = BuildCenterActor();
            _localActor = BuildLocalActor();
        }

        private ActorDescription BuildCenterActor()
        {
            var actorType = _appSetting.GetItem<string>(ActorCenterTypeKey);
            var actorName = _appSetting.GetItem<string>(ActorCenterNameKey);
            var actorAddress = _appSetting.GetItem<string>(ActorCenterAddressKey);
            var actorPort = _appSetting.GetItem<string>(ActorCenterPortKey);

            var actor = new ActorDescription(actorType, actorName);
            actor.Address = actorAddress;
            actor.Port = actorPort;

            return actor;
        }

        private ActorDescription BuildLocalActor()
        {
            var actorType = _appSetting.GetItem<string>(ActorTypeKey);
            var actorName = _appSetting.GetItem<string>(ActorNameKey);
            var actorAddress = _appSetting.GetItem<string>(ActorAddressKey);
            var actorPort = _appSetting.GetItem<string>(ActorPortKey);

            var actor = new ActorDescription(actorType, actorName);
            actor.Address = actorAddress;
            actor.Port = actorPort;

            return actor;
        }
    }
}
