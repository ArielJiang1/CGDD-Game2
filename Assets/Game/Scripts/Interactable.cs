// @role Anything the player can use with E (desk cubby, panel, phone, shoulder tap, seats, doors)
using UnityEngine;

namespace PhoneGame
{
    /// <summary>
    /// Something the player can use by looking at it (crosshair) and pressing E.
    /// The level director sets prompt / active / onUse.
    /// </summary>
    public class Interactable : MonoBehaviour
    {
        public string prompt = "Use";
        public bool active = true;
        public System.Action onUse;
    }
}
