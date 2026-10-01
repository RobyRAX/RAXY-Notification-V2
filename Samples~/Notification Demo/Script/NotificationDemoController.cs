using UnityEngine;

namespace RAXY.Notification
{
    public sealed class NotificationDemoController : MonoBehaviour
    {
        [SerializeField]
        Sprite getItemIcon;

        [SerializeField]
        Sprite queuedLightIcon;

        [SerializeField]
        Sprite queuedGroundIcon;

        [SerializeField]
        Sprite queuedBloomIcon;

        [SerializeField]
        Sprite unlockItemIcon;

        public void ShowItem()
        {
            NotificationManager.Show(new GetItemRequest("Camera", getItemIcon));
        }

        public void QueueItems()
        {
            NotificationManager.Show(new GetItemRequest("Lights", queuedLightIcon));
            NotificationManager.Show(new GetItemRequest("Ground", queuedGroundIcon));
            NotificationManager.Show(new GetItemRequest("Bloom", queuedBloomIcon));
        }

        public void ShowWeapon()
        {
            NotificationManager.Show(new UnlockItemRequest("Test Unlock Item", unlockItemIcon));
        }
    }
}
