using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.XamlIl.Runtime;

namespace CompiledAvaloniaXaml
{
	// Token: 0x02000029 RID: 41
	[CompilerGenerated]
	internal class XamlIlContext
	{
		// Token: 0x0200002A RID: 42
		[CompilerGenerated]
		public class Context<TTarget> : IRootObjectProvider, IAvaloniaXamlIlParentStackProvider, ITypeDescriptorContext, IProvideValueTarget, IUriContext, IServiceProvider, IAvaloniaXamlIlEagerParentStackProvider
		{
			// Token: 0x17000054 RID: 84
			// (get) Token: 0x0600018C RID: 396 RVA: 0x00008AAC File Offset: 0x00006CAC
			object IRootObjectProvider.RootObject
			{
				get
				{
					if (this.RootObject != null)
					{
						return this.RootObject;
					}
					if (this._sp != null)
					{
						IRootObjectProvider rootObjectProvider = (IRootObjectProvider)this._sp.GetService(typeof(IRootObjectProvider));
						if (rootObjectProvider != null)
						{
							return rootObjectProvider.RootObject;
						}
					}
					return null;
				}
			}

			// Token: 0x17000055 RID: 85
			// (get) Token: 0x0600018D RID: 397 RVA: 0x00008B0C File Offset: 0x00006D0C
			object IRootObjectProvider.IntermediateRootObject
			{
				get
				{
					return this.IntermediateRoot;
				}
			}

			// Token: 0x17000056 RID: 86
			// (get) Token: 0x0600018E RID: 398 RVA: 0x00008B20 File Offset: 0x00006D20
			IEnumerable<object> IAvaloniaXamlIlParentStackProvider.Parents
			{
				get
				{
					return this._parentStackEnumerable;
				}
			}

			// Token: 0x17000057 RID: 87
			// (get) Token: 0x0600018F RID: 399 RVA: 0x00008B34 File Offset: 0x00006D34
			IContainer ITypeDescriptorContext.Container
			{
				get
				{
					return null;
				}
			}

			// Token: 0x17000058 RID: 88
			// (get) Token: 0x06000190 RID: 400 RVA: 0x00008B44 File Offset: 0x00006D44
			object ITypeDescriptorContext.Instance
			{
				get
				{
					return null;
				}
			}

			// Token: 0x17000059 RID: 89
			// (get) Token: 0x06000191 RID: 401 RVA: 0x00008B54 File Offset: 0x00006D54
			PropertyDescriptor ITypeDescriptorContext.PropertyDescriptor
			{
				get
				{
					return null;
				}
			}

			// Token: 0x06000192 RID: 402 RVA: 0x00008B64 File Offset: 0x00006D64
			bool ITypeDescriptorContext.OnComponentChanging()
			{
				throw new NotSupportedException();
			}

			// Token: 0x06000193 RID: 403 RVA: 0x00008B78 File Offset: 0x00006D78
			void ITypeDescriptorContext.OnComponentChanged()
			{
				throw new NotSupportedException();
			}

			// Token: 0x1700005A RID: 90
			// (get) Token: 0x06000194 RID: 404 RVA: 0x00008B8C File Offset: 0x00006D8C
			object IProvideValueTarget.TargetObject
			{
				get
				{
					return this.ProvideTargetObject;
				}
			}

			// Token: 0x1700005B RID: 91
			// (get) Token: 0x06000195 RID: 405 RVA: 0x00008BA0 File Offset: 0x00006DA0
			object IProvideValueTarget.TargetProperty
			{
				get
				{
					return this.ProvideTargetProperty;
				}
			}

			// Token: 0x1700005C RID: 92
			// (get) Token: 0x06000196 RID: 406 RVA: 0x00008BB4 File Offset: 0x00006DB4
			// (set) Token: 0x06000197 RID: 407 RVA: 0x00008BC8 File Offset: 0x00006DC8
			public virtual Uri BaseUri
			{
				get
				{
					return this._baseUri;
				}
				set
				{
					this._baseUri = value;
				}
			}

