using System;
using System.CodeDom.Compiler;
using System.Runtime.CompilerServices;

namespace ReactiveUI.SourceGenerators
{
	// Token: 0x02000006 RID: 6
	[NullableContext(2)]
	[Nullable(0)]
	[GeneratedCode("ReactiveUI.SourceGenerators.ReactiveCommandGenerator", "1.1.0.0")]
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
	internal sealed class ReactiveCommandAttribute : Attribute
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000D RID: 13 RVA: 0x000020AB File Offset: 0x000002AB
		// (set) Token: 0x0600000E RID: 14 RVA: 0x000020B3 File Offset: 0x000002B3
		public string CanExecute { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000F RID: 15 RVA: 0x000020BC File Offset: 0x000002BC
		// (set) Token: 0x06000010 RID: 16 RVA: 0x000020C4 File Offset: 0x000002C4
		public string OutputScheduler { get; set; }
	}
}
