using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace clone
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

