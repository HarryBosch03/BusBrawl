using UnityEngine;

[RequireComponent(typeof(VehicleModel))]
public class VehicleAnimator : MonoBehaviour
{
    public float gain;
    public float spring;
    public float damping;
    public float drawSize = 10f;
    public Vector3 xInfluence;
    public Vector3 yInfluence;

    public Vector3 position;
    public Vector3 velocity;

    private VehicleController controller;
    private VehicleModel model;
    private Vector3 lastTargetVelocity;
    
    public Vector2 chassisOffset { get; private set; }

    private void Awake()
    {
        controller = GetComponent<VehicleController>();
        model = GetComponent<VehicleModel>();
    }

    private void FixedUpdate()
    {
        var targetAcceleration = (controller.body.linearVelocity - lastTargetVelocity) / Time.fixedDeltaTime;
        lastTargetVelocity = controller.body.linearVelocity;
        var force = -position * spring + -velocity * damping;
        force += targetAcceleration * gain * Time.fixedDeltaTime;
        
        position += velocity * Time.fixedDeltaTime;
        velocity += force * Time.fixedDeltaTime;

        if (position.magnitude > 1f)
        {
            var normal = position.normalized;
            position = normal;
            velocity -= normal * Mathf.Max(Vector3.Dot(normal, velocity), 0f);
        }

        chassisOffset = new Vector2
        {
            x = Vector3.Dot(transform.right, position),
            y = Vector3.Dot(transform.forward, position),
        };

        model.wheelFrontLeft.localRotation = Quaternion.Euler(0f, controller.steerAngle, 0f);
        model.wheelFrontRight.localRotation = Quaternion.Euler(0f, controller.steerAngle, 0f);

        if (model != null) model.root.localRotation = Quaternion.Euler(xInfluence * chassisOffset.x + yInfluence * chassisOffset.y);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        
        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix * Matrix4x4.Scale(Vector3.one * drawSize);
        var end = new Vector3(chassisOffset.x, 0f, chassisOffset.y);
        Gizmos.DrawLine(Vector3.zero, end);
        Gizmos.DrawSphere(end,  0.1f);
        Gizmos.DrawWireSphere(Vector3.zero, 0.2f);
    }
}
