using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace clone
{
    public abstract class ObjectPool
    {
        protected static List<ObjectPool> objectPools = new List<ObjectPool>();

        public abstract void Clear();
        
        public static void ClearAll()
        {
            for (int i = 0; i < objectPools.Count; i++)
            {
                objectPools[i].Clear();
            }
        }
    }
    public class ObjectPool<T> : ObjectPool where T : class, new()
    {
        private static Stack<T> pool = new Stack<T>();

        private int newCount;

        public int PoolCount => pool.Count;

        public ObjectPool()
        {
            objectPools.Add(this);
            newCount = 0;
        }

        public T GetOrCreate()
        {
            if (pool.Count > 0)
            {
                return pool.Pop();
            }
            else
            {
                ++newCount;
                return new T();
            }
        }

        public void Return(T obj)
        {
            if (pool.Contains(obj))
                return;
            
            pool.Push(obj);
        }

        public override void Clear()
        {
            newCount = 0;
            pool.Clear();
        }
    }
    
    
}