			// Token: 0x06000198 RID: 408 RVA: 0x00008BDC File Offset: 0x00006DDC
			public virtual object GetService(Type A_1)
			{
				if (this._innerSp != null)
				{
					object service = this._innerSp.GetService(A_1);
					if (service != null)
					{
						return service;
					}
				}
				if (typeof(IRootObjectProvider).Equals(A_1))
				{
					return this;
				}
				if (typeof(IAvaloniaXamlIlParentStackProvider).Equals(A_1))
				{
					return this;
				}
				if (typeof(ITypeDescriptorContext).Equals(A_1))
				{
					return this;
				}
				if (typeof(IProvideValueTarget).Equals(A_1))
				{
					return this;
				}
				if (typeof(IUriContext).Equals(A_1))
				{
					return this;
				}
				if (this._staticProviders != null)
				{
					for (int i = 0; i < this._staticProviders.Length; i++)
					{
						object obj = this._staticProviders[i];
						if (A_1.IsAssignableFrom(obj.GetType()))
						{
							return obj;
						}
					}
				}
				if (this._sp != null)
				{
					return this._sp.GetService(A_1);
				}
				return null;
			}

			// Token: 0x06000199 RID: 409 RVA: 0x00008CD4 File Offset: 0x00006ED4
			public Context(IServiceProvider A_1, object[] A_2, string A_3)
			{
				this._sp = A_1;
				this._staticProviders = A_2;
				if (A_3 != null)
				{
					this._baseUri = new Uri(A_3);
				}
				this.ParentsStack = new List<object>();
				this._parentStackEnumerable = new XamlIlContext.ParentStackEnumerable(this.ParentsStack, this._sp);
				this.AvaloniaNameScope = A_1.GetService(typeof(INameScope));
				this._innerSp = XamlIlRuntimeHelpers.CreateInnerServiceProviderV1(this);
			}

			// Token: 0x1700005D RID: 93
			// (get) Token: 0x0600019A RID: 410 RVA: 0x00008D4C File Offset: 0x00006F4C
			IReadOnlyList<object> IAvaloniaXamlIlEagerParentStackProvider.DirectParentsStack
			{
				get
				{
					return (IReadOnlyList<object>)this.ParentsStack;
				}
			}

			// Token: 0x1700005E RID: 94
			// (get) Token: 0x0600019B RID: 411 RVA: 0x00008D64 File Offset: 0x00006F64
			IAvaloniaXamlIlEagerParentStackProvider IAvaloniaXamlIlEagerParentStackProvider.ParentProvider
			{
				get
				{
					return this._sp.GetService(typeof(IAvaloniaXamlIlParentStackProvider)).AsEagerParentStackProvider();
				}
			}

			// Token: 0x0600019C RID: 412 RVA: 0x00008D8C File Offset: 0x00006F8C
			public void PushParent(object A_1)
			{
				this.ParentsStack.Add(A_1);
				this.ProvideTargetObject = A_1;
			}

			// Token: 0x0600019D RID: 413 RVA: 0x00008DAC File Offset: 0x00006FAC
			public void PopParent()
			{
				int num = this.ParentsStack.Count - 1;
				this.ParentsStack.RemoveAt(num);
				this.ProvideTargetObject = ((num == 0) ? null : this.ParentsStack[num - 1]);
			}

			// Token: 0x040000C9 RID: 201
			public TTarget RootObject;

			// Token: 0x040000CA RID: 202
			public object IntermediateRoot;

			// Token: 0x040000CB RID: 203
			private IServiceProvider _sp;

			// Token: 0x040000CC RID: 204
			private IServiceProvider _innerSp;

			// Token: 0x040000CD RID: 205
			private object[] _staticProviders;

			// Token: 0x040000CE RID: 206
			public List<object> ParentsStack;

			// Token: 0x040000CF RID: 207
			private IEnumerable<object> _parentStackEnumerable;

			// Token: 0x040000D0 RID: 208
			public object ProvideTargetObject;

			// Token: 0x040000D1 RID: 209
			public object ProvideTargetProperty;

			// Token: 0x040000D2 RID: 210
			private Uri _baseUri;

