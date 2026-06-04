using System;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using RandomizerCommon.ViewModels;

namespace DS1Randomizer
{
	// Token: 0x0200000E RID: 14
	public class ViewLocator : IDataTemplate, ITemplate<object, Control>
	{
		// Token: 0x06000026 RID: 38 RVA: 0x00002244 File Offset: 0x00000444
		[NullableContext(2)]
		public Control Build(object param)
		{
			if (param == null)
			{
				return null;
			}
			string text = param.GetType().FullName.Replace("ViewModel", "View", StringComparison.Ordinal);
			Type type = Type.GetType(text);
			if (type != null)
			{
				return (Control)Activator.CreateInstance(type);
			}
			return new TextBlock
			{
				Text = "Not Found: " + text
			};
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000022A4 File Offset: 0x000004A4
		[NullableContext(2)]
		public bool Match(object data)
		{
			return data is ViewModelBase;
		}
	}
}
