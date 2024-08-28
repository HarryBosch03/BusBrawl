using System;
using UnityEngine;

namespace Runtime.Vehicles
{
    public class BusSegment : MonoBehaviour
    {
        public Transform vehicle;
        public Transform interpolation;
        public Transform visuals;
        public Transform anchorFront;
        public Transform anchorEnd;
        public float rollSpeed;

        private Vector3 lerpPosition0;
        private Vector3 lerpPosition1;
        private Quaternion lerpRotation0;
        private Quaternion lerpRotation1;
        private float lerpTimer;

        public void Move(Vector3 following, Vector3 up)
        {
            var newUp = Vector3.Slerp(transform.up, up, rollSpeed * Time.deltaTime);
            
            transform.LookAt(following, newUp);
            var delta = following - anchorFront.position;
            transform.position += delta;

            lerpPosition1 = lerpPosition0;
            lerpRotation1 = lerpRotation0;
            
            lerpPosition0 = transform.position;
            lerpRotation0 = transform.rotation;

            lerpTimer = 0f;
        }

        private void LateUpdate()
        {
            visuals.position = Vector3.LerpUnclamped(lerpPosition1 ,lerpPosition0, lerpTimer / Time.fixedDeltaTime);
            visuals.rotation = Quaternion.SlerpUnclamped(lerpRotation1, lerpRotation0, lerpTimer / Time.fixedDeltaTime);
            lerpTimer += Time.deltaTime;
        }
    }
}