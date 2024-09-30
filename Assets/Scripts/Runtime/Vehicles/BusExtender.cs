using System.Collections.Generic;
using Runtime.Utility;
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
            var pose = new Pose(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f));
            if (poseLog.Count == 0 || (poseLog[0].position - pose.position).magnitude > logThreshold)
            {
                poseLog.Insert(0, pose);
            }
            UpdateSegments();
        }

        private void UpdateSegments()
        {
            if (poseLog.Count == 0) poseLog.Add(new Pose(transform.position, transform.rotation * Quaternion.Euler(0f, 180f, 0f)));
            
            var spline = new Spline(poseLog.ToArray());

            var distance = segmentOffset;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                var sampleFront = spline.Sample(distance);
                var sampleBack = spline.Sample(distance + segment.frontOffset + segment.rearOffset);

                var center = (sampleFront.position + sampleBack.position) * 0.5f;
                var forward = (sampleFront.position - sampleBack.position).normalized;
                var up = (sampleFront.rotation * Vector3.up + sampleBack.rotation * Vector3.up).normalized;
                
                segment.transform.position = center;
                segment.transform.rotation = Quaternion.LookRotation(forward, up);
                
                distance += segment.frontOffset + segment.rearOffset;
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