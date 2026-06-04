using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Logging;
using Avalonia.ReactiveUI;

namespace DS1Randomizer
{
	// Token: 0x0200000D RID: 13
	[NullableContext(1)]
	[Nullable(0)]
	internal sealed class Program
	{
		// Token: 0x06000020 RID: 32
		[DllImport("kernel32.dll")]
		private static extern bool AttachConsole(int dwProcessId);

		// Token: 0x06000021 RID: 33
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool AllocConsole();

		// Token: 0x06000022 RID: 34
		[DllImport("kernel32")]
		private static extern bool FreeConsole();

		// Token: 0x06000023 RID: 35 RVA: 0x0000220A File Offset: 0x0000040A
		[STAThread]
		public static void Main(string[] args)
		{
			Program.BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, null);
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002219 File Offset: 0x00000419
		public static AppBuilder BuildAvaloniaApp()
		{
			return AppBuilder.Configure<App>().UsePlatformDetect().WithInterFont().LogToTrace(LogEventLevel.Warning, Array.Empty<string>()).UseReactiveUI();
		}
	}
}
