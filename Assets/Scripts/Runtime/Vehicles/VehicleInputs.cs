using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Runtime.Vehicles
{
    public class VehicleInputs : MonoBehaviour
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

        private void OnEnable()
        {
            foreach (var axis in allAxes)
            {
                axis.action.Enable();
            }
            
            foreach (var action in allActions)
            {
                action.Enable();
            }
        }

        private void OnDisable()
        {
            foreach (var axis in allAxes)
            {
                axis.action.Disable();
            }
            
            foreach (var action in allActions)
            {
                action.Disable();
            }
        }

        private void Update()
        {
            foreach (var axis in allAxes)
            {
                axis.Update(Time.deltaTime);
            }
            
            controller.throttle = throttle.value;
            controller.steering = steering.value;
            controller.brake = brake.value;
            controller.boost = boost.IsPressed();

            if (Keyboard.current.rightBracketKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(SceneManager.GetSceneAt(0).name);
            }
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

            public InputAxis(string key) : this()
            {
                action = new InputAction(type: InputActionType.Value, binding: $"<Keyboard>/{key}");
            }
            
            public InputAxis(string positive, string negative) : this()
            {
                action = new InputAction(type: InputActionType.Value);
                action.AddCompositeBinding("1DAxis").With("Positive", $"<Keyboard>/{positive}").With("Negative", $"<Keyboard>/{negative}");
            }

            public void Update(float dt)
            {
                value = Mathf.MoveTowards(value, action.ReadValue<float>(), Time.deltaTime / smoothTime);
            }
        }
    }
}