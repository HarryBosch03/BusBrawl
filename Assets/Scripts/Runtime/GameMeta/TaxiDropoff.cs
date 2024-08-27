using System.Collections.Generic;
using FishNet.Object;

namespace Runtime.GameMeta
{
    public class TaxiDropoff : NetworkBehaviour
    {
        public static List<TaxiDropoff> all = new();
        
        private void OnEnable()
        {
            all.Add(this);
        }

        private void OnDisable()
        {
            all.Remove(this);
        }
    }
}