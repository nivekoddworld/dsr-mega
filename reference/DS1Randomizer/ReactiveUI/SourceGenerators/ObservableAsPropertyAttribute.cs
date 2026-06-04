using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace ReactiveUI.SourceGenerators
{
	// Token: 0x02000005 RID: 5
	[NullableContext(2)]
	[Nullable(0)]
	[GeneratedCode("ReactiveUI.SourceGenerators.ObservableAsPropertyGenerator", "1.1.0.0")]
	[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	internal sealed class ObservableAsPropertyAttribute : Attribute
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000004 RID: 4 RVA: 0x00002058 File Offset: 0x00000258
		// (set) Token: 0x06000005 RID: 5 RVA: 0x00002060 File Offset: 0x00000260
		public string PropertyName { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000006 RID: 6 RVA: 0x00002069 File Offset: 0x00000269
		// (set) Token: 0x06000007 RID: 7 RVA: 0x00002071 File Offset: 0x00000271
		public bool ReadOnly { get; set; } = true;

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000008 RID: 8 RVA: 0x0000207A File Offset: 0x0000027A
		// (set) Token: 0x06000009 RID: 9 RVA: 0x00002082 File Offset: 0x00000282
		public bool UseProtected { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600000A RID: 10 RVA: 0x0000208B File Offset: 0x0000028B
		// (set) Token: 0x0600000B RID: 11 RVA: 0x00002093 File Offset: 0x00000293
		public string InitialValue { get; set; }
	}
}
