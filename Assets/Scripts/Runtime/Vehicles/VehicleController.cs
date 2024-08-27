using System;
using System.Collections.Generic;
using Runtime.Utility;
using UnityEditor.Rendering;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class VehicleController : MonoBehaviour
{
    public Vector3 wheelPosition;
    public float wheelRadius;

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
    public float driftMiniBoostThreshold;
    public float driftMiniBoostDuration;
    public float driftMaxiBoostThreshold;
    public float driftMaxiBoostDuration;

    [Space]
    public float suspensionSpring;
    public float suspensionDamping;

    [Space]
    public float startingAntiRoll;
    public float antiRollIncreaseSpeed;
    public float antiRollTorque;

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
        antiRollTorque = startingAntiRoll;
    }

    public void Simulate()
    {
        forcesToApply.Clear();

        CheckWheelsOnGround();
        body.sleepThreshold = -1f;

        ApplySuspension();
        ApplyTangentFriction();
        ApplyForwardForce();
        AntiRoll();

        foreach (var force in forcesToApply)
        {
            force?.Invoke();
        }
    }

    private void AntiRoll()
    {
        if (onGround)
        {
            antiRollTorque = startingAntiRoll;
            lastGroundNormal = transform.up;
            return;
        }

        var angle = -Vector3.SignedAngle(lastGroundNormal, transform.up, transform.forward) / 180f;
        body.AddTorque(transform.forward * angle * antiRollTorque, ForceMode.Acceleration);
        antiRollTorque += antiRollIncreaseSpeed * Time.fixedDeltaTime;
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
        if (CheckWheelOnGround(wheelPosition.x, wheelPosition.z)) wheelsOnGround++;
        if (CheckWheelOnGround(-wheelPosition.x, wheelPosition.z)) wheelsOnGround++;
        if (CheckWheelOnGround(-wheelPosition.x, -wheelPosition.z)) wheelsOnGround++;
        if (CheckWheelOnGround(wheelPosition.x, -wheelPosition.z)) wheelsOnGround++;
        onGround = wheelsOnGround > 1;
    }

    private bool CheckWheelOnGround(float wheelX, float wheelZ)
    {
        var position = transform.TransformPoint(wheelX, wheelPosition.y, wheelZ) + transform.up * wheelRadius;
        var ray = new Ray(position, -transform.up);
        return Physics.Raycast(ray, out var hit, wheelRadius * 2f * 1.05f);
    }

    private void ApplyForwardForce()
    {
        if (boost && boostPercent > 0f && !boostFullyUsed)
        {
            var fwdSpeed = Vector3.Dot(transform.forward, body.linearVelocity);
            var force = (maxSpeed * boostMulti - fwdSpeed) * 2f / accelerationTime;

            body.linearVelocity += transform.forward * force * Time.fixedDeltaTime;
            boostPercent -= Time.fixedDeltaTime / boostUseDuration;
            boostWaitTimer = boostRechargeDelay;

            boostFx.SetPlaying(true);

            if (boostPercent <= 0f) boostFullyUsed = true;

            return;
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
        }

        var isDrifting = throttle * brake > 0.1f;
        if (!isDrifting && wasDrifting && onGround)
        {
            var speed = body.linearVelocity.magnitude;
            body.linearVelocity = transform.forward * speed;
        }

        wasDrifting = isDrifting;

        if (onGround)
        {
            var throttleForce = ComputeThrottleForce();
            var brakeForce = ComputeBrakeForce();
            var finalForce = Mathf.Lerp(brakeForce, throttleForce, Mathf.Abs(throttle));
            body.linearVelocity += transform.forward * finalForce * Time.fixedDeltaTime;
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

        ApplyTangentFriction(wheelPosition.x, -wheelPosition.z, 0f);
        ApplyTangentFriction(-wheelPosition.x, -wheelPosition.z, 0f);
        ApplyTangentFriction(wheelPosition.x, wheelPosition.z, steerAngle);
        ApplyTangentFriction(-wheelPosition.x, wheelPosition.z, steerAngle);
    }

    private void ApplyTangentFriction(float xOffset, float zOffset, float angleDeg)
    {
        var angleRad = angleDeg * Mathf.Deg2Rad;
        var position = body.worldCenterOfMass + transform.forward * zOffset + transform.right * xOffset;

        var velocity = body.GetPointVelocity(position);
        var tangent = transform.right * Mathf.Cos(angleRad) - transform.forward * Mathf.Sin(angleRad);

        var magnitude = Mathf.Lerp(normalTangentFriction, driftTangentFriction, throttle * brake);

        var dv = -Vector3.Project(velocity, tangent) * magnitude;
        ChangeVelocityAtPosition(dv / 4f, position);
    }

    private void ApplySuspension()
    {
        if (!onGround) return;

        ApplySuspension(transform.TransformPoint(wheelPosition.x, wheelPosition.y, wheelPosition.z));
        ApplySuspension(transform.TransformPoint(wheelPosition.x, wheelPosition.y, -wheelPosition.z));
        ApplySuspension(transform.TransformPoint(-wheelPosition.x, wheelPosition.y, -wheelPosition.z));
        ApplySuspension(transform.TransformPoint(-wheelPosition.x, wheelPosition.y, wheelPosition.z));
    }

    private void ApplySuspension(Vector3 position)
    {
        var ray = new Ray(position + transform.up * wheelRadius, -transform.up);
        if (Physics.Raycast(ray, out var hit, wheelRadius * 2f))
        {
            var compression = Mathf.Abs(Vector3.Dot(transform.up, hit.point - (position - transform.up * wheelRadius)));
            var velocity = Vector3.Dot(transform.up, body.GetPointVelocity(position));
            var dv = Mathf.Max(compression * suspensionSpring - velocity * suspensionDamping);
            ChangeVelocityAtPosition(transform.up * dv * Time.fixedDeltaTime, position);
        }
    }

    private void ChangeVelocityAtPosition(Vector3 force, Vector3 position)
    {
        forcesToApply.Add(() =>
        {
            var vector = position - body.worldCenterOfMass;
            
            body.linearVelocity += force;
            body.angularVelocity += Vector3.Cross(vector.normalized, force);
        });
    }

    public void AddVelocityAtPositionNow(Vector3 deltaVelocity, Vector3 position)
    {
        var leverage = (position - body.worldCenterOfMass);
        var normal = leverage.normalized;

        body.linearVelocity += Vector3.Project(deltaVelocity, normal);
        body.angularVelocity += Vector3.Cross(normal, deltaVelocity);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(new Vector3(wheelPosition.x, wheelPosition.y, wheelPosition.z), wheelRadius);
        Gizmos.DrawWireSphere(new Vector3(wheelPosition.x, wheelPosition.y, -wheelPosition.z), wheelRadius);
        Gizmos.DrawWireSphere(new Vector3(-wheelPosition.x, wheelPosition.y, -wheelPosition.z), wheelRadius);
        Gizmos.DrawWireSphere(new Vector3(-wheelPosition.x, wheelPosition.y, wheelPosition.z), wheelRadius);
    }
}