using System;
using System.CodeDom.Compiler;

namespace ReactiveUI.SourceGenerators
{
	// Token: 0x02000009 RID: 9
	[GeneratedCode("ReactiveUI.SourceGenerators.ReactiveGenerator", "2.1.0.0")]
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
	internal sealed class ReactiveAttribute : Attribute
	{
		// Token: 0x17000007 RID: 7
		// (get) Token: 0x06000012 RID: 18 RVA: 0x000020CD File Offset: 0x000002CD
		// (set) Token: 0x06000013 RID: 19 RVA: 0x000020D5 File Offset: 0x000002D5
		public AccessModifier SetModifier { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000014 RID: 20 RVA: 0x000020DE File Offset: 0x000002DE
		// (set) Token: 0x06000015 RID: 21 RVA: 0x000020E6 File Offset: 0x000002E6
		public InheritanceModifier Inheritance { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000016 RID: 22 RVA: 0x000020EF File Offset: 0x000002EF
		// (set) Token: 0x06000017 RID: 23 RVA: 0x000020F7 File Offset: 0x000002F7
		public bool UseRequired { get; set; }
	}
}
