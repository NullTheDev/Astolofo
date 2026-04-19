using UnityEngine;

namespace AstolfoGorillaTagMenu.Core
{
    internal sealed class CoreTickBehaviour : MonoBehaviour
    {
        private void LateUpdate()
        {
            AstolfoCore.RaiseNetworkFrame();
            LimitMonitor.Tick();
        }
    }
}
