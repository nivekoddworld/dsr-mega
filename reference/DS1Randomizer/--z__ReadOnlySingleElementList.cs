using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x02000023 RID: 35
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<!0>, IReadOnlyCollection<!0>, IReadOnlyList<!0>, ICollection<!0>, IList<!0>
{
	// Token: 0x060000EF RID: 239 RVA: 0x0000731C File Offset: 0x0000551C
	public <>z__ReadOnlySingleElementList(T item)
	{
		this._item = item;
	}

	// Token: 0x060000F0 RID: 240 RVA: 0x0000732B File Offset: 0x0000552B
	[return: Nullable(1)]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return new <>z__ReadOnlySingleElementList<T>.Enumerator(this._item);
	}

	// Token: 0x17000047 RID: 71
	// (get) Token: 0x060000F1 RID: 241 RVA: 0x000072B7 File Offset: 0x000054B7
	int ICollection.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x17000048 RID: 72
	// (get) Token: 0x060000F2 RID: 242 RVA: 0x00007288 File Offset: 0x00005488
	bool ICollection.IsSynchronized
	{
		get
		{
			return false;
		}
	}

	// Token: 0x17000049 RID: 73
	// (get) Token: 0x060000F3 RID: 243 RVA: 0x0000728B File Offset: 0x0000548B
	object ICollection.SyncRoot
	{
		[return: Nullable(1)]
		get
		{
			return this;
		}
	}

	// Token: 0x060000F4 RID: 244 RVA: 0x00007338 File Offset: 0x00005538
	void ICollection.CopyTo([Nullable(1)] Array array, int index)
	{
		array.SetValue(this._item, index);
	}

	// Token: 0x1700004A RID: 74
	object IList.this[int index]
	{
		[return: Nullable(2)]
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
		[param: Nullable(2)]
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x1700004B RID: 75
	// (get) Token: 0x060000F7 RID: 247 RVA: 0x000072B7 File Offset: 0x000054B7
	bool IList.IsFixedSize
	{
		get
		{
			return true;
		}
	}

	// Token: 0x1700004C RID: 76
	// (get) Token: 0x060000F8 RID: 248 RVA: 0x000072B7 File Offset: 0x000054B7
	bool IList.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x060000F9 RID: 249 RVA: 0x000072B0 File Offset: 0x000054B0
	int IList.Add([Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000FA RID: 250 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000FB RID: 251 RVA: 0x00007362 File Offset: 0x00005562
	bool IList.Contains([Nullable(2)] object value)
	{
		return EqualityComparer<T>.Default.Equals(this._item, (T)((object)value));
	}

	// Token: 0x060000FC RID: 252 RVA: 0x0000737A File Offset: 0x0000557A
	int IList.IndexOf([Nullable(2)] object value)
	{
		if (!EqualityComparer<T>.Default.Equals(this._item, (T)((object)value)))
		{
			return -1;
		}
		return 0;
	}

	// Token: 0x060000FD RID: 253 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Insert(int index, [Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000FE RID: 254 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Remove([Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000FF RID: 255 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000100 RID: 256 RVA: 0x0000732B File Offset: 0x0000552B
	[return: Nullable(new byte[]
	{
		1,
		0
	})]
	IEnumerator<T> IEnumerable<!0>.GetEnumerator()
	{
		return new <>z__ReadOnlySingleElementList<T>.Enumerator(this._item);
	}

	// Token: 0x1700004D RID: 77
	// (get) Token: 0x06000101 RID: 257 RVA: 0x000072B7 File Offset: 0x000054B7
	int IReadOnlyCollection<!0>.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x1700004E RID: 78
	T IReadOnlyList<!0>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
	}

	// Token: 0x1700004F RID: 79
	// (get) Token: 0x06000103 RID: 259 RVA: 0x000072B7 File Offset: 0x000054B7
	int ICollection<!0>.Count
	{
		get
		{
			return 1;
		}
	}

	// Token: 0x17000050 RID: 80
	// (get) Token: 0x06000104 RID: 260 RVA: 0x000072B7 File Offset: 0x000054B7
	bool ICollection<!0>.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x06000105 RID: 261 RVA: 0x000072B0 File Offset: 0x000054B0
	void ICollection<!0>.Add(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000106 RID: 262 RVA: 0x000072B0 File Offset: 0x000054B0
	void ICollection<!0>.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x06000107 RID: 263 RVA: 0x000073A8 File Offset: 0x000055A8
	bool ICollection<!0>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(this._item, item);
	}

	// Token: 0x06000108 RID: 264 RVA: 0x000073BB File Offset: 0x000055BB
	void ICollection<!0>.CopyTo([Nullable(new byte[]
	{
		1,
		0
	})] T[] array, int arrayIndex)
	{
		array[arrayIndex] = this._item;
	}

	// Token: 0x06000109 RID: 265 RVA: 0x000072B0 File Offset: 0x000054B0
	bool ICollection<!0>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x17000051 RID: 81
	T IList<!0>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return this._item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x0600010C RID: 268 RVA: 0x000073CA File Offset: 0x000055CA
	int IList<!0>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(this._item, item))
		{
			return -1;
		}
		return 0;
	}

	// Token: 0x0600010D RID: 269 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList<!0>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x0600010E RID: 270 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList<!0>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x0400009C RID: 156
	[CompilerGenerated]
	private readonly T _item;

	// Token: 0x02000024 RID: 36
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		// Token: 0x0600010F RID: 271 RVA: 0x000073E2 File Offset: 0x000055E2
		public Enumerator(T item)
		{
			this.System.Collections.Generic.IEnumerator<T>.Current = item;
		}

		// Token: 0x17000052 RID: 82
		// (get) Token: 0x06000110 RID: 272 RVA: 0x000073F1 File Offset: 0x000055F1
		object IEnumerator.Current
		{
			get
			{
				return this._item;
			}
		}

		// Token: 0x17000053 RID: 83
		// (get) Token: 0x06000111 RID: 273 RVA: 0x000073FE File Offset: 0x000055FE
		T IEnumerator<!0>.Current
		{
			get
			{
				return this._item;
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x00007408 File Offset: 0x00005608
		bool IEnumerator.MoveNext()
		{
			return !this._moveNextCalled && (this._moveNextCalled = true);
		}

		// Token: 0x06000113 RID: 275 RVA: 0x00007429 File Offset: 0x00005629
		void IEnumerator.Reset()
		{
			this._moveNextCalled = false;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00007432 File Offset: 0x00005632
		void IDisposable.Dispose()
		{
		}

		// Token: 0x0400009D RID: 157
		[CompilerGenerated]
		private readonly T _item;

		// Token: 0x0400009E RID: 158
		[CompilerGenerated]
		private bool _moveNextCalled;
	}
}
