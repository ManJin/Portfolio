using UnityEngine;

namespace clone
{
    public class HpChangedEvent : Event
    {
        public bool IsPlayer = false;
        public float ChangeValue = 0f;
        private static ObjectPool<HpChangedEvent> pool = new ObjectPool<HpChangedEvent>();
        
        public override void Dispose()
        {
            pool.Return(this);
        }
        
        public static HpChangedEvent Create(bool isPlayer, float value)
        {
            var e = pool.GetOrCreate();
            e.IsPlayer = isPlayer;
            e.ChangeValue = value;
            return e;
        }
    }    
}

