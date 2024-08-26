using UnityEngine;

public class VehicleModel : MonoBehaviour
{
    public Transform root;
    public Transform wheelFrontLeft;
    public Transform wheelFrontRight;
    public Transform wheelRearLeft;
    public Transform wheelRearRight;

    private void OnValidate()
    {
        if (root)
        {
            wheelFrontLeft = deepFind(root, "Wheel.FL");
            wheelFrontRight = deepFind(root, "Wheel.FR");
            wheelRearLeft = deepFind(root, "Wheel.RL");
            wheelRearRight = deepFind(root, "Wheel.RR");
        }

        Transform deepFind(Transform parent, string name)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>())
            {
                if (child.name == name) return child;
            }

            return null;
        }
    }
}
