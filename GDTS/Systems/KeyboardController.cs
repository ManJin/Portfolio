using System;
using System.Collections.Generic;
using UnityEditor.DeviceSimulation;
using UnityEngine;

namespace clone
{
    public class KeyboardController : IUpdatable, IDisposable
    {
        public struct KeyCodeBindingInfo
        {
            public KeyCode KeyCode;
            public UIType UIType;
            public TouchEventType DownTouchEventType;
            public TouchEventType UpTouchEventType;

            public override int GetHashCode()
            {
                return new Tuple<KeyCode, UIType>(KeyCode, UIType).GetHashCode();
            }

            public override bool Equals(object obj)
            {
                if (obj is KeyCodeBindingInfo info)
                {
                    return GetHashCode() == info.GetHashCode();
                }

                return false;
            }
        }

        private bool leftPressed = false;
        private bool rightPressed = false;
        private bool upPressed = false;
        private bool downPressed = false;

        private Direction lastDirection = Direction.None;

        private Dictionary<KeyCodeBindingInfo, bool> keyCodeBindingDict = new Dictionary<KeyCodeBindingInfo, bool>();

        #region interfaces

        void IUpdatable.UpdateFrame(float dt)
        {
            ProcessInputs();
        }

        void IDisposable.Dispose()
        {
        }

        #endregion

        public KeyboardController()
        {
            KeyCodeBinding();
        }

        private void KeyCodeBinding()
        {
            keyCodeBindingDict.Add(new KeyCodeBindingInfo
            {
                KeyCode = KeyCode.A,
                UIType = UIType.MoveLeft,
                DownTouchEventType = TouchEventType.MoveLeftTouchDown,
                UpTouchEventType = TouchEventType.MoveLeftTouchUp,
            }, false);

            keyCodeBindingDict.Add(new KeyCodeBindingInfo
            {
                KeyCode = KeyCode.D,
                UIType = UIType.MoveRight,
                DownTouchEventType = TouchEventType.MoveRightTouchDown,
                UpTouchEventType = TouchEventType.MoveRightTouchUp,
            }, false);

            keyCodeBindingDict.Add(new KeyCodeBindingInfo
            {
                KeyCode = KeyCode.Q,
                UIType = UIType.Action1,
                DownTouchEventType = TouchEventType.Action1TouchDown,
                UpTouchEventType = TouchEventType.Action1TouchUp,
            }, false);

            keyCodeBindingDict.Add(new KeyCodeBindingInfo
            {
                KeyCode = KeyCode.W,
                UIType = UIType.Action2,
                DownTouchEventType = TouchEventType.Action2TouchDown,
                UpTouchEventType = TouchEventType.Action2TouchUp,
            }, false);

            keyCodeBindingDict.Add(new KeyCodeBindingInfo
            {
                KeyCode = KeyCode.E,
                UIType = UIType.Action3,
                DownTouchEventType = TouchEventType.Action3TouchDown,
                UpTouchEventType = TouchEventType.Action3TouchUp,
            }, false);
        }

        private void ProcessInputs()
        {
            if (MainSystem.Instance.State != GameState.Playing)
                return;

            /*Vector3 totalDir = Vector3.zero;
            Direction dir = Direction.None;

            int dirCount = 0;
            if (Input.GetKey(KeyCode.A))
            {
                if (!leftPressed)
                {
                    totalDir += Vector3.left;
                    dirCount += 1;
                    leftPressed = true;
                    dir = Direction.Left;
                }
            }
            else
            {
                leftPressed = false;
            }

            if (Input.GetKey(KeyCode.D))
            {
                if (!rightPressed)
                {
                    totalDir += Vector3.right;
                    dirCount += 1;
                    rightPressed = true;
                    dir = Direction.Right;
                }
            }
            else
            {
                rightPressed = false;
            }*/

            var pressedButtonList = PooledList<KeyCodeBindingInfo>.Get();
            var releaseButtonList = PooledList<KeyCodeBindingInfo>.Get();

            foreach (KeyValuePair<KeyCodeBindingInfo, bool> kvp in keyCodeBindingDict)
            {
                var bindingInfo = kvp.Key;
                var pressed = kvp.Value;

                if (Input.GetKeyDown(bindingInfo.KeyCode))
                {
                    // 눌렀다 이벤트
                    pressedButtonList.Add(bindingInfo);
                    MessageSystem.Instance.Publish(TouchEvent.Create(bindingInfo.DownTouchEventType, Vector2.zero));
                }
                else if (Input.GetKeyUp(bindingInfo.KeyCode))
                {
                    // 떨어졌다 이벤트 
                    MessageSystem.Instance.Publish(TouchEvent.Create(bindingInfo.UpTouchEventType, Vector2.zero));
                    releaseButtonList.Add(bindingInfo);
                }
            }

            // 현재 바인딩 한 키들의 상태 변경. 
            for (int i = 0; i < pressedButtonList.Count; i++)
            {
                keyCodeBindingDict[pressedButtonList[i]] = true;
            }

            pressedButtonList.Dispose();

            for (int i = 0; i < releaseButtonList.Count; i++)
            {
                keyCodeBindingDict[releaseButtonList[i]] = true;
            }

            releaseButtonList.Dispose();
        }

        public void Dispose()
        {
            keyCodeBindingDict.Clear();
            keyCodeBindingDict = null;
        }
    }
}