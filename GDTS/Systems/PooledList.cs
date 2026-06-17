using System;
using System.Collections.Generic;
using System.Threading;

namespace clone
{
	/// <summary>
	/// PooledList 모음
	/// </summary>
	public static class PooledList
	{
		public static event Action PoolCleared;

		public static void ClearAll()
		{
			PoolCleared?.Invoke();
		}

		public static List<Type> PoolCreated = new List<Type>();
	}

	/// <summary>
	/// 풀링하는 리스트(리스트를 리턴할 때 사용)
	/// </summary>
	/// <remarks>사용이 완료된 리스트는 Dipose 해서 재사용 가능하게 해야 함</remarks>
	public class PooledList<T> : List<T>, IDisposable
	{
		private static Stack<PooledList<T>> pool = new Stack<PooledList<T>>();

		static PooledList()
		{
			// PooledList 클리어 이벤트 받으면 내용 날림
			PooledList.PoolCleared += () =>
			{
				pool.Clear();
			};
			PooledList.PoolCreated.Add(typeof(PooledList<T>));
		}

		public static PooledList<T> Get()
		{
			if (pool.Count > 0)
			{
				var obj = pool.Pop();
				obj.disposed = 0;

				return obj;
			}
			else
			{
				return new PooledList<T>();
			}
		}

		/// <summary>
		/// Pool의 Stack내에 보관중인 재활용 오브젝트 수
		/// </summary>
		public static int PoolCount
		{
			get { return pool.Count; }
		}

		#region IDisposable
		/// <summary>
		/// 재사용 가능하도록 해제되었는지(0: 사용 중, 1: 해제 됨)
		/// </summary>
		private int disposed = 0;

		/// <summary>
		/// 사용을 끝내고 풀로 되돌림
		/// </summary>
		public void Dispose()
		{
			// 오브젝트가 풀에 한 번만 들어가도록
			if (Interlocked.Exchange(ref disposed, 1) != 1)
			{
				// 리스트 내용 청소
				Clear();

				// 풀에 삽입
				pool.Push(this);
			}
		}
		#endregion
	}
}
