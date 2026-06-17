using UnityEngine;

namespace clone
{
    public interface IEventListner
    {
        /// <summary>
        /// 이벤트 동기화용 
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        bool OnEvent(Event e);
    }    
}