			// Token: 0x040000D3 RID: 211
			public INameScope AvaloniaNameScope;
		}

		// Token: 0x0200002B RID: 43
		[CompilerGenerated]
		private class ParentStackEnumerable : IEnumerable<object>
		{
			// Token: 0x0600019E RID: 414 RVA: 0x00008DF4 File Offset: 0x00006FF4
			public ParentStackEnumerable(List<object> A_1, IServiceProvider A_2)
			{
				this._parentList = A_1;
				this._parentSP = A_2;
			}

			// Token: 0x0600019F RID: 415 RVA: 0x00008E18 File Offset: 0x00007018
			public virtual IEnumerator<object> GetEnumerator()
			{
				return new XamlIlContext.ParentStackEnumerable.Enumerator(this._parentList, this._parentSP);
			}

			// Token: 0x060001A0 RID: 416 RVA: 0x00008E38 File Offset: 0x00007038
			IEnumerator IEnumerable.GetEnumerator()
			{
				return this.GetEnumerator();
			}

			// Token: 0x040000D4 RID: 212
			private List<object> _parentList;

			// Token: 0x040000D5 RID: 213
			private IServiceProvider _parentSP;

			// Token: 0x0200002C RID: 44
			[CompilerGenerated]
			public class Enumerator : IEnumerator<object>
			{
				// Token: 0x060001A1 RID: 417 RVA: 0x00008E4C File Offset: 0x0000704C
				public Enumerator(List<object> A_1, IServiceProvider A_2)
				{
					this._parentList = A_1;
					this._parentSP = A_2;
				}

				// Token: 0x1700005F RID: 95
				// (get) Token: 0x060001A2 RID: 418 RVA: 0x00008E70 File Offset: 0x00007070
				public virtual object Current
				{
					get
					{
						return this._current;
					}
				}

				// Token: 0x060001A3 RID: 419 RVA: 0x00008E84 File Offset: 0x00007084
				void IEnumerator.IEnumerator()
				{
					throw new NotSupportedException();
				}

				// Token: 0x060001A4 RID: 420 RVA: 0x00008E98 File Offset: 0x00007098
				void IDisposable.Dispose()
				{
					if (this._parentEnumerator != null)
					{
						this._parentEnumerator.Dispose();
					}
				}

				// Token: 0x060001A5 RID: 421 RVA: 0x00008EBC File Offset: 0x000070BC
				bool IEnumerator.IEnumerator()
				{
					if (this._state != 0)
					{
						if (this._state != 1)
						{
							if (this._state != 2)
							{
								return false;
							}
							goto IL_C8;
						}
					}
					else
					{
						this._list = this._parentList;
						this._listIndex = this._list.Count - 1;
						this._state = 1;
					}
					if (this._listIndex >= 0)
					{
						this._current = this._list[this._listIndex];
						this._listIndex--;
						return true;
					}
					IAvaloniaXamlIlParentStackProvider avaloniaXamlIlParentStackProvider;
					if (this._parentSP == null || (avaloniaXamlIlParentStackProvider = (IAvaloniaXamlIlParentStackProvider)this._parentSP.GetService(typeof(IAvaloniaXamlIlParentStackProvider))) == null)
					{
						goto IL_EB;
					}
					this._parentEnumerator = avaloniaXamlIlParentStackProvider.Parents.GetEnumerator();
					this._state = 2;
					IL_C8:
					if (this._parentEnumerator.MoveNext())
					{
						this._current = this._parentEnumerator.Current;
						return true;
					}
					IL_EB:
					this._state = 3;
					return false;
				}

				// Token: 0x040000D6 RID: 214
				private int _state;

				// Token: 0x040000D7 RID: 215
				private List<object> _parentList;

				// Token: 0x040000D8 RID: 216
				private IServiceProvider _parentSP;

				// Token: 0x040000D9 RID: 217
				private List<object> _list;

				// Token: 0x040000DA RID: 218
				private int _listIndex;

				// Token: 0x040000DB RID: 219
				private object _current;

				// Token: 0x040000DC RID: 220
				private IEnumerator<object> _parentEnumerator;
			}
		}
	}
}
