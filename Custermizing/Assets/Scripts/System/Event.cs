using UnityEngine;

namespace Portfolio
{
    public abstract class Event : IDisposable
    {
        public abstract void Dispose();
    }    
}

