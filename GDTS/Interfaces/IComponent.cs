using UnityEngine;

namespace clone
{
    public interface IComponent
    {
        void AttachTo(FieldObject fo);
        void DetachFrom(FieldObject fo);
    }    
}

