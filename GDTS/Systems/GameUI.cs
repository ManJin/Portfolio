using System;
using UnityEngine;
using UnityEngine.UI;

namespace clone
{
    public class GameUI : MonoBehaviour, IEventListner
    {
        public ButtonUI LeftButton;
        public ButtonUI RightButton;

        public Slider PlayerHp;
        public Slider EnemyHp;
        
        private void Start()
        {
            LeftButton.OnDown += OnDown_Left;
            LeftButton.OnUp += OnUp_Left;
            RightButton.OnDown += OnDown_Right;
            RightButton.OnUp += OnUp_Right;
            
            MessageSystem.Instance.Subscribe<HpChangedEvent>(OnEvent);
        }

        private void OnDestroy()
        {
            MessageSystem.Instance.Unsubscribe<HpChangedEvent>(OnEvent);
        }
        public bool OnEvent(Event e)
        {
            if (e is HpChangedEvent he)
            {
                if (he.IsPlayer)
                {
                    PlayerHp.value = he.ChangeValue;
                }
                else
                {
                    EnemyHp.value = he.ChangeValue;
                }
            }
            return false;
        }
        
        public void OnDown_Left()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.MoveLeftTouchDown, Vector2.zero));
        }

        public void OnDown_Right()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.MoveRightTouchDown, Vector2.zero));
        }

        public void OnUp_Left()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.MoveLeftTouchUp, Vector2.zero));
        }

        public void OnUp_Right()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.MoveRightTouchUp, Vector2.zero));
        }
        
        public void OnClick_Left()
        {
            // 일단 사용안함. 
        }
        
        public void OnClick_Right()
        {
            // 일단 사용안함. 
        }
        
        public void OnClick_Action1()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.Action1TouchDown, Vector2.zero));
        }
        
        public void OnClick_Action2()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.Action2TouchDown, Vector2.zero));
        }
        
        public void OnClick_Action3()
        {
            MessageSystem.Instance.Publish(TouchEvent.Create(TouchEventType.Action3TouchDown, Vector2.zero));
        }
        
    }    
}

