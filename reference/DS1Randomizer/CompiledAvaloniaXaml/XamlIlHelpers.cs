using System;
using System.Reactive;
using System.Runtime.CompilerServices;
using Avalonia.Data.Core;
using DS1Randomizer.ViewModels;
using ReactiveUI;

namespace CompiledAvaloniaXaml
{
	// Token: 0x02000025 RID: 37
	[CompilerGenerated]
	internal class XamlIlHelpers
	{
		// Token: 0x06000115 RID: 277 RVA: 0x00007434 File Offset: 0x00005634
		private static object Title!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Title;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x0000744C File Offset: 0x0000564C
		private static void Title!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Title = (string)A_1;
		}

		// Token: 0x06000117 RID: 279 RVA: 0x0000746C File Offset: 0x0000566C
		public static IPropertyInfo Title!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Field = new ClrPropertyInfo("Title", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Title!Field;
		}

		// Token: 0x06000118 RID: 280 RVA: 0x000074C0 File Offset: 0x000056C0
		private static object UninstallCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).UninstallCommand;
		}

		// Token: 0x06000119 RID: 281 RVA: 0x000074D8 File Offset: 0x000056D8
		public static IPropertyInfo UninstallCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.UninstallCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.UninstallCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.UninstallCommand!Field = new ClrPropertyInfo("UninstallCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.UninstallCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.UninstallCommand!Field;
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00007520 File Offset: 0x00005720
		private static object CheckUpdatesCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).CheckUpdatesCommand;
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00007538 File Offset: 0x00005738
		public static IPropertyInfo CheckUpdatesCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.CheckUpdatesCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.CheckUpdatesCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.CheckUpdatesCommand!Field = new ClrPropertyInfo("CheckUpdatesCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.CheckUpdatesCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.CheckUpdatesCommand!Field;
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00007580 File Offset: 0x00005780
		private static object ResetOptionsCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).ResetOptionsCommand;
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00007598 File Offset: 0x00005798
		public static IPropertyInfo ResetOptionsCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ResetOptionsCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ResetOptionsCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ResetOptionsCommand!Field = new ClrPropertyInfo("ResetOptionsCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ResetOptionsCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ResetOptionsCommand!Field;
		}

		// Token: 0x0600011E RID: 286 RVA: 0x000075E0 File Offset: 0x000057E0
		private static object ToggleDarkModeCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).ToggleDarkModeCommand;
		}

		// Token: 0x0600011F RID: 287 RVA: 0x000075F8 File Offset: 0x000057F8
		public static IPropertyInfo ToggleDarkModeCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ToggleDarkModeCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ToggleDarkModeCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ToggleDarkModeCommand!Field = new ClrPropertyInfo("ToggleDarkModeCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ToggleDarkModeCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.ToggleDarkModeCommand!Field;
		}

		// Token: 0x06000120 RID: 288 RVA: 0x00007640 File Offset: 0x00005840
		private static object Simpleasylum!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Simpleasylum;
		}

		// Token: 0x06000121 RID: 289 RVA: 0x00007660 File Offset: 0x00005860
		private static void Simpleasylum!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Simpleasylum = (bool)A_1;
		}

		// Token: 0x06000122 RID: 290 RVA: 0x00007680 File Offset: 0x00005880
		public static IPropertyInfo Simpleasylum!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Field = new ClrPropertyInfo("Simpleasylum", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpleasylum!Field;
		}

		// Token: 0x06000123 RID: 291 RVA: 0x000076D4 File Offset: 0x000058D4
		private static object Simpletaurus!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Simpletaurus;
		}

		// Token: 0x06000124 RID: 292 RVA: 0x000076F4 File Offset: 0x000058F4
		private static void Simpletaurus!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Simpletaurus = (bool)A_1;
		}

		// Token: 0x06000125 RID: 293 RVA: 0x00007714 File Offset: 0x00005914
		public static IPropertyInfo Simpletaurus!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Field = new ClrPropertyInfo("Simpletaurus", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Simpletaurus!Field;
		}

		// Token: 0x06000126 RID: 294 RVA: 0x00007768 File Offset: 0x00005968
		private static object Scale_default!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Scale_default;
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00007788 File Offset: 0x00005988
		private static void Scale_default!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Scale_default = (bool)A_1;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x000077A8 File Offset: 0x000059A8
		public static IPropertyInfo Scale_default!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Field = new ClrPropertyInfo("Scale_default", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scale_default!Field;
		}

		// Token: 0x06000129 RID: 297 RVA: 0x000077FC File Offset: 0x000059FC
		private static object Noscale!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Noscale;
		}

		// Token: 0x0600012A RID: 298 RVA: 0x0000781C File Offset: 0x00005A1C
		private static void Noscale!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Noscale = (bool)A_1;
		}

		// Token: 0x0600012B RID: 299 RVA: 0x0000783C File Offset: 0x00005A3C
		public static IPropertyInfo Noscale!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Field = new ClrPropertyInfo("Noscale", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Noscale!Field;
		}

		// Token: 0x0600012C RID: 300 RVA: 0x00007890 File Offset: 0x00005A90
		private static object Scaleup!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Scaleup;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x000078B0 File Offset: 0x00005AB0
		private static void Scaleup!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Scaleup = (bool)A_1;
		}

		// Token: 0x0600012E RID: 302 RVA: 0x000078D0 File Offset: 0x00005AD0
		public static IPropertyInfo Scaleup!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Field = new ClrPropertyInfo("Scaleup", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Scaleup!Field;
		}

		// Token: 0x0600012F RID: 303 RVA: 0x00007924 File Offset: 0x00005B24
		private static object Earlyscale!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Earlyscale;
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00007944 File Offset: 0x00005B44
		private static void Earlyscale!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Earlyscale = (bool)A_1;
		}

		// Token: 0x06000131 RID: 305 RVA: 0x00007964 File Offset: 0x00005B64
		public static IPropertyInfo Earlyscale!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Field = new ClrPropertyInfo("Earlyscale", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Earlyscale!Field;
		}

		// Token: 0x06000132 RID: 306 RVA: 0x000079B8 File Offset: 0x00005BB8
		private static object Gravelord_default!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Gravelord_default;
		}

		// Token: 0x06000133 RID: 307 RVA: 0x000079D8 File Offset: 0x00005BD8
		private static void Gravelord_default!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Gravelord_default = (bool)A_1;
		}

		// Token: 0x06000134 RID: 308 RVA: 0x000079F8 File Offset: 0x00005BF8
		public static IPropertyInfo Gravelord_default!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Field = new ClrPropertyInfo("Gravelord_default", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Gravelord_default!Field;
		}

		// Token: 0x06000135 RID: 309 RVA: 0x00007A4C File Offset: 0x00005C4C
		private static object Permagravelord!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Permagravelord;
		}

		// Token: 0x06000136 RID: 310 RVA: 0x00007A6C File Offset: 0x00005C6C
		private static void Permagravelord!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Permagravelord = (bool)A_1;
		}

		// Token: 0x06000137 RID: 311 RVA: 0x00007A8C File Offset: 0x00005C8C
		public static IPropertyInfo Permagravelord!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Field = new ClrPropertyInfo("Permagravelord", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permagravelord!Field;
		}

		// Token: 0x06000138 RID: 312 RVA: 0x00007AE0 File Offset: 0x00005CE0
		private static object Nogravelord!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Nogravelord;
		}

		// Token: 0x06000139 RID: 313 RVA: 0x00007B00 File Offset: 0x00005D00
		private static void Nogravelord!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Nogravelord = (bool)A_1;
		}

		// Token: 0x0600013A RID: 314 RVA: 0x00007B20 File Offset: 0x00005D20
		public static IPropertyInfo Nogravelord!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Field = new ClrPropertyInfo("Nogravelord", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Nogravelord!Field;
		}

		// Token: 0x0600013B RID: 315 RVA: 0x00007B74 File Offset: 0x00005D74
		private static object Vagrant_default!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Vagrant_default;
		}

		// Token: 0x0600013C RID: 316 RVA: 0x00007B94 File Offset: 0x00005D94
		private static void Vagrant_default!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Vagrant_default = (bool)A_1;
		}

		// Token: 0x0600013D RID: 317 RVA: 0x00007BB4 File Offset: 0x00005DB4
		public static IPropertyInfo Vagrant_default!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Field = new ClrPropertyInfo("Vagrant_default", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Vagrant_default!Field;
		}

		// Token: 0x0600013E RID: 318 RVA: 0x00007C08 File Offset: 0x00005E08
		private static object Permavagrant!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Permavagrant;
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00007C28 File Offset: 0x00005E28
		private static void Permavagrant!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Permavagrant = (bool)A_1;
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00007C48 File Offset: 0x00005E48
		public static IPropertyInfo Permavagrant!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Field = new ClrPropertyInfo("Permavagrant", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Permavagrant!Field;
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00007C9C File Offset: 0x00005E9C
		private static object Limitvagrant!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Limitvagrant;
		}

		// Token: 0x06000142 RID: 322 RVA: 0x00007CBC File Offset: 0x00005EBC
		private static void Limitvagrant!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Limitvagrant = (bool)A_1;
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00007CDC File Offset: 0x00005EDC
		public static IPropertyInfo Limitvagrant!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Field = new ClrPropertyInfo("Limitvagrant", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Limitvagrant!Field;
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00007D30 File Offset: 0x00005F30
		private static object Editnames!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Editnames;
		}

		// Token: 0x06000145 RID: 325 RVA: 0x00007D50 File Offset: 0x00005F50
		private static void Editnames!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Editnames = (bool)A_1;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x00007D70 File Offset: 0x00005F70
		public static IPropertyInfo Editnames!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Field = new ClrPropertyInfo("Editnames", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Editnames!Field;
		}

		// Token: 0x06000147 RID: 327 RVA: 0x00007DC4 File Offset: 0x00005FC4
		private static object Antighost!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Antighost;
		}

		// Token: 0x06000148 RID: 328 RVA: 0x00007DE4 File Offset: 0x00005FE4
		private static void Antighost!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Antighost = (bool)A_1;
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00007E04 File Offset: 0x00006004
		public static IPropertyInfo Antighost!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Field = new ClrPropertyInfo("Antighost", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Antighost!Field;
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00007E58 File Offset: 0x00006058
		private static object Impolite!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Impolite;
		}

		// Token: 0x0600014B RID: 331 RVA: 0x00007E78 File Offset: 0x00006078
		private static void Impolite!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Impolite = (bool)A_1;
		}

		// Token: 0x0600014C RID: 332 RVA: 0x00007E98 File Offset: 0x00006098
		public static IPropertyInfo Impolite!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Field = new ClrPropertyInfo("Impolite", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Impolite!Field;
		}

		// Token: 0x0600014D RID: 333 RVA: 0x00007EEC File Offset: 0x000060EC
		private static object Crashfix!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Crashfix;
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00007F0C File Offset: 0x0000610C
		private static void Crashfix!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Crashfix = (bool)A_1;
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00007F2C File Offset: 0x0000612C
		public static IPropertyInfo Crashfix!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Field = new ClrPropertyInfo("Crashfix", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Crashfix!Field;
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00007F80 File Offset: 0x00006180
		private static object Mergegame!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Mergegame;
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00007FA0 File Offset: 0x000061A0
		private static void Mergegame!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Mergegame = (bool)A_1;
		}

		// Token: 0x06000152 RID: 338 RVA: 0x00007FC0 File Offset: 0x000061C0
		public static IPropertyInfo Mergegame!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Field = new ClrPropertyInfo("Mergegame", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mergegame!Field;
		}

		// Token: 0x06000153 RID: 339 RVA: 0x00008014 File Offset: 0x00006214
		private static object Loose!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Loose;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00008034 File Offset: 0x00006234
		private static void Loose!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Loose = (bool)A_1;
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00008054 File Offset: 0x00006254
		public static IPropertyInfo Loose!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Field = new ClrPropertyInfo("Loose", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Loose!Field;
		}

		// Token: 0x06000156 RID: 342 RVA: 0x000080A8 File Offset: 0x000062A8
		private static object Exe!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Exe;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x000080C0 File Offset: 0x000062C0
		private static void Exe!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Exe = (string)A_1;
		}

		// Token: 0x06000158 RID: 344 RVA: 0x000080E0 File Offset: 0x000062E0
		public static IPropertyInfo Exe!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Field = new ClrPropertyInfo("Exe", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Exe!Field;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00008134 File Offset: 0x00006334
		private static object SelectExeCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).SelectExeCommand;
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000814C File Offset: 0x0000634C
		public static IPropertyInfo SelectExeCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.SelectExeCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.SelectExeCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.SelectExeCommand!Field = new ClrPropertyInfo("SelectExeCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.SelectExeCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.SelectExeCommand!Field;
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00008194 File Offset: 0x00006394
		private static object Mod!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Mod;
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000081AC File Offset: 0x000063AC
		private static void Mod!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Mod = (string)A_1;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000081CC File Offset: 0x000063CC
		public static IPropertyInfo Mod!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Field = new ClrPropertyInfo("Mod", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Mod!Field;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00008220 File Offset: 0x00006420
		private static object MergeModsCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).MergeModsCommand;
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00008238 File Offset: 0x00006438
		public static IPropertyInfo MergeModsCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MergeModsCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MergeModsCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MergeModsCommand!Field = new ClrPropertyInfo("MergeModsCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MergeModsCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MergeModsCommand!Field;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00008280 File Offset: 0x00006480
		private static object DllStr!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).DllStr;
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00008298 File Offset: 0x00006498
		private static void DllStr!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).DllStr = (string)A_1;
		}

		// Token: 0x06000162 RID: 354 RVA: 0x000082B8 File Offset: 0x000064B8
		public static IPropertyInfo DllStr!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Field = new ClrPropertyInfo("DllStr", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.DllStr!Field;
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000830C File Offset: 0x0000650C
		private static object AddDllCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).AddDllCommand;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00008324 File Offset: 0x00006524
		public static IPropertyInfo AddDllCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.AddDllCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.AddDllCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.AddDllCommand!Field = new ClrPropertyInfo("AddDllCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.AddDllCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.AddDllCommand!Field;
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000836C File Offset: 0x0000656C
		private static object Message!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Message;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00008384 File Offset: 0x00006584
		private static void Message!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Message = (string)A_1;
		}

		// Token: 0x06000167 RID: 359 RVA: 0x000083A4 File Offset: 0x000065A4
		public static IPropertyInfo Message!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Field = new ClrPropertyInfo("Message", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Message!Field;
		}

		// Token: 0x06000168 RID: 360 RVA: 0x000083F8 File Offset: 0x000065F8
		private static object MessageError!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).MessageError;
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00008418 File Offset: 0x00006618
		private static void MessageError!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).MessageError = (bool)A_1;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00008438 File Offset: 0x00006638
		public static IPropertyInfo MessageError!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Field = new ClrPropertyInfo("MessageError", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MessageError!Field;
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000848C File Offset: 0x0000668C
		private static object Seed!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Seed;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x000084A4 File Offset: 0x000066A4
		private static void Seed!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Seed = (string)A_1;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x000084C4 File Offset: 0x000066C4
		public static IPropertyInfo Seed!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Field = new ClrPropertyInfo("Seed", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Seed!Field;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00008518 File Offset: 0x00006718
		private static object RerollSeed!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).RerollSeed;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00008538 File Offset: 0x00006738
		private static void RerollSeed!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).RerollSeed = (bool)A_1;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00008558 File Offset: 0x00006758
		public static IPropertyInfo RerollSeed!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Field = new ClrPropertyInfo("RerollSeed", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RerollSeed!Field;
		}

		// Token: 0x06000171 RID: 369 RVA: 0x000085AC File Offset: 0x000067AC
		private static object MustRerollSeed!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).MustRerollSeed;
		}

		// Token: 0x06000172 RID: 370 RVA: 0x000085CC File Offset: 0x000067CC
		private static void MustRerollSeed!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).MustRerollSeed = (bool)A_1;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x000085EC File Offset: 0x000067EC
		public static IPropertyInfo MustRerollSeed!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Field = new ClrPropertyInfo("MustRerollSeed", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.MustRerollSeed!Field;
		}

		// Token: 0x06000174 RID: 372 RVA: 0x00008640 File Offset: 0x00006840
		private static object RandomizeText!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).RandomizeText;
		}

		// Token: 0x06000175 RID: 373 RVA: 0x00008658 File Offset: 0x00006858
		private static void RandomizeText!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).RandomizeText = (string)A_1;
		}

		// Token: 0x06000176 RID: 374 RVA: 0x00008678 File Offset: 0x00006878
		public static IPropertyInfo RandomizeText!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Field = new ClrPropertyInfo("RandomizeText", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeText!Field;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x000086CC File Offset: 0x000068CC
		private static object RandomizeCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).RandomizeCommand;
		}

		// Token: 0x06000178 RID: 376 RVA: 0x000086E4 File Offset: 0x000068E4
		public static IPropertyInfo RandomizeCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeCommand!Field = new ClrPropertyInfo("RandomizeCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.RandomizeCommand!Field;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000872C File Offset: 0x0000692C
		private static object Working!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Working;
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000874C File Offset: 0x0000694C
		private static void Working!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Working = (bool)A_1;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000876C File Offset: 0x0000696C
		public static IPropertyInfo Working!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Field = new ClrPropertyInfo("Working", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Working!Field;
		}

		// Token: 0x0600017C RID: 380 RVA: 0x000087C0 File Offset: 0x000069C0
		private static object LaunchGameCommand!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).LaunchGameCommand;
		}

		// Token: 0x0600017D RID: 381 RVA: 0x000087D8 File Offset: 0x000069D8
		public static IPropertyInfo LaunchGameCommand!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.LaunchGameCommand!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.LaunchGameCommand!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.LaunchGameCommand!Field = new ClrPropertyInfo("LaunchGameCommand", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.LaunchGameCommand!Getter), null, typeof(ReactiveCommand<Unit, Unit>));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.LaunchGameCommand!Field;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x00008820 File Offset: 0x00006A20
		private static object Launching!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Launching;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x00008840 File Offset: 0x00006A40
		private static void Launching!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Launching = (bool)A_1;
		}

		// Token: 0x06000180 RID: 384 RVA: 0x00008860 File Offset: 0x00006A60
		public static IPropertyInfo Launching!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Field = new ClrPropertyInfo("Launching", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Launching!Field;
		}

		// Token: 0x06000181 RID: 385 RVA: 0x000088B4 File Offset: 0x00006AB4
		private static object StatusFailure!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).StatusFailure;
		}

		// Token: 0x06000182 RID: 386 RVA: 0x000088D4 File Offset: 0x00006AD4
		private static void StatusFailure!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).StatusFailure = (bool)A_1;
		}

		// Token: 0x06000183 RID: 387 RVA: 0x000088F4 File Offset: 0x00006AF4
		public static IPropertyInfo StatusFailure!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Field = new ClrPropertyInfo("StatusFailure", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusFailure!Field;
		}

		// Token: 0x06000184 RID: 388 RVA: 0x00008948 File Offset: 0x00006B48
		private static object StatusSuccess!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).StatusSuccess;
		}

		// Token: 0x06000185 RID: 389 RVA: 0x00008968 File Offset: 0x00006B68
		private static void StatusSuccess!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).StatusSuccess = (bool)A_1;
		}

		// Token: 0x06000186 RID: 390 RVA: 0x00008988 File Offset: 0x00006B88
		public static IPropertyInfo StatusSuccess!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Field = new ClrPropertyInfo("StatusSuccess", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Setter), typeof(bool));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.StatusSuccess!Field;
		}

		// Token: 0x06000187 RID: 391 RVA: 0x000089DC File Offset: 0x00006BDC
		private static object Status!Getter(object A_0)
		{
			return ((MainWindowViewModel)A_0).Status;
		}

		// Token: 0x06000188 RID: 392 RVA: 0x000089F4 File Offset: 0x00006BF4
		private static void Status!Setter(object A_0, object A_1)
		{
			((MainWindowViewModel)A_0).Status = (string)A_1;
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00008A14 File Offset: 0x00006C14
		public static IPropertyInfo Status!Property()
		{
			if (XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Field != null)
			{
				return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Field;
			}
			XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Field = new ClrPropertyInfo("Status", new Func<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Getter), new Action<object, object>(XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Setter), typeof(string));
			return XamlIlHelpers.DS1Randomizer.ViewModels.MainWindowViewModel,DS1Randomizer.Status!Field;
		}

		// Token: 0x0400009F RID: 159
		private static IPropertyInfo Title!Field;

		// Token: 0x040000A0 RID: 160
		private static IPropertyInfo UninstallCommand!Field;

		// Token: 0x040000A1 RID: 161
		private static IPropertyInfo CheckUpdatesCommand!Field;

		// Token: 0x040000A2 RID: 162
		private static IPropertyInfo ResetOptionsCommand!Field;

		// Token: 0x040000A3 RID: 163
		private static IPropertyInfo ToggleDarkModeCommand!Field;

		// Token: 0x040000A4 RID: 164
		private static IPropertyInfo Simpleasylum!Field;

		// Token: 0x040000A5 RID: 165
		private static IPropertyInfo Simpletaurus!Field;

		// Token: 0x040000A6 RID: 166
		private static IPropertyInfo Scale_default!Field;

		// Token: 0x040000A7 RID: 167
		private static IPropertyInfo Noscale!Field;

		// Token: 0x040000A8 RID: 168
		private static IPropertyInfo Scaleup!Field;

		// Token: 0x040000A9 RID: 169
		private static IPropertyInfo Earlyscale!Field;

		// Token: 0x040000AA RID: 170
		private static IPropertyInfo Gravelord_default!Field;

		// Token: 0x040000AB RID: 171
		private static IPropertyInfo Permagravelord!Field;

		// Token: 0x040000AC RID: 172
		private static IPropertyInfo Nogravelord!Field;

		// Token: 0x040000AD RID: 173
		private static IPropertyInfo Vagrant_default!Field;

		// Token: 0x040000AE RID: 174
		private static IPropertyInfo Permavagrant!Field;

		// Token: 0x040000AF RID: 175
		private static IPropertyInfo Limitvagrant!Field;

		// Token: 0x040000B0 RID: 176
		private static IPropertyInfo Editnames!Field;

		// Token: 0x040000B1 RID: 177
		private static IPropertyInfo Antighost!Field;

		// Token: 0x040000B2 RID: 178
		private static IPropertyInfo Impolite!Field;

		// Token: 0x040000B3 RID: 179
		private static IPropertyInfo Crashfix!Field;

		// Token: 0x040000B4 RID: 180
		private static IPropertyInfo Mergegame!Field;

		// Token: 0x040000B5 RID: 181
		private static IPropertyInfo Loose!Field;

		// Token: 0x040000B6 RID: 182
		private static IPropertyInfo Exe!Field;

		// Token: 0x040000B7 RID: 183
		private static IPropertyInfo SelectExeCommand!Field;

		// Token: 0x040000B8 RID: 184
		private static IPropertyInfo Mod!Field;

		// Token: 0x040000B9 RID: 185
		private static IPropertyInfo MergeModsCommand!Field;

		// Token: 0x040000BA RID: 186
		private static IPropertyInfo DllStr!Field;

		// Token: 0x040000BB RID: 187
		private static IPropertyInfo AddDllCommand!Field;

		// Token: 0x040000BC RID: 188
		private static IPropertyInfo Message!Field;

		// Token: 0x040000BD RID: 189
		private static IPropertyInfo MessageError!Field;

		// Token: 0x040000BE RID: 190
		private static IPropertyInfo Seed!Field;

		// Token: 0x040000BF RID: 191
		private static IPropertyInfo RerollSeed!Field;

		// Token: 0x040000C0 RID: 192
		private static IPropertyInfo MustRerollSeed!Field;

		// Token: 0x040000C1 RID: 193
		private static IPropertyInfo RandomizeText!Field;

		// Token: 0x040000C2 RID: 194
		private static IPropertyInfo RandomizeCommand!Field;

		// Token: 0x040000C3 RID: 195
		private static IPropertyInfo Working!Field;

		// Token: 0x040000C4 RID: 196
		private static IPropertyInfo LaunchGameCommand!Field;

		// Token: 0x040000C5 RID: 197
		private static IPropertyInfo Launching!Field;

		// Token: 0x040000C6 RID: 198
		private static IPropertyInfo StatusFailure!Field;

		// Token: 0x040000C7 RID: 199
		private static IPropertyInfo StatusSuccess!Field;

		// Token: 0x040000C8 RID: 200
		private static IPropertyInfo Status!Field;
	}
}
