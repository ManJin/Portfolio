using System.Collections.Generic;
using UnityEngine;

namespace clone
{
    public class FieldObject : MonoBehaviour, IEventListner
    {
        // 모든 오브젝트는 충돌 처리하기위한 바운드를 가짐. 
        public Bounds HittableBounds;

        

        public EntityGroup EntityGroup { get; set; }

        bool IEventListner.OnEvent(Event e)
        {
            return false;
        }
    }

    public struct FieldObjectDistance
    {
        /// <summary>
        /// 바운딩 박스 사이의 거리(딱 붙거나 겹치면 0)
        /// </summary>
        /// <remarks>거리 정렬에만 이용하므로 제곱한 값을 이용</remarks>
        public float distance;

        /// <summary>
        /// 필드 오브젝트 중심점 사이의 거리(완전히 딱 붙었을 경우 그래도 뭐가 더 가까워 보이는지 판별하는데 사용)
        /// </summary>
        /// <remarks>거리 정렬에만 이용하므로 제곱한 값을 이용</remarks>
        public float distanceBtwnOrigins;
    }


    public class FieldObjectDistanceComparer : IComparer<FieldObjectDistance>
    {
        public int Compare(FieldObjectDistance x, FieldObjectDistance y)
        {
            return (x.distance < y.distance ? -1
                : x.distance > y.distance ? 1
                : x.distanceBtwnOrigins < y.distanceBtwnOrigins ? -1 : 1);
        }
    }    
}
