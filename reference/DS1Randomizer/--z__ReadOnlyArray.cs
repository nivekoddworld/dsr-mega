using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// Token: 0x02000022 RID: 34
[CompilerGenerated]
internal sealed class <>z__ReadOnlyArray<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	// Token: 0x060000CF RID: 207 RVA: 0x00007262 File Offset: 0x00005462
	public <>z__ReadOnlyArray(T[] items)
	{
		this._items = items;
	}

	// Token: 0x060000D0 RID: 208 RVA: 0x00007271 File Offset: 0x00005471
	[return: Nullable(1)]
	IEnumerator IEnumerable.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	// Token: 0x1700003C RID: 60
	// (get) Token: 0x060000D1 RID: 209 RVA: 0x0000727E File Offset: 0x0000547E
	int ICollection.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x1700003D RID: 61
	// (get) Token: 0x060000D2 RID: 210 RVA: 0x00007288 File Offset: 0x00005488
	bool ICollection.IsSynchronized
	{
		get
		{
			return false;
		}
	}

	// Token: 0x1700003E RID: 62
	// (get) Token: 0x060000D3 RID: 211 RVA: 0x0000728B File Offset: 0x0000548B
	object ICollection.SyncRoot
	{
		[return: Nullable(1)]
		get
		{
			return this;
		}
	}

	// Token: 0x060000D4 RID: 212 RVA: 0x0000728E File Offset: 0x0000548E
	void ICollection.CopyTo([Nullable(1)] Array array, int index)
	{
		this._items.CopyTo(array, index);
	}

	// Token: 0x1700003F RID: 63
	object IList.this[int index]
	{
		[return: Nullable(2)]
		get
		{
			return this._items[index];
		}
		[param: Nullable(2)]
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x17000040 RID: 64
	// (get) Token: 0x060000D7 RID: 215 RVA: 0x000072B7 File Offset: 0x000054B7
	bool IList.IsFixedSize
	{
		get
		{
			return true;
		}
	}

	// Token: 0x17000041 RID: 65
	// (get) Token: 0x060000D8 RID: 216 RVA: 0x000072B7 File Offset: 0x000054B7
	bool IList.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x060000D9 RID: 217 RVA: 0x000072B0 File Offset: 0x000054B0
	int IList.Add([Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000DA RID: 218 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000DB RID: 219 RVA: 0x000072BA File Offset: 0x000054BA
	bool IList.Contains([Nullable(2)] object value)
	{
		return this._items.Contains(value);
	}

	// Token: 0x060000DC RID: 220 RVA: 0x000072C8 File Offset: 0x000054C8
	int IList.IndexOf([Nullable(2)] object value)
	{
		return this._items.IndexOf(value);
	}

	// Token: 0x060000DD RID: 221 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Insert(int index, [Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000DE RID: 222 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.Remove([Nullable(2)] object value)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000DF RID: 223 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000E0 RID: 224 RVA: 0x000072D6 File Offset: 0x000054D6
	[return: Nullable(new byte[]
	{
		1,
		0
	})]
	IEnumerator<T> IEnumerable<!0>.GetEnumerator()
	{
		return this._items.GetEnumerator();
	}

	// Token: 0x17000042 RID: 66
	// (get) Token: 0x060000E1 RID: 225 RVA: 0x0000727E File Offset: 0x0000547E
	int IReadOnlyCollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x17000043 RID: 67
	T IReadOnlyList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
	}

	// Token: 0x17000044 RID: 68
	// (get) Token: 0x060000E3 RID: 227 RVA: 0x0000727E File Offset: 0x0000547E
	int ICollection<!0>.Count
	{
		get
		{
			return this._items.Length;
		}
	}

	// Token: 0x17000045 RID: 69
	// (get) Token: 0x060000E4 RID: 228 RVA: 0x000072B7 File Offset: 0x000054B7
	bool ICollection<!0>.IsReadOnly
	{
		get
		{
			return true;
		}
	}

	// Token: 0x060000E5 RID: 229 RVA: 0x000072B0 File Offset: 0x000054B0
	void ICollection<!0>.Add(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000E6 RID: 230 RVA: 0x000072B0 File Offset: 0x000054B0
	void ICollection<!0>.Clear()
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000E7 RID: 231 RVA: 0x000072F1 File Offset: 0x000054F1
	bool ICollection<!0>.Contains(T item)
	{
		return this._items.Contains(item);
	}

	// Token: 0x060000E8 RID: 232 RVA: 0x000072FF File Offset: 0x000054FF
	void ICollection<!0>.CopyTo([Nullable(new byte[]
	{
		1,
		0
	})] T[] array, int arrayIndex)
	{
		this._items.CopyTo(array, arrayIndex);
	}

	// Token: 0x060000E9 RID: 233 RVA: 0x000072B0 File Offset: 0x000054B0
	bool ICollection<!0>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x17000046 RID: 70
	T IList<!0>.this[int index]
	{
		get
		{
			return this._items[index];
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	// Token: 0x060000EC RID: 236 RVA: 0x0000730E File Offset: 0x0000550E
	int IList<!0>.IndexOf(T item)
	{
		return this._items.IndexOf(item);
	}

	// Token: 0x060000ED RID: 237 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList<!0>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	// Token: 0x060000EE RID: 238 RVA: 0x000072B0 File Offset: 0x000054B0
	void IList<!0>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	// Token: 0x0400009B RID: 155
	[CompilerGenerated]
	private readonly T[] _items;
}
