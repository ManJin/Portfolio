using UnityEngine;

namespace Portfolio
{
    public interface IComponent
    {
        void AttachTo(FieldObject fo);
        void DetachFrom(FieldObject fo);
    }    
}

