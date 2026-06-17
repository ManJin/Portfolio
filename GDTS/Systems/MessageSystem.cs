using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace clone
{
    /// <summary>
    /// 구독 콜백
    /// </summary>
    public delegate bool SubscribeCallback(Event e);

    public struct SubscribeCallbackInfo : IEquatable<SubscribeCallbackInfo>
    {
        public SubscribeCallback Callback;
        /// <summary>
        /// 일회성 구독인가 
        /// </summary>
        public bool SubscribeOnce;

        public SubscribeCallbackInfo(SubscribeCallback callback, bool subscribeOnce = false)
        {
            Callback = callback;
            SubscribeOnce = subscribeOnce;
        }
        
        public bool Equals(SubscribeCallbackInfo info)
        {
            // 종류 / 횟수 같아야 
            return info.Callback == Callback &&  info.SubscribeOnce == SubscribeOnce;
        }

        public override bool Equals(object obj)
        {
            return obj is SubscribeCallbackInfo info && Equals(info);
        }

        public override int GetHashCode()
        {
            return Callback.GetHashCode();
        }
    }
    public class MessageSystem
    {
        private static MessageSystem instance = null;
        public static MessageSystem Instance
        {
            get
            {
                if (instance == null)
                    instance = new MessageSystem();
                return instance;
            }
        }

        private struct PublishedEvent
        {
            /// <summary>
            /// 이벤트 받을 대상
            /// </summary>
            public IEventListner target;

            /// <summary>
            /// 이벤트
            /// </summary>
            public Event e;
        }
        
        /// <summary>
        /// 이번 프레임에 발행된 이벤트 
        /// </summary>
        private List<PublishedEvent> publishedOnThisFrame = new List<PublishedEvent>();

        /// <summary>
        /// 현재 구독중
        /// </summary>
        private Dictionary<Type, List<SubscribeCallbackInfo>> subscribeCallbacks =
            new Dictionary<Type, List<SubscribeCallbackInfo>>();
        
        /// <summary>
        /// 구독 신규 요청
        /// </summary>
        private List<KeyValuePair<Type, SubscribeCallbackInfo>> requestCallbacks =
            new List<KeyValuePair<Type, SubscribeCallbackInfo>>();
        
        /// <summary>
        /// 구독 해지요청
        /// </summary>
        private List<KeyValuePair<Type, SubscribeCallback>> unSubscribeCallbacks =
            new List<KeyValuePair<Type, SubscribeCallback>>();

        /// <summary>
        /// 메인 시스템에서 업데이트 실행. 
        /// </summary>
        public void Update()
        {
            //신규 요청 추가 
            foreach (var item in requestCallbacks)
            {
                List<SubscribeCallbackInfo> callbacks = null;
                if (subscribeCallbacks.TryGetValue(item.Key, out callbacks))
                {
                    callbacks.Add(item.Value);
                }
                else
                {
                    callbacks = new List<SubscribeCallbackInfo>();
                    callbacks.Add(item.Value);

                    subscribeCallbacks.Add(item.Key, callbacks);
                }
            }

            // 다 더해줫으니 제거 
            requestCallbacks.Clear();

            // 구독 삭제
            foreach (var item in unSubscribeCallbacks)
            {
                List<SubscribeCallbackInfo> callbacks = null;
                {
                    if (subscribeCallbacks.TryGetValue(item.Key, out callbacks))
                    {
                        SubscribeCallbackInfo info = new SubscribeCallbackInfo(item.Value);
                        if (callbacks.Remove(info))
                        {
                            if (callbacks.Count == 0)
                            {
                                subscribeCallbacks.Remove(item.Key);
                            }
                        }
                    }
                }
            }

            // 다 빼주면 제거 
            unSubscribeCallbacks.Clear();

            // 이번 프레임용 
            for (int i = 0; i < publishedOnThisFrame.Count; i++)
            {
                List<SubscribeCallbackInfo> callbacks = null;
                PublishedEvent publishedEvent = publishedOnThisFrame[i];
                if (publishedEvent.target == null)
                {
                    
                    if (subscribeCallbacks.TryGetValue(publishedEvent.e.GetType(), out callbacks))
                    {
                        for (int j = 0; j < callbacks.Count; j++)
                        {
                            var callback = callbacks[j];
                            bool removed = false;

                            foreach (var unSubs in unSubscribeCallbacks)
                            {
                                if (unSubs.Key == publishedEvent.e.GetType() && unSubs.Value == callback.Callback)
                                {
                                    removed = true;
                                    break;
                                }
                            }

                            if (removed)
                            {
                                continue;
                            }

                            callback.Callback.Invoke(publishedEvent.e);

                            //일회성 구독 삭제  
                            if (callback.SubscribeOnce)
                            {
                                callbacks.RemoveAt(j--);
                            }
                        }
                    }
                }
                else
                {
                    // 이벤트 실행 
                    publishedEvent.target.OnEvent(publishedEvent.e);
                }

                publishedEvent.e.Dispose();
            }
            // 이번 프레임 종료함. 
            publishedOnThisFrame.Clear();
        }

        public void Publish<T>(T e) where T : Event
        {
            publishedOnThisFrame.Add(new PublishedEvent
            {
                e = e,
            });
        }

        //즉시 invoke 시킴
        public void PublishSync<T>(T e) where T : Event
        {
            List<SubscribeCallbackInfo> callbacks = null;
            if (subscribeCallbacks.TryGetValue(typeof(T), out callbacks))
            {
                foreach (var info in callbacks)
                {
                    info.Callback.Invoke(e);
                }
            }
            e.Dispose();
        }

        // 특정 타겟에 전송
        public void Send(IEventListner target, Event e)
        {
            // 타겟이 있을때만 
            if (target != null)
            {
                publishedOnThisFrame.Add(new PublishedEvent
                {
                    target = target,
                    e = e,
                });
            }
        }

        public bool SendSync(IEventListner target, Event e, bool dispose = true)
        {
            bool result = false;
            if (target != null)
            {
                result = target.OnEvent(e);
            }

            if (dispose)
            {
                e.Dispose();
            }

            return result;
        }

        public void Subscribe<T>(SubscribeCallback callback) where T : Event
        {
            Subscribe(typeof(T), callback);
        }

        public void Subscribe(Type type, SubscribeCallback callback)
        {
            requestCallbacks.Add(new KeyValuePair<Type, SubscribeCallbackInfo>(type, new SubscribeCallbackInfo(callback)));
        }
        
        public void Subscribe(Type[] types, SubscribeCallback callback)
        {
            for (int i = 0; i < types.Length; i++)
            {
                requestCallbacks.Add(
                    new KeyValuePair<Type, SubscribeCallbackInfo>(types[i], new SubscribeCallbackInfo(callback)));
            }
        }
        
        public void SubscribeOnce<T>(SubscribeCallback callback) where T : Event
        {
            SubscribeOnce(typeof(T), callback);
        }
        
        public void SubscribeOnce(Type type, SubscribeCallback callback)
        {
            requestCallbacks.Add(
                new KeyValuePair<Type, SubscribeCallbackInfo>(type, new SubscribeCallbackInfo(callback, true)));
        }
        
        /*public SubscribeCoroutine SubscribeOnceAsync<T>(SubscribeCallback cb) where T : Event
        {
            return SubscribeOnceAsync(typeof(T), cb);
        }
        
        public SubscribeCoroutine SubscribeOnceAsync(Type topic, SubscribeCallback cb)
        {
            return new SubscribeCoroutine(this, topic, cb);
        }*/
        public void Unsubscribe<T>(SubscribeCallback callback) where T : Event
        {
            Unsubscribe(typeof(T), callback);
        }
        
        public void Unsubscribe(Type type, SubscribeCallback callback)
        {
            unSubscribeCallbacks.Add(new KeyValuePair<Type, SubscribeCallback>(type, callback));
        }
        
        public void Unsubscribe(Type[] types, SubscribeCallback callback)
        {
            for (int i = 0; i < types.Length; i++)
            {
                unSubscribeCallbacks.Add(new KeyValuePair<Type, SubscribeCallback>(types[i], callback));
            }
        }
    }

    /*public class SubscribeCoroutine : Coroutine
    {
        public SubscribeCoroutine(MessageSystem messageSystem, Type type, SubscribeCallback callback) :
            base(null, Routine(messageSystem, type, callback))
        {
            
        }

        private static IEnumerator Routine(MessageSystem messageSystem, Type type, SubscribeCallback callback)
        {
            bool got = false;
            SubscribeCallback waitCallback = (e) =>
            {
                got = true;
                return callback(e);
            };
            messageSystem.Subscribe(type, waitCallback);
            while (!got) yield return null;
            messageSystem.Unsubscribe(type, waitCallback);
        }
    }*/
}

