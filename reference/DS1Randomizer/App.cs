using System;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml.XamlIl.Runtime;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;
using CompiledAvaloniaXaml;
using DS1Randomizer.ViewModels;
using DS1Randomizer.Views;

namespace DS1Randomizer
{
	// Token: 0x0200000C RID: 12
	[CompilerGenerated]
	public class App : Application
	{
		// Token: 0x0600001B RID: 27 RVA: 0x00002100 File Offset: 0x00000300
		public override void Initialize()
		{
			App.!XamlIlPopulateTrampoline(this);
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002108 File Offset: 0x00000308
		public override void OnFrameworkInitializationCompleted()
		{
			IClassicDesktopStyleApplicationLifetime classicDesktopStyleApplicationLifetime = base.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
			if (classicDesktopStyleApplicationLifetime != null)
			{
				classicDesktopStyleApplicationLifetime.MainWindow = new MainWindow
				{
					DataContext = new MainWindowViewModel()
				};
			}
			base.OnFrameworkInitializationCompleted();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002148 File Offset: 0x00000348
		private static void !XamlIlPopulate(IServiceProvider A_0, App A_1)
		{
			XamlIlContext.Context<App> context = new XamlIlContext.Context<App>(A_0, new object[]
			{
				(object)CompiledAvaloniaXaml.!AvaloniaResources.NamespaceInfo:/App.axaml.Singleton
			}, "avares://DS1Randomizer/App.axaml");
			context.RootObject = A_1;
			context.IntermediateRoot = A_1;
			context.PushParent(A_1);
			A_1.RequestedThemeVariant = ThemeVariant.Default;
			A_1.DataTemplates.Add(new ViewLocator());
			A_1.Styles.Add(new FluentTheme(context));
			context.PopParent();
			StyledElement styled;
			if ((styled = (A_1 as StyledElement)) != null)
			{
				NameScope.SetNameScope(styled, context.AvaloniaNameScope);
			}
			context.AvaloniaNameScope.Complete();
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000021E5 File Offset: 0x000003E5
		private static void !XamlIlPopulateTrampoline(App A_0)
		{
			if (App.!XamlIlPopulateOverride != null)
			{
				App.!XamlIlPopulateOverride(A_0);
				return;
			}
			App.!XamlIlPopulate(XamlIlRuntimeHelpers.CreateRootServiceProviderV3(null), A_0);
		}

		// Token: 0x04000017 RID: 23
		private static Action<object> !XamlIlPopulateOverride;
	}
}
