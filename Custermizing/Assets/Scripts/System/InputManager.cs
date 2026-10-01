
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

namespace Portfolio
{
    public class InputManager : IEventListner, IUpdatable, IDisposable
    {
       // public TouchController touchController { get; private set; }
        public KeyboardController keyboardController { get; private set; }

        /*private World world;
        private EntityQuery inputQuery;*/

        public bool MoveLeft { get; private set; } = false;
        public bool MoveRight { get; private set; } = false;
        public bool MoveUp { get; private set; } = false;
        public bool MoveDown { get; private set; } = false;
        
        public InputManager()
        {
            /*world = World.DefaultGameObjectInjectionWorld;
            inputQuery = world.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerInput>(),
                ComponentType.ReadOnly<GhostOwnerIsLocal>());*/
            MessageSystem.Instance.Subscribe<TouchEvent>(((IEventListner)this).OnEvent);
            
        }

        bool IEventListner.OnEvent(Event e)
        {
            if (e is TouchEvent te)
            {
                if (te.TouchEventType == TouchEventType.MoveDownTouchDown)
                {
                    MoveDown = true;
                    /*Vector2 move = Vector2.zero;
                    move.x += 1;
                    input.Move = move;
                    world.EntityManager.SetComponentData(entity, input);*/
                }
                else if (te.TouchEventType == TouchEventType.MoveDownTouchUp)
                {
                    MoveDown = false;
                }
                else if (te.TouchEventType == TouchEventType.MoveUpTouchDown)
                {
                    MoveUp = true;
                }
                else if (te.TouchEventType == TouchEventType.MoveUpTouchUp)
                {
                    MoveUp = false;
                }
                else if (te.TouchEventType == TouchEventType.MoveLeftTouchDown)
                {
                    MoveLeft = true;
                }
                else if (te.TouchEventType == TouchEventType.MoveLeftTouchUp)
                {
                    MoveLeft = false;
                }
                else if (te.TouchEventType == TouchEventType.MoveRightTouchDown)
                {
                    MoveRight = true;
                }
                else if (te.TouchEventType == TouchEventType.MoveRightTouchUp)
                {
                    MoveRight = false;
                }
            }
            return false;
        }
        
        void IUpdatable.UpdateFrame(float dt)
        {
            (keyboardController as IUpdatable)?.UpdateFrame(dt);
        }

        void IDisposable.Dispose()
        {
            MessageSystem.Instance.Unsubscribe<TouchEvent>(((IEventListner)this).OnEvent);
            (keyboardController as IDisposable)?.Dispose();
            keyboardController = null;
            /*world.Dispose();
            world = null;
            if(inputQuery != default) 
                inputQuery.Dispose();*/
        }

        public void LoadControllers()
        {
            keyboardController = new KeyboardController();
           // touchController = new TouchController()
        }
    }    
}
