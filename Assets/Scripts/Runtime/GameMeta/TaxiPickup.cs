using FishNet.Object;
using FishNet.Object.Synchronizing;

namespace Runtime.GameMeta
{
    public class TaxiPickup : NetworkBehaviour
    {
        public readonly SyncVar<bool> isActive = new SyncVar<bool>();

        [Server]
        public void SetActive(bool isActive)
        {
            this.isActive.Value = isActive;
        }
    }
}