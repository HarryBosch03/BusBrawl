using System;
using System.Collections.Generic;
using Runtime.Utility;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public Vector3 wheelOffset;
    public Vector3 wheelSpacing;
    public float wheelRadius;
    public bool drawForceDebug;

    [Space]
    public float maxSpeedKmph;
    public float accelerationTime;

    [Space]
    public float stationaryTurnAngle = 30f;
    public float maxSpeedTurnAngle = 6f;

    [Space]
    public float normalTangentFriction = 0.9f;
    public float driftTangentFriction = 0.3f;

    [Space]
    public float boostMulti;
    [Range(0f, 1f)]
    public float boostPercent;
    public bool boostFullyUsed;
    public float boostUseDuration;
    public float boostRechargeDuration;
    public float boostRechargeDelay;
    public ParticleSystem boostFx;

    [Space]
    public float airControl;

    [Space]
    public float suspensionSpring;
    public float suspensionDamping;

    [Range(0f, 1f)]
    public float throttle;
    [Range(-1f, 1f)]
    public float steering;
    [Range(0f, 1f)]
    public float brake;
    public float steerAngle;
    public bool boost;
    [Range(0, 4)]
    public int wheelsOnGround;
    public bool onGround;
    public Vector3 lastGroundNormal;

    public float boostWaitTimer;
    public bool wasDrifting;
    private List<Action> forcesToApply = new();

    public Rigidbody body { get; private set; }
    public float maxSpeed => maxSpeedKmph / 3.6f;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();

        boostPercent = 1f;
    }

    public void Simulate()
    {
        forcesToApply.Clear();

        CheckWheelsOnGround();
        body.sleepThreshold = -1f;

        ApplySuspension();
        ApplyTangentFriction();
        ApplyForwardForce();
        DoAirControl();

        foreach (var force in forcesToApply)
        {
            force?.Invoke();
        }
    }

    private void DoAirControl()
    {
        if (onGround) return;

        var airInput = new Vector2(throttle, steering);
        airInput = Vector2.ClampMagnitude(airInput, 1f);
        body.angularVelocity += (transform.right * airInput.x - transform.forward * airInput.y) * airControl * Time.fixedDeltaTime;
    }

    private void OnCollisionStay(Collision other)
    {
        if (!onGround)
        {
            lastGroundNormal = Vector3.zero;
            for (var i = 0; i < other.contactCount; i++)
            {
                lastGroundNormal += other.GetContact(i).normal;
            }

            lastGroundNormal.Normalize();
        }
    }

    private void CheckWheelsOnGround()
    {
        wheelsOnGround = 0;
        if (CheckWheelOnGround(wheelSpacing.x, wheelSpacing.z)) wheelsOnGround++;
        if (CheckWheelOnGround(-wheelSpacing.x, wheelSpacing.z)) wheelsOnGround++;
        if (CheckWheelOnGround(-wheelSpacing.x, -wheelSpacing.z)) wheelsOnGround++;
        if (CheckWheelOnGround(wheelSpacing.x, -wheelSpacing.z)) wheelsOnGround++;
        onGround = wheelsOnGround > 1;
    }

    private bool CheckWheelOnGround(float wheelX, float wheelZ)
    {
        var position = transform.TransformPoint(wheelOffset + new Vector3(wheelX, 0f, wheelZ)) + transform.up * wheelRadius;
        var ray = new Ray(position, -transform.up);
        return Physics.Raycast(ray, out var hit, wheelRadius * 2f * 1.05f);
    }

    private void ApplyForwardForce()
    {
        if (boost && boostPercent > 0f && !boostFullyUsed)
        {
            body.linearVelocity += transform.up * maxSpeed * 2f / accelerationTime * boostMulti * Time.fixedDeltaTime;
            
            boostPercent -= Time.fixedDeltaTime / boostUseDuration;
            boostWaitTimer = boostRechargeDelay;

            boostFx.SetPlaying(true);

            if (boostPercent <= 0f) boostFullyUsed = true;
        }
        else
        {
            boostFx.SetPlaying(false);
            if (boostWaitTimer < 0f)
            {
                if (boostPercent < 1f)
                {
                    boostPercent += Time.fixedDeltaTime / boostRechargeDuration;
                    if (boostPercent >= 1f)
                    {
                        boostPercent = 1f;
                        boostFullyUsed = false;
                    }
                }
            }
            else
            {
                boostWaitTimer -= Time.fixedDeltaTime;
            }

            var isDrifting = throttle * brake > 0.1f;
            if (!isDrifting && wasDrifting && onGround)
            {
                var speed = body.linearVelocity.magnitude;
                body.linearVelocity = transform.forward * speed;
            }

            wasDrifting = isDrifting;

        }
        
        if (onGround)
        {
            var throttleForce = ComputeThrottleForce();
            var brakeForce = ComputeBrakeForce();
            var force = Mathf.Lerp(brakeForce, throttleForce, Mathf.Abs(throttle));
            var steerAngleRad = steerAngle * Mathf.Deg2Rad;
            var fwd = transform.forward * Mathf.Cos(steerAngleRad) + transform.right * Mathf.Sin(steerAngleRad);
            body.linearVelocity += fwd * force * Time.fixedDeltaTime;
        }

    }

    private float ComputeThrottleForce()
    {
        var targetSpeed = throttle * maxSpeed;
        var fwdSpeed = Vector3.Dot(transform.forward, body.linearVelocity);
        var force = (targetSpeed - fwdSpeed) * 2f / accelerationTime;
        return force;
    }

    private float ComputeBrakeForce()
    {
        var fwdSpeed = Vector3.Dot(transform.forward, body.linearVelocity);

        var a = -fwdSpeed / Time.fixedDeltaTime;
        var b = maxSpeed * 2f / accelerationTime * -Mathf.Sign(fwdSpeed);

        return (Mathf.Abs(a) < Mathf.Abs(b) ? a : b) * brake;
    }

    private void ApplyTangentFriction()
    {
        if (!onGround) return;

        var speed = Mathf.Abs(Vector3.Dot(transform.forward, body.linearVelocity));
        steerAngle = Mathf.Lerp(stationaryTurnAngle, maxSpeedTurnAngle, speed / maxSpeed);
        steerAngle = Mathf.Lerp(steerAngle, stationaryTurnAngle, throttle * brake);
        steerAngle *= steering;

        ApplyTangentFriction(wheelSpacing.x, -wheelSpacing.z, 0f);
        ApplyTangentFriction(-wheelSpacing.x, -wheelSpacing.z, 0f);
        ApplyTangentFriction(wheelSpacing.x, wheelSpacing.z, steerAngle);
        ApplyTangentFriction(-wheelSpacing.x, wheelSpacing.z, steerAngle);
    }

    private void ApplyTangentFriction(float xOffset, float zOffset, float angleDeg)
    {
        var angleRad = angleDeg * Mathf.Deg2Rad;
        var position = body.worldCenterOfMass + transform.TransformVector(wheelOffset + new Vector3(xOffset, 0f, zOffset));
        position += Vector3.Project(body.worldCenterOfMass - position, transform.up);

        var velocity = body.GetPointVelocity(position);
        var tangent = transform.right * Mathf.Cos(angleRad) - transform.forward * Mathf.Sin(angleRad);

        var magnitude = Mathf.Lerp(normalTangentFriction, driftTangentFriction, throttle * brake);

        var dv = -Vector3.Project(velocity, tangent) * magnitude;
        ChangeVelocityAtPosition(dv / 4f, position, Color.red);
    }

    private void ApplySuspension()
    {
        if (!onGround) return;

        ApplySuspension(transform.TransformPoint(wheelOffset + new Vector3(wheelSpacing.x, 0f, wheelSpacing.z)));
        ApplySuspension(transform.TransformPoint(wheelOffset + new Vector3(wheelSpacing.x, 0f, -wheelSpacing.z)));
        ApplySuspension(transform.TransformPoint(wheelOffset + new Vector3(-wheelSpacing.x, 0f, -wheelSpacing.z)));
        ApplySuspension(transform.TransformPoint(wheelOffset + new Vector3(-wheelSpacing.x, 0f, wheelSpacing.z)));
    }

    private void ApplySuspension(Vector3 position)
    {
        var ray = new Ray(position + transform.up * wheelRadius, -transform.up);
        if (Physics.Raycast(ray, out var hit, wheelRadius * 2f))
        {
            var compression = Mathf.Abs(Vector3.Dot(transform.up, hit.point - (position - transform.up * wheelRadius)));
            var velocity = Vector3.Dot(transform.up, body.GetPointVelocity(position));
            var dv = Mathf.Max(compression * suspensionSpring - velocity * suspensionDamping);
            ChangeVelocityAtPosition(transform.up * dv * Time.fixedDeltaTime, position, Color.green);
        }
    }

    private void ChangeVelocityAtPosition(Vector3 force, Vector3 position, Color debugColor)
    {
        forcesToApply.Add(() =>
        {
            var vector = position - body.worldCenterOfMass;

            body.linearVelocity += force;

            var torque = Vector3.Cross(vector, force * body.mass);
            torque = body.rotation * div(Quaternion.Inverse(body.rotation) * torque, body.inertiaTensor);

            body.angularVelocity += torque;


            Vector3 div(Vector3 n, Vector3 d) => new Vector3(n.x / d.x, n.y / d.y, n.z / d.z);
        });
        
        if (drawForceDebug)
        {
            Debug.DrawRay(position, force * 10f, debugColor, Time.fixedDeltaTime);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(wheelOffset + new Vector3(wheelSpacing.x, 0f, wheelSpacing.z), wheelRadius);
        Gizmos.DrawWireSphere(wheelOffset + new Vector3(wheelSpacing.x, 0f, -wheelSpacing.z), wheelRadius);
        Gizmos.DrawWireSphere(wheelOffset + new Vector3(-wheelSpacing.x, 0f, -wheelSpacing.z), wheelRadius);
        Gizmos.DrawWireSphere(wheelOffset + new Vector3(-wheelSpacing.x, 0f, wheelSpacing.z), wheelRadius);

        var body = GetComponent<Rigidbody>();
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(body.centerOfMass, 0.5f);
    }
}