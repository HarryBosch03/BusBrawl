using System.Text.RegularExpressions;
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
            wheelFrontLeft =  deepFind(root, new Regex(@"([a-z0-9.]+\.)?Wheel(\.[a-z0-9.]+)?\.FL", RegexOptions.IgnoreCase));
            wheelFrontRight = deepFind(root, new Regex(@"([a-z0-9.]+\.)?Wheel(\.[a-z0-9.]+)?\.FR", RegexOptions.IgnoreCase));
            wheelRearLeft =   deepFind(root, new Regex(@"([a-z0-9.]+\.)?Wheel(\.[a-z0-9.]+)?\.RL", RegexOptions.IgnoreCase));
            wheelRearRight =  deepFind(root, new Regex(@"([a-z0-9.]+\.)?Wheel(\.[a-z0-9.]+)?\.RR", RegexOptions.IgnoreCase));
        }

        Transform deepFind(Transform parent, Regex regex)
        {
            foreach (var child in parent.GetComponentsInChildren<Transform>())
            {
                if (regex.IsMatch(child.name)) return child;
            }

            return null;
        }
    }
}
