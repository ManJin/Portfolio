using Unity.XR.Oculus.Input;
using UnityEngine;

namespace clone
{
    public class InputManager : IUpdatable, IDisposable
    {
       // public TouchController touchController { get; private set; }
        public KeyboardController keyboardController { get; private set; }
        
        void IUpdatable.UpdateFrame(float dt)
        {
            (keyboardController as IUpdatable)?.UpdateFrame(dt);
        }

        void IDisposable.Dispose()
        {
            (keyboardController as IDisposable)?.Dispose();
            keyboardController = null;
        }

        public void LoadControllers()
        {
            keyboardController = new KeyboardController();
           // touchController = new TouchController()
        }
    }    
}
