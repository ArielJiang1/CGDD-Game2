// @role Something worth photographing with the phone's Camera app (Chapter 4 Picture Day)
using UnityEngine;

namespace PhoneGame
{
    /// <summary>Something worth photographing with the phone's Camera app (Chapter 4).</summary>
    public class PhotoTarget : MonoBehaviour
    {
        public string id;
        [Tooltip("Shown in the viewfinder and as the photo's caption (English; Chinese from LocTable).")]
        public string label;
        public float range = 9f;
        [HideInInspector] public bool taken;
    }
}
