using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace clone
{
    public class MainSystem : MonoBehaviour, IDisposable
    {
        public static MainSystem Instance { get; private set; }
    
        public GameState State = GameState.Prepare;
    
        public InputManager InputManager { get; private set; }

        // 등록된 플레이어 캐릭터 (적/나)
        public List<Character> Characters { get; } = new List<Character>();

        public List<FieldObject> Objects = new List<FieldObject>();
        
        public Character player;
        public Character enemy;

        public ProjectileManager ProjectileManager = null;
        #region UpdateFrame

        private struct UpdateFrameInfo<T>
        {
            public int priority;
            public T updatable;
            
        }

        private enum UpdateFrameOperation
        {
            Add,
            Remove,
        }

        private struct UpdateFrameList<T> where T : class
        {
            public UpdateFrameInfo<T>[] callbacks;
            public int count;
            public List<(UpdateFrameOperation, UpdateFrameInfo<T>)> deferredOps;

            public UpdateFrameList(int initSize)
            {
                callbacks = new UpdateFrameInfo<T>[initSize];
                deferredOps = new List<(UpdateFrameOperation, UpdateFrameInfo<T>)>();
                count = 0;
            }

            public void Insert(UpdateFrameInfo<T> info)
            {
                if (callbacks.Length <= count)
                {
                    Array.Resize(ref callbacks, count * 2);
                }

                int i;
                for (i = 0; i < count; i++)
                {
                    // 위쪽에서 부터 작업해야 값이 날아가지 않음. 
                    for (int j = count; j > i; j--)
                    {
                        callbacks[j] =  callbacks[j - 1];
                    }
                    break;
                }

                callbacks[i] = info;
                count++;
            }

            public void Remove(UpdateFrameInfo<T> info)
            {
                for (int i = 0; i < count; i++)
                {
                    if (callbacks[i].updatable == info.updatable)
                    {
                        // 한 칸씩 당김
                        for (int j = i; j < count - 1; j++)
                        {
                            callbacks[j] = callbacks[j + 1];
                        }

                        // 마지막 콜백 참조 해제
                        callbacks[--count].updatable = null;
                        break;
                    }
                }
            }
        }
        
        private UpdateFrameList<IUpdatable> updateList = new UpdateFrameList<IUpdatable>(1024);
        private UpdateFrameList<ILateUpdatable> lateUpdateList = new UpdateFrameList<ILateUpdatable>(1024);
        private UpdateFrameList<IFixedUpdatable> fixedUpdateList = new UpdateFrameList<IFixedUpdatable>(1024);

        public void AddUpdateFrameCallback(int priority, IUpdatable updatable)
        {
            var info = new UpdateFrameInfo<IUpdatable>{priority = priority, updatable = updatable};
            updateList.deferredOps.Add((UpdateFrameOperation.Add, info));
        }

        public void RemoveUpdateFrameCallback(int priority, IUpdatable updatable)
        {
            var info = new UpdateFrameInfo<IUpdatable>{priority = priority, updatable = updatable};
            updateList.deferredOps.Add((UpdateFrameOperation.Remove, info));
        }
        
        public void AddLateUpdateFrameCallback(int priority, ILateUpdatable lateUpdatable)
        {
            var info = new UpdateFrameInfo<ILateUpdatable>{priority = priority, updatable = lateUpdatable};
            lateUpdateList.deferredOps.Add((UpdateFrameOperation.Add, info));
        }

        public void RemoveLateUpdateFrameCallback(int priority, ILateUpdatable lateUpdatable)
        {
            var info = new UpdateFrameInfo<ILateUpdatable>{priority = priority, updatable = lateUpdatable};
            lateUpdateList.deferredOps.Add((UpdateFrameOperation.Remove, info));
        }
        
        public void AddFixedUpdateFrameCallback(int priority, IFixedUpdatable fixedUpdatable)
        {
            var info = new UpdateFrameInfo<IFixedUpdatable>{priority = priority, updatable = fixedUpdatable};
            fixedUpdateList.deferredOps.Add((UpdateFrameOperation.Add, info));
        }

        public void RemoveFixedUpdateFrameCallback(int priority, IFixedUpdatable fixedUpdatable)
        {
            var info = new UpdateFrameInfo<IFixedUpdatable>{priority = priority, updatable = fixedUpdatable};
            fixedUpdateList.deferredOps.Add((UpdateFrameOperation.Remove, info));
        }
        
        #endregion
        #region Mono
        void Awake()
        {
            DontDestroyOnLoad(this);
            Instance = this;
            InputManager = new InputManager();
            Characters.Add(player);
            Characters.Add(enemy);
        }
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            InputManager.LoadControllers();
            MakeProjectileSpecs();
        }

        // 여기 있는 정보로 프로젝타일 발사 할것. 
        void MakeProjectileSpecs()
        {
            UnityObjectPool.GetOrCreate("NormalProjectile");
            UnityObjectPool.GetOrCreate("Skill1Projectile");
            UnityObjectPool.GetOrCreate("Skill2Projectile");
        }

        // Update is called once per frame
        void Update()
        {
            foreach (var operation in updateList.deferredOps)
            {
                if (operation.Item1 == UpdateFrameOperation.Add)
                {
                    updateList.Insert(operation.Item2);
                }
                else
                {
                    updateList.Remove(operation.Item2);
                }
            }
            updateList.deferredOps.Clear();
            
            MessageSystem.Instance.Update();

            float dt = Time.deltaTime;

            // 업데이트 콜백 실행. 
            for (int i = 0; i < updateList.count; i++)
            {
                updateList.callbacks[i].updatable.UpdateFrame(dt);
            }
            
            (InputManager as IUpdatable)?.UpdateFrame(dt);
        }
        #endregion

        private void LateUpdate()
        {
            foreach (var op in lateUpdateList.deferredOps)
            {
                if (op.Item1 == UpdateFrameOperation.Add)
                {
                    lateUpdateList.Insert(op.Item2);
                }
                else
                {
                    lateUpdateList.Remove(op.Item2);
                }
            }

            lateUpdateList.deferredOps.Clear();
            
            float dt = Time.deltaTime;

            // late업데이트 콜백 실행. 
            for (int i = 0; i < lateUpdateList.count; i++)
            {
                lateUpdateList.callbacks[i].updatable.LateUpdateFrame(dt);
            }
        }

        void FixedUpdate()
        {
            foreach (var op in fixedUpdateList.deferredOps)
            {
                if (op.Item1 == UpdateFrameOperation.Add)
                {
                    fixedUpdateList.Insert(op.Item2);
                }
                else
                {
                    fixedUpdateList.Remove(op.Item2);
                }
            }

            fixedUpdateList.deferredOps.Clear();
            
            float dt = Time.fixedDeltaTime;

            // Fixed 업데이트 콜백 실행. 
            for (int i = 0; i < fixedUpdateList.count; i++)
            {
                fixedUpdateList.callbacks[i].updatable.FixedUpdateFrame(dt);
            }
        }
        #region Interfaces

        void IDisposable.Dispose()
        {
            Dispose();
        }
        #endregion

        private void Dispose()
        {
            Instance = null;
            (InputManager as IDisposable).Dispose();
            InputManager = null;
        }
        
        /// <summary>
        /// 충돌 확인할 오브젝트를 검사 해야 하는데 ..
        /// 모든 오브젝트는 Bounds 를 가지고 오브젝트의 이동시 해당 바운드 사이즈와 충돌 검사 할수 있도록 한다
        /// 생성된 오브젝트는 전부 리스트에 등록해서 정보를 가지고 있도록 한다.
        /// 
        /// </summary>
        /// <param name="bounds"></param>
        /// <param name="moveVector"></param>
        /// <returns></returns>
        public PooledList<FieldObject> GetFieldObjectsCollideBy(Bounds bounds, Vector2 moveVector)
        {
            // 튜플 
            PooledList<FieldObject> pooledList = new PooledList<FieldObject>();
            Vector2 center = bounds.center;
            Bounds moveBounds = bounds;
            moveBounds.center += new Vector3(moveVector.x * 0.5f, moveVector.y * 0.5f);
            // 사선으로 움직이는지 
            bool diagonal = (moveVector.x != 0 && moveVector.y != 0);

            // 캐릭터 리스트에서 충돌 하는것이 있는지 확인 
            for (int i = 0; i < Characters.Count; i++)
            {
                // 닿아있는 부분이 있으면 0이 아니므로.. 
                if (!Utils.IsAlmostZero(GetOverlappedBounds(bounds, Characters[i].HittableBounds)))
                {
                    //Debug.LogFormat("GetFieldObjectsCollideBy {0}" , Characters[i]);
                    var fo = Characters[i];
                    // 이미 추가된거면 그냥 둠. 
                    if (pooledList.Contains(fo))
                    {
                        continue;
                    }
                    
                    float dist = (!diagonal ? Utils.GetDistanceBetween(bounds, fo.HittableBounds) 
                        : Utils.GetDistanceOnRay(bounds, fo.HittableBounds, moveVector.normalized));

                    if (dist <= moveVector.magnitude + 0.0001f)
                    {// 바운드 끼리 곂쳤으면 리스트에 포함. 
                        pooledList.Add(fo);
                    }
                }
            }

            for (int i = 0; i < Objects.Count; i++)
            {
                // 닿아있는 부분이 있으면 0이 아니므로.. 
                if (!Utils.IsAlmostZero(GetOverlappedBounds(bounds, Objects[i].HittableBounds)))
                {
                    var fo = Objects[i];
                    if (pooledList.Contains(fo))
                    {
                        continue;
                    }
                    
                    float dist = (!diagonal ? Utils.GetDistanceBetween(bounds, fo.HittableBounds) 
                        : Utils.GetDistanceOnRay(bounds, fo.HittableBounds, moveVector.normalized));

                    if (dist <= moveVector.magnitude + 0.0001f)
                    {// 바운드 끼리 곂쳤으면 리스트에 포함. 
                        pooledList.Add(fo);
                    }
                }
            }

            return pooledList;
        }

        /// <summary>
        /// 곂치지 않아야 0 리턴 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public float GetOverlappedBounds(Bounds a, Bounds b)
        {
            Vector3 minA = a.min, maxA = a.max;
            Vector3 minB = b.min, maxB = b.max;

            float minX = Mathf.Max(minA.x, minB.x), minY = Mathf.Max(minA.y, minB.y);
            float maxX = Mathf.Min(maxA.x, maxB.x), maxY = Mathf.Min(maxA.y, maxB.y);

            return Mathf.Max(0, maxX - minX) * Mathf.Max(0, maxY - minY);
        }
    }
    
}
