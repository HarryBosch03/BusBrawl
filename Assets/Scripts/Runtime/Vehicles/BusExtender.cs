using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Vehicles
{
    public class BusExtender : MonoBehaviour
    {
        public Vector3 anchorPoint;
        public BusSegment busMiddleSegment;
        public BusSegment busEndSegment;
        public float segmentOffset;
        public float logThreshold = 0.5f;

        private List<BusSegment> segments = new();
        private List<Pose> poseLog = new();
        private bool enableInterpolation = true;

        private void Awake()
        {
            InsertSegment(0, busEndSegment);
            busMiddleSegment.gameObject.SetActive(false);
            busEndSegment.gameObject.SetActive(false);
        }

        private void LateUpdate()
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                InsertSegment();
            }

            if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {
                enableInterpolation = !enableInterpolation;
            }
        }

        private void FixedUpdate()
        {
            var pose = new Pose(transform.position, transform.rotation);
            if (poseLog.Count == 0 || (poseLog[0].position - pose.position).magnitude > logThreshold)
            {
                poseLog.Insert(0, pose);
            }
            UpdateSegments();
        }

        private void UpdateSegments()
        {
            if (segments.Count == 0)
            {
                poseLog.Clear();
                return;
            }

            var distanceTotal = 0f;
            var lastSegmentDistance = 0f;
            var segmentIndex = 0;
            var last = poseLog.Count > 0 ? poseLog[0] : new Pose(transform.position, transform.rotation);
            var lastSegmentLength = segmentOffset;
            
            for (var i = 1; segmentIndex < segments.Count; i++)
            {
                var pose = i < poseLog.Count ? poseLog[i] : new Pose
                {
                    position = last.position - last.rotation * Vector3.forward * logThreshold,
                    rotation = last.rotation,
                };
                var distance = (pose.position - last.position).magnitude;
                distanceTotal += distance;

                var nextSegment = segments[segmentIndex];

                var totalOffset = lastSegmentLength + nextSegment.frontOffset;
                if (distanceTotal > lastSegmentDistance + totalOffset)
                {
                    nextSegment.transform.position = pose.position;
                    
                    nextSegment.transform.rotation = pose.rotation;

                    lastSegmentDistance += totalOffset;
                    segmentIndex++;
                    if (segmentIndex >= segments.Count)
                    {
                        if (i < poseLog.Count) poseLog.RemoveRange(i, poseLog.Count - i);
                        break;
                    }
                }

                last = pose;
            }
        }

        public void InsertSegment() => InsertSegment(Mathf.Max(0, segments.Count - 2), busMiddleSegment);

        public void InsertSegment(int index, BusSegment prefab)
        {
            var element = Instantiate(prefab);
            element.gameObject.SetActive(true);
            element.transform.SetParent(transform.parent);
            element.name = $"{prefab.name}.{index}";
            element.transform.SetSiblingIndex(index + 1);

            segments.Insert(index, element);
            
            UpdateSegments();
        }

        public void RemoveSegment(int i)
        {
            var element = segments[i];
            segments.RemoveAt(i);
            Destroy(element.gameObject);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;

            if (poseLog.Count > 0)
            {
                for (var i = 0; i < poseLog.Count - 1; i++)
                {
                    var a = poseLog[i];
                    var b = poseLog[i + 1];

                    Gizmos.DrawLine(a.position, b.position);
                    Gizmos.DrawSphere(a.position, 0.02f);
                }

                Gizmos.DrawSphere(poseLog[^1].position, 0.02f);
            }

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(transform.position - transform.forward * segmentOffset, 0.2f);
        }
    }
}