using UnityEngine;

namespace Eurydice.Gameplay.Progression
{
    public sealed class PortalArrival : MonoBehaviour
    {
        [SerializeField] private string arrivalId;
        public string ArrivalId => arrivalId;
    }
}
