using UnityEngine;

namespace Runtime.Utility
{
    public static class Extensions
    {
        public static void SetPlaying(this ParticleSystem system, bool isPlaying, bool withChildren = true, ParticleSystemStopBehavior stopBehaviour = ParticleSystemStopBehavior.StopEmitting)
        {
            switch (isPlaying)
            {
                case true when !system.isPlaying:
                    system.Play(withChildren);
                    break;
                case false when system.isPlaying:
                    system.Stop(withChildren, stopBehaviour);
                    break;
            }
        }
    }
}