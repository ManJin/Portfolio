using UnityEngine;
using UnityEngine.Pool;

namespace clone
{
    public class TouchEvent : Event
    {
        public TouchEventType TouchEventType { get; protected set; }
        
        public Vector2 TouchPosition { get; protected set; }

        private static ObjectPool<TouchEvent> pool = new ObjectPool<TouchEvent>();
        
        public override void Dispose()
        {
            pool.Return(this);
        }
        
        public static TouchEvent Create(TouchEventType type, Vector2 touchPosition)
        {
            var e = pool.GetOrCreate();
            e.TouchEventType = type;
            e.TouchPosition = touchPosition;
            return e;
        }
    }    
}

