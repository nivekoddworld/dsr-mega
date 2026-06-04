using System;
using System.ComponentModel;
using DS1Randomizer;
using DS1Randomizer.Views;

namespace CompiledAvaloniaXaml
{
	// Token: 0x0200002D RID: 45
	[EditorBrowsable(EditorBrowsableState.Never)]
	public class !XamlLoader
	{
		// Token: 0x060001A6 RID: 422 RVA: 0x00008FBC File Offset: 0x000071BC
		public static object TryLoad(IServiceProvider A_0, string A_1)
		{
			if (string.Equals(A_1, "avares://DS1Randomizer/App.axaml", StringComparison.OrdinalIgnoreCase))
			{
				return new App();
			}
			if (string.Equals(A_1, "avares://DS1Randomizer/Views/MainWindow.axaml", StringComparison.OrdinalIgnoreCase))
			{
				return new MainWindow();
			}
			return null;
		}

		// Token: 0x060001A7 RID: 423 RVA: 0x00008FF7 File Offset: 0x000071F7
		public static object TryLoad(string A_0)
		{
			return !XamlLoader.TryLoad(null, A_0);
		}
	}
}
