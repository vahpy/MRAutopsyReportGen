using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

namespace HoloAutopsy.Record.Logging
{
    //[RequireComponent(typeof(RigBuilder))/*, ExecuteInEditMode*/]
    public class TransformRigLoggerHandler : MonoBehaviour, ObjectLogger
    {
        [SerializeField]private Transform[] joints = default;

        private void OnEnable()
        {
            print("Joint Count: " + joints.Length);
            foreach (Transform jt in joints)
            {
                print(jt);
            }
        }
        public void Call(string[] data)
        {
            throw new System.NotImplementedException();
        }

        public string Fetch(int frameNum)
        {
            throw new System.NotImplementedException();
        }

        public string GetName()
        {
            throw new System.NotImplementedException();
        }

        public void ResetChangeTrackers()
        {
            throw new System.NotImplementedException();
        }

        public void Undo()
        {
            throw new System.NotImplementedException();
        }
    }
}