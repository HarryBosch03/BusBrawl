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
        public float segmentLength;

        private List<BusSegment> segments = new();

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                if (busMiddleSegment)
                {
                    anchorPoint = transform.InverseTransformPoint(busMiddleSegment.transform.position);
                    if (busEndSegment) segmentLength = Vector3.Dot(-busMiddleSegment.transform.forward, busEndSegment.transform.position - busMiddleSegment.transform.position);
                }
            }
        }

        private void Awake()
        {
            InsertSegment(0, busEndSegment);
            busMiddleSegment.gameObject.SetActive(false);
            busEndSegment.gameObject.SetActive(false);
        }

        private void Update()
        {
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                InsertSegment();
            }
        }

        private void FixedUpdate()
        {
            var anchor = transform.TransformPoint(anchorPoint);
            var up = transform.up;
            for (var i = 0; i < segments.Count; i++)
            {
                var segment = segments[i];
                segment.Move(anchor, up);

                up = segment.transform.up;
                
                if (segment.anchorEnd == null) break;
                anchor = segment.anchorEnd.position;
            }
        }

        public void InsertSegment() => InsertSegment(Mathf.Max(0, segments.Count - 2), busMiddleSegment);

        public void InsertSegment(int i, BusSegment prefab)
        {
            var element = Instantiate(prefab);
            segments.Insert(i, element);
            element.gameObject.SetActive(true);
            element.transform.SetParent(transform.parent);
            element.name = $"{prefab.name}.{i}";
            element.transform.SetSiblingIndex(i + 1);

            var delta = Vector3.zero;
            if (i != 0)
            {
                var elementBefore = segments[i - 1];
                element.transform.rotation = elementBefore.transform.rotation;
                delta = elementBefore.anchorEnd.position - element.anchorFront.position;
            }
            else
            {
                element.transform.rotation = transform.rotation;
                delta = transform.TransformPoint(anchorPoint) - element.anchorFront.position;
            }

            for (var j = i; j < segments.Count; j++)
            {
                segments[j].transform.position += delta;
            }
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
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(anchorPoint, 0.1f);
            Gizmos.DrawWireSphere(anchorPoint - Vector3.forward * segmentLength, 0.1f);
        }
    }
}