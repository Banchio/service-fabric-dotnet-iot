﻿// ------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All rights reserved.
//  Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------

namespace FloorActor
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using IoTActor.Common;
    using Microsoft.ServiceFabric.Actors;
    using Newtonsoft.Json.Linq;

    public class FloorActor : StatefulActor<FloorActorState>, IIoTActor
    {
        private static string buildingActorService = "fabric:/IoTApplication/BuildingActor";
        private static string buildingActorIdFormat = "{0}-{1}-{2}";
        private IIoTActor buildingActor = null;

        public async Task Post(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            Task taskForward = this.ForwardToBuildingActor(DeviceId, EventHubName, ServiceBusNS, Body);

            /*
           The following are the chain in this samples
           Device->Floor->Building

           You can tailor the aggregators even further. for example
           Device->Floor->Building->Campus

           A floor actor in this sample represents end of the chain, you can use it
           to respond to events at the floor level (including all devices). 

           Note: aggregators should not store events, since raw events are stored
                 at the device level using Storage Actor. You can choose to store other 
                 data such as commands generated by floor actor.. 
           */

            // mean while you can do CEP to generate commands to devices. 

            await taskForward;
        }

        private IIoTActor CreateBuildingActor(string BuildingId, string EventHubName, string ServiceBusNS)
        {
            ActorId actorId = new ActorId(string.Format(buildingActorIdFormat, BuildingId, EventHubName, ServiceBusNS));
            return ActorProxy.Create<IIoTActor>(actorId, new Uri(buildingActorService));
        }

        private async Task ForwardToBuildingActor(string DeviceId, string EventHubName, string ServiceBusNS, byte[] Body)
        {
            if (null == this.buildingActor)
            {
                JObject j = JObject.Parse(Encoding.UTF8.GetString(Body));
                string BuildingId = j["BuildingId"].Value<string>();

                this.buildingActor = this.CreateBuildingActor(BuildingId, EventHubName, ServiceBusNS);
            }
            await this.buildingActor.Post(DeviceId, EventHubName, ServiceBusNS, Body);
        }
    }
}