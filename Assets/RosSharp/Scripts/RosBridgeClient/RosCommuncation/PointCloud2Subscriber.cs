﻿using RosSharp.RosBridgeClient.Messages.Sensor;
using UnityEngine;

namespace RosSharp.RosBridgeClient
{
    public class PointCloud2Subscriber : Subscriber<Messages.Sensor.PointCloud2>
    {
    
        public uint width, height;

        public PointField[] fields;

        public byte[] data;


        protected override void ReceiveMessage(PointCloud2 message)
        {
            this.width = message.width;
            this.height = message.height;
            this.fields = message.fields;
            this.data = message.data;
        }
    }
}
