using FishNet.Managing;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Runtime.Network
{
    [RequireComponent(typeof(NetworkManager))]
    public class DebugNetworkOverlay : MonoBehaviour
    {
        public string address = "127.0.0.1";
        
        private NetworkManager netManager;

        private void Awake() { netManager = GetComponent<NetworkManager>(); }

        private void Update()
        {
            if (netManager != null && !netManager.IsServer && !netManager.IsClient)
            {
                var kb = Keyboard.current;
                if (kb.spaceKey.wasPressedThisFrame || kb.hKey.wasPressedThisFrame) StartHost();
                if (kb.cKey.wasPressedThisFrame) StartClient();
            }
        }

        private void OnGUI()
        {
            if (netManager != null && !netManager.IsServer && !netManager.IsClient)
            {
                using (new GUILayout.AreaScope(new Rect(10f, 10f, 150f, Screen.height - 20)))
                {
                    if (GUILayout.Button("Start Host")) StartHost();
                    if (GUILayout.Button("Start Client")) StartClient();
                    address = GUILayout.TextField(address);
                }
            }
        }

        private void StartHost()
        {
            netManager.ServerManager.StartConnection();
            netManager.ClientManager.StartConnection("127.0.0.1");
        }

        private void StartClient()
        {
            netManager.ClientManager.StartConnection(address);
        }
    }
}