using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace HoloAutopsy.Utils
{
    public class SerializationUtils
    {
        public static byte[] PoseToByteArray(Vector3 pos, Quaternion rot)
        {
            byte[] positionBytes = BitConverter.GetBytes(pos.x)
                .Concat(BitConverter.GetBytes(pos.y))
                .Concat(BitConverter.GetBytes(pos.z))
                .ToArray();
            byte[] rotationBytes = BitConverter.GetBytes(rot.x)
                .Concat(BitConverter.GetBytes(rot.y))
                .Concat(BitConverter.GetBytes(rot.z))
                .Concat(BitConverter.GetBytes(rot.w))
                .ToArray();
            return positionBytes.Concat(rotationBytes).ToArray();
        }

        public static Pose ByteArrayToPose(byte[] data)
        {

            Vector3 position = new Vector3(
                BitConverter.ToSingle(data, 0),
                BitConverter.ToSingle(data, 4),
                BitConverter.ToSingle(data, 8)
            );
            Quaternion rotation = new Quaternion(
                BitConverter.ToSingle(data, 12),
                BitConverter.ToSingle(data, 16),
                BitConverter.ToSingle(data, 20),
                BitConverter.ToSingle(data, 24)
            );
            return new Pose(position, rotation);
        }
    }
}
