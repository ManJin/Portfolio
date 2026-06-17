using System.Collections;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace clone
{
    public enum PoolState
    {
        Prepare,
        Loading,
        Ready,
    }
    public class UnityObjectPool : MonoBehaviour
    {
         public string PresetName { get; private set; }
         // 최대 갯수
         private int maxSize = 10;
         // 로딩 상태 
         public PoolState State { get; private set; } = PoolState.Prepare;

         public GameObject OriginalPrefab = null;

         //오브젝트 풀
         private Queue<GameObject> pool = new Queue<GameObject>();
         // 사용중인것 리스트 
         private List<GameObject> inUseList = new List<GameObject>();

         private bool disposed = false;
         
         // 미리 생성해둘 갯수 
         private int count = 5;
         public int Count
         {
             get { return count; }
             private set
             {
                 if (!OriginalPrefab)
                 {
                     if (count <= value)
                     {//증가만 
                         IncreasePool(value - count);
                     }
                 }

                 count = value;
             }
         }

         private Transform transform;

         public Transform Transform
         {
             get
             {
                 if (transform == null)
                 {
                     transform = GetComponent<Transform>();
                 }
                 return transform;
             }
             
         }
         
         private static Dictionary<string,UnityObjectPool> poolDict = new Dictionary<string, UnityObjectPool>();
         
         public static UnityObjectPool GetOrCreate(string presetName)
         {
             UnityObjectPool pool;
             if (poolDict.TryGetValue(presetName, out pool))
             {
                 return pool;
             }

             var poolGo = new GameObject(presetName);
             pool = poolGo.AddComponent<UnityObjectPool>();
             pool.PresetName = presetName;
             pool.Count = 5;
             pool.Init();
             
             poolDict.Add(presetName, pool);
             return pool;
         }
         
         private void Init()
         {
             State = PoolState.Loading;
             // 프리팹 생성. 
             StartCoroutine(LoadOriginalPrefabAsync());
         }

         IEnumerator LoadOriginalPrefabAsync()
         {
             ResourceRequest request = Resources.LoadAsync<GameObject>(PresetName);
             while (request.isDone == false)
             {
                 yield return null; 
             }
             GameObject go = request.asset as GameObject;
             OriginalPrefab = go;
             // 풀 생성 
             IncreasePool(count);
             // 준비 완료 
             State = PoolState.Ready;
         }

         void IncreasePool(int amount)
         {
             for (int i = 0; i < amount; i++)
             {
                 var go = Instantiate<GameObject>(OriginalPrefab, Transform);
                 go.transform.localPosition = Vector3.zero;
                 // 비활성화 했다가 사용할때 켜서 씀. 
                 go.SetActive(false);
                 // 가용 풀에 등록. 
                 pool.Enqueue(go);
             }
         }
         
         void OnDestroy()
         {
             Dispose();
         }

         void Dispose()
         {
             disposed = true;
             // 사용중인것 풀에다 돌려둠. 
             for (int i = 0; i < inUseList.Count; i++)
             {
                 if (pool.Contains(inUseList[i]))
                 {
                    continue;                    
                 }
                 pool.Enqueue(inUseList[i]);
             }
             inUseList.Clear();
             //다 빼서 삭제
             while (pool.Count > 0)
             {
                 var go = pool.Dequeue();
                 if (go != null)
                 {
                     Destroy(go);
                 }
             }
         }

         public GameObject Instantiate(Vector2 position)
         {
             if (State != PoolState.Ready)
             {
                 Debug.LogErrorFormat("UnityObjectPool Not Ready Yet");
             }

             GameObject go = null;
             if (pool.Count > 0)
             {
                 go = pool.Dequeue();
                 go.SetActive(true);
                 go.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
             }
             else if (maxSize != 0 && Count == maxSize)
             {//최대량 넘어갔다면 사용한지 오래된걸 반환
                 go = inUseList[0];
                 // 사용하고 있는 리스트에서 삭제 
                 inUseList.RemoveAt(0);
                 //일단 반환 하고 재 할당. 
                 pool.Enqueue(go);
                 if (pool.Count > 0)
                 {
                     go = pool.Dequeue();
                 }
                 go.SetActive(true);
                 go.transform.SetLocalPositionAndRotation(position, Quaternion.identity);
             }
             else
             {
                 go = Instantiate<GameObject>(OriginalPrefab, position, Quaternion.identity);
                 go.transform.SetParent(transform);
                 // 신규 오브젝트 생성이라 카운트 추가
                 count++;
             }

             if (go)
             {
                 if (!inUseList.Contains(go))
                 {
                     inUseList.Add(go);
                 }
             }

             return go;
         }

         public void Return(GameObject go)
         {
             if (disposed)
             {// 파괴되는중이면 그냥 오브젝트 파괴 함. 
                 Destroy(go);
                 return;
             }

             // 사용중 리스트에서 지우고 풀에 리턴시킴. 
             bool removed = inUseList.Remove(go);
             if (removed)
             {
                 pool.Enqueue(go);
             }
         }
    }    
}

