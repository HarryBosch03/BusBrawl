using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Vehicles
{
    public class VehicleInputs : NetworkBehaviour
    {
        public InputAxis throttle = new InputAxis("w", "s");
        public InputAxis steering = new InputAxis("d", "a");
        public InputAxis brake = new InputAxis("leftShift");
        public InputAction boost;

        private bool jumpFlag;

        private List<InputAxis> allAxes = new();
        private List<InputAction> allActions = new();
        private VehicleController controller;

        private void Awake()
        {
            controller = GetComponent<VehicleController>();

            foreach (var field in GetType().GetFields().Where(e => e.FieldType == typeof(InputAxis)))
            {
                allAxes.Add((InputAxis)field.GetValue(this));
            }

            foreach (var field in GetType().GetFields().Where(e => e.FieldType == typeof(InputAction)))
            {
                allActions.Add((InputAction)field.GetValue(this));
            }
        }

        public override void OnStartNetwork()
        {
            foreach (var axis in allAxes)
            {
                axis.action.Enable();
            }

            foreach (var action in allActions)
            {
                action.Enable();
            }

            TimeManager.OnTick += OnTick;
            TimeManager.OnPostTick += OnPostTick;
        }

        public override void OnStopNetwork()
        {
            foreach (var axis in allAxes)
            {
                axis.action.Disable();
            }

            foreach (var action in allActions)
            {
                action.Disable();
            }

            TimeManager.OnTick -= OnTick;
            TimeManager.OnPostTick -= OnPostTick;
        }

        private void OnTick()
        {
            RunInputs(GetInputs());
        }

        private ReplicateData GetInputs()
        {
            if (!IsOwner) return default;

            return new ReplicateData
            {
                throttle = throttle.value,
                steering = steering.value,
                brake = brake.value,
                boost = boost.IsPressed(),
            };
        }

        [Replicate]
        private void RunInputs(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable)
        {
            controller.throttle = data.throttle;
            controller.steering = data.steering;
            controller.brake = data.brake;
            controller.boost = data.boost;

            controller.Simulate();
        }

        private void OnPostTick() { CreateReconcile(); }

        public override void CreateReconcile()
        {
            var data = new ReconcileData
            {
                position = transform.position,
                rotation = transform.rotation,
                linearVelocity = controller.body.linearVelocity,
                angularVelocity = controller.body.angularVelocity,
                boostPercent = controller.boostPercent,
                boostFullyUsed = controller.boostFullyUsed,
                boostWaitTimer = controller.boostWaitTimer,
                wasDrifting = controller.wasDrifting,
            };
            Reconcile(data);
        }

        [Reconcile]
        private void Reconcile(ReconcileData data, Channel channel = Channel.Unreliable)
        {
            transform.position = data.position;
            transform.rotation = data.rotation;
            controller.body.linearVelocity = data.linearVelocity;
            controller.body.angularVelocity = data.angularVelocity;
            controller.boostPercent = data.boostPercent;
            controller.boostFullyUsed = data.boostFullyUsed;
            controller.boostWaitTimer = data.boostWaitTimer;
            controller.wasDrifting = data.wasDrifting;
        }

        private void Update()
        {
            foreach (var axis in allAxes)
            {
                axis.Update(Time.deltaTime);
            }
        }

        public struct ReplicateData : IReplicateData
        {
            public float throttle;
            public float steering;
            public float brake;
            public bool boost;

            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
        }

        public struct ReconcileData : IReconcileData
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 linearVelocity;
            public Vector3 angularVelocity;
            public float boostPercent;
            public bool boostFullyUsed;
            public float boostWaitTimer;
            public bool wasDrifting;

            private uint tick;
            public void Dispose() { }
            public uint GetTick() => tick;
            public void SetTick(uint value) => tick = value;
        }

        [Serializable]
        public class InputAxis
        {
            public InputAction action;
            public float smoothTime;

            public float value;

            public InputAxis()
            {
                action = new InputAction();
                smoothTime = 0.5f;
            }

            public InputAxis(string key) : this() { action = new InputAction(type: InputActionType.Value, binding: $"<Keyboard>/{key}"); }

            public InputAxis(string positive, string negative) : this()
            {
                action = new InputAction(type: InputActionType.Value);
                action.AddCompositeBinding("1DAxis").With("Positive", $"<Keyboard>/{positive}").With("Negative", $"<Keyboard>/{negative}");
            }

            public void Update(float dt) { value = Mathf.MoveTowards(value, action.ReadValue<float>(), Time.deltaTime / smoothTime); }
        }
    }
}