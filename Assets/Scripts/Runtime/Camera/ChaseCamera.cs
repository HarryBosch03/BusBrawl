﻿using UnityEngine;

namespace Runtime.Camera
{
    public class ChaseCamera : MonoBehaviour
    {
        public VehicleController target;
        public float damping;
        public Vector3 cameraOffset;
        public Vector3 lookAtOffset;
        public float orientationBlendTime = 0.5f;

        [Space]
        public float defaultFov = 90f;
        public float maxSpeedFov = 110f;
        
        [Space]
        public float noiseFrequency;
        public float noiseAmplitudeWithSpeed;

        [Space]
        public bool controllingCamera;

        private Vector3 dampedPosition;
        private Quaternion orientation;
        private Quaternion lastOrientation;
        private float orientationBlend;
        private float speed;
        
        private UnityEngine.Camera mainCamera;

        private void Awake()
        {
            mainCamera = UnityEngine.Camera.main;
        }

        private void Update()
        {
            if (target == null) return;
            
            if (target.onGround)
            {
                var fwd = target.transform.forward;
                fwd = new Vector3(fwd.x, 0f, fwd.z).normalized;
                var targetOrientation = Quaternion.LookRotation(fwd);
                orientationBlend = Mathf.MoveTowards(orientationBlend, 1f, Time.deltaTime / orientationBlendTime);
                orientation = Quaternion.Slerp(lastOrientation, targetOrientation, orientationBlend);
            }
            else
            {
                orientationBlend = 0f;
                lastOrientation = orientation;
            }
            
            speed = target.body.linearVelocity.magnitude / target.maxSpeed;
            
            var targetPosition = target.transform.position + orientation * cameraOffset;
            dampedPosition = Vector3.Lerp(dampedPosition, targetPosition, Time.deltaTime / Mathf.Max(Time.deltaTime, damping));

            transform.position = dampedPosition;
            transform.LookAt(target.transform.position + orientation * lookAtOffset);
            
            AddNoise();

            if (controllingCamera)
            {
                mainCamera.transform.position = transform.position;
                mainCamera.transform.rotation = transform.rotation;
                mainCamera.fieldOfView = Mathf.Lerp(defaultFov, maxSpeedFov, speed);
            }
        }

        private void AddNoise()
        {
            if (!target.onGround) return;
            
            var amplitude = noiseAmplitudeWithSpeed * speed;
            
            var sample = new Vector2(Mathf.PerlinNoise1D(Time.time * noiseFrequency), Mathf.PerlinNoise1D(Time.time * noiseFrequency + 4769f));
            sample = new Vector2(Mathf.Cos(sample.x * 2f * Mathf.PI), Mathf.Sin(sample.x * 2f * Mathf.PI)) * sample.y * amplitude;

            transform.position += transform.right * sample.x + transform.up * sample.y;
        }
    }
}