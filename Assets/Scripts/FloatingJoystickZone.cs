using UnityEngine;
using UnityEngine.EventSystems;

namespace Fpp
{
    public sealed class FloatingJoystickZone : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public HudControl control;
        public void OnPointerDown(PointerEventData e){if(!control.owner.Editing && control.owner.FloatingJoystick)control.OnPointerDown(e);}
        public void OnDrag(PointerEventData e){if(!control.owner.Editing)control.OnDrag(e);}
        public void OnPointerUp(PointerEventData e){control.OnPointerUp(e);}
    }
}
