using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.MarkupExtensions;

namespace CompiledAvaloniaXaml
{
	// Token: 0x02000028 RID: 40
	[CompilerGenerated]
	internal class XamlDynamicSetters
	{
		// Token: 0x0600018B RID: 395 RVA: 0x00008A80 File Offset: 0x00006C80
		public static void <>XamlDynamicSetter_1(ContentControl A_0, CompiledBindingExtension A_1)
		{
			if (A_1 != null)
			{
				A_0.Bind(ContentControl.ContentProperty, A_1, null);
				return;
			}
			A_0.Content = A_1;
		}
	}
}
