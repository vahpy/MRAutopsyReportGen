/* Copyright (c) 2020 ExT (V.Sigalkin)
 * Customised by IALab at Monash University
 */

using UnityEngine;


using extOSC.Core.Events;

namespace extOSC.Components.Events
{
    [AddComponentMenu("extOSC/Components/Receiver/Customised Transform Event")]

    public class CustomisedOSCReceiverEventTransform : OSCReceiverEvent<OSCEventVector3>
    {

        #region Protected Methods

        protected override void Invoke(OSCMessage message)
        {
            if (onReceive != null && message.ToVector3(out var value))  //To vector3 works for transform, but rotation is in a 3x3 rotation  matrix
            {
                this.transform.position = new Vector3(-value.y * 0.001f, value.z * 0.001f, value.x * 0.001f);

                string theMessage = message.ToString();
                if (!theMessage.Contains("unlabeled"))
                {
                    string[] splitMessage = theMessage.Split('"');

                    Vector3 yVector = new Vector3(float.Parse(splitMessage[11]), float.Parse(splitMessage[17]), float.Parse(splitMessage[23])); // get Vicon's y and z rotations from the rotation matrix
                    Vector3 zVector = new Vector3(float.Parse(splitMessage[13]), float.Parse(splitMessage[19]), float.Parse(splitMessage[25]));

                    // taken from vicon rb script
                    Quaternion theQuaternion = Quaternion.LookRotation(zVector, yVector); // https://stackoverflow.com/questions/53447104/how-to-apply-transformation-using-3x3-rotation-matrix-and-a-translation-vector answer2
                    this.transform.rotation = new Quaternion(theQuaternion[1], -theQuaternion[2], -theQuaternion[0], theQuaternion[3]); // taken from rb script
                }
            }
        }
        #endregion
    }
}