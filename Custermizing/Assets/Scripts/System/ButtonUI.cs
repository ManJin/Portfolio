using System;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Portfolio
{
    public class ButtonUI : Button
    {
        public Action OnDown;
        public Action OnUp;
        
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            OnDown?.Invoke();
        }

        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            OnUp?.Invoke();
        }
        
    }    
}

