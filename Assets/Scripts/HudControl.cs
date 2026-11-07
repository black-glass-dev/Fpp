using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Fpp
{
    public sealed class HudControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public MobileHud owner;
        public ControlPlacement placement;
        public Vector2 defaultPosition;
        public float baseSize;
        public RectTransform knob;
        private int finger=int.MinValue;
        private RectTransform rect;
        private Vector2 dragOffset;private Vector2 floatingOrigin;
        public void Apply()
        {
            var group=GetComponent<CanvasGroup>();if(group==null)group=gameObject.AddComponent<CanvasGroup>();group.alpha=placement.opacity;
            rect=(RectTransform)transform;rect.anchorMin=rect.anchorMax=new Vector2(placement.x,placement.y);
            rect.anchoredPosition=Vector2.zero;rect.sizeDelta=Vector2.one*(baseSize*placement.scale);
            var label=GetComponentInChildren<Text>();
            if(label!=null)label.fontSize=Mathf.RoundToInt((placement.id=="fire"?24:17)*placement.scale);
            var parent=(RectTransform)rect.parent;
            if(parent.rect.width>0 && parent.rect.height>0)
            {
                float mx=Mathf.Clamp(rect.rect.width*.5f/parent.rect.width,.03f,.45f);
                float my=Mathf.Clamp(rect.rect.height*.5f/parent.rect.height,.03f,.45f);
                placement.x=Mathf.Clamp(placement.x,mx,1-mx);placement.y=Mathf.Clamp(placement.y,my,1-my);
                rect.anchorMin=rect.anchorMax=new Vector2(placement.x,placement.y);
            }
        }
        public void OnPointerDown(PointerEventData data)
        {
            if(finger!=int.MinValue)return;finger=data.pointerId;
            if(owner.Editing)
            {
                owner.Select(this);
                RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,data.position,data.pressEventCamera,out var point);
                dragOffset=rect.localPosition-(Vector3)point;
            }
            else { owner.Press(placement.id,true);if(placement.id=="move"){RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)rect.parent,data.position,data.pressEventCamera,out floatingOrigin);rect.localPosition=floatingOrigin;Move(data);} }
        }
        public void OnDrag(PointerEventData data)
        {
            if(data.pointerId!=finger)return;
            if(owner.Editing)
            {
                var parent=(RectTransform)rect.parent;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,data.position,data.pressEventCamera,out var p);p+=dragOffset;
                float marginX=rect.rect.width*.5f/parent.rect.width;
                float marginY=rect.rect.height*.5f/parent.rect.height;
                placement.x=Mathf.Clamp((p.x-parent.rect.xMin)/parent.rect.width,marginX,1-marginX);
                placement.y=Mathf.Clamp((p.y-parent.rect.yMin)/parent.rect.height,marginY,1-marginY);Apply();
            }
            else if(placement.id=="move")Move(data);
        }
        private void Move(PointerEventData data)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect,data.position,data.pressEventCamera,out var p);
            var value=Vector2.ClampMagnitude(p/(rect.rect.width*.35f),1);owner.SetMove(value);
            if(knob!=null)knob.anchoredPosition=value*rect.rect.width*.28f;
        }
        public void OnPointerUp(PointerEventData data) { if(data.pointerId==finger)ResetInput(); }
        public void ResetInput() { finger=int.MinValue;owner.Release(placement.id);if(knob!=null){knob.anchoredPosition=Vector2.zero;if(rect!=null)rect.anchoredPosition=Vector2.zero;} }
        private void OnDisable() { if(owner!=null)ResetInput(); }
    }
}
