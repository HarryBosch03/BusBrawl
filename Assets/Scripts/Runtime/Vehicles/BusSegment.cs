using System;
using UnityEngine;

namespace Runtime.Vehicles
{
    public class BusSegment : MonoBehaviour
    {
        public Transform vehicle;
        public Transform interpolation;
        public Transform visuals;
        public float frontOffset;
        public float rearOffset;
        public float maxRollDeviation;
        public float lastCalcAngle;

        private Vector3 lerpPosition0;
        private Vector3 lerpPosition1;
        private Quaternion lerpRotation0;
        private Quaternion lerpRotation1;
        
        private void LateUpdate()
        {
            //var t = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
            //visuals.position = Vector3.Lerp(lerpPosition1, lerpPosition0, t);
            //visuals.rotation = Quaternion.Slerp(lerpRotation1, lerpRotation0, t);
        }

        private void FixedUpdate()
        {
            lerpPosition1 = lerpPosition0;
            lerpPosition0 = transform.position;

            lerpRotation1 = lerpRotation0;
            lerpRotation0 = transform.rotation;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow; 
            Gizmos.DrawSphere(transform.position + transform.forward * frontOffset, 0.2f);
            Gizmos.DrawSphere(transform.position - transform.forward * frontOffset, 0.2f);
        }
    }
}