using System;
using UnityEngine;

namespace Runtime.Utility
{
    public class Spline
    {
        private readonly Pose[] points;
        private readonly float[] lengths;

        public Spline(Pose[] points)
        {
            this.points = new Pose[points.Length];
            Array.Copy(points, this.points, points.Length);

            lengths = new float[Mathf.Max(0, points.Length - 1)];
            for (var i = 0; i < lengths.Length; i++)
            {
                var a = points[i];
                var b = points[i + 1];
                lengths[i] = (b.position - a.position).magnitude;
            }
        }

        public Pose Sample(float distance)
        {
            if (points.Length == 0) throw new IndexOutOfRangeException();

            if (points.Length == 1)
            {
                var pose = points[0];
                return new Pose
                {
                    position = pose.position + pose.rotation * Vector3.forward * distance,
                    rotation = pose.rotation,
                };
            }

            if (distance < 0f)
            {
                var start = points[0];
                return new Pose
                {
                    position = start.position + start.rotation * Vector3.forward * distance,
                    rotation = start.rotation,
                };
            }

            for (var i = 0; i < lengths.Length; i++)
            {
                var length = lengths[i];
                if (length > distance)
                {
                    var a = points[i];
                    var b = points[i + 1];
                    var t = distance / length;
                    return new Pose()
                    {
                        position = Vector3.Lerp(a.position, b.position, t),
                        rotation = Quaternion.Slerp(a.rotation, b.rotation, t),
                    };
                }

                distance -= length;
            }

            var end = points[^1];
            return new Pose
            {
                position = end.position + end.rotation * Vector3.forward * distance,
                rotation = end.rotation,
            };
        }
    }
}