using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using RandomizerCommon;
using RandomizerCommon.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using SoulsIds;

namespace DS1Randomizer.ViewModels
{
	// Token: 0x02000013 RID: 19
	[NullableContext(1)]
	[Nullable(0)]
	public class MainWindowViewModel : ViewModelBase
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000039 RID: 57 RVA: 0x00004802 File Offset: 0x00002A02
		// (set) Token: 0x0600003A RID: 58 RVA: 0x0000480A File Offset: 0x00002A0A
		private uint RunSeed { get; set; }

		// Token: 0x1700000B RID: 11
		// (get) Token: 0x0600003B RID: 59 RVA: 0x00004813 File Offset: 0x00002A13
		[Nullable(new byte[]
		{
			1,
			1,
			2
		})]
		public Interaction<OptionsViewModel, OptionsViewModel.Result> ShowOptionsDialog { [return: Nullable(new byte[]
		{
			1,
			1,
			2
		})] get; } = new Interaction<OptionsViewModel, OptionsViewModel.Result>(null);

		// Token: 0x1700000C RID: 12
		// (get) Token: 0x0600003C RID: 60 RVA: 0x0000481B File Offset: 0x00002A1B
		[Nullable(new byte[]
		{
			1,
			1,
			2
		})]
		public Interaction<MergeModViewModel, string> ShowMergeModDialog { [return: Nullable(new byte[]
		{
			1,
			1,
			2
		})] get; } = new Interaction<MergeModViewModel, string>(null);

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x0600003D RID: 61 RVA: 0x00004823 File Offset: 0x00002A23
		[Nullable(new byte[]
		{
			1,
			1,
			2,
			1
		})]
		public Interaction<DllViewModel, ImmutableList<string>> ShowDllDialog { [return: Nullable(new byte[]
		{
			1,
			1,
			2,
			1
		})] get; } = new Interaction<DllViewModel, ImmutableList<string>>(null);

		// Token: 0x0600003E RID: 62 RVA: 0x0000482C File Offset: 0x00002A2C
		public MainWindowViewModel()
		{
			this.messages = new Messages("dist1", false);
			this.runner = new ModRunner(GameSpec.FromGame.DS1R, "config_randomizer.toml", "dist1\\ModEngine\\modengine2_launcher.exe", null);
			this.helperPath = new FileInfo("dist1\\DLL\\DS1HeapPatch.dll").FullName;
			string text = RandomizerOptions.ReadOptions();
			if (string.IsNullOrEmpty(text))
			{
				text = this.DefaultOpts;
			}
			this.SetOptionsFromString(text, false);
			string text2 = RandomizerOptions.ReadExe();
			if (string.IsNullOrEmpty(text2) || !MainWindowViewModel.IsValidExe(text2))
			{
				text2 = null;
			}
			string exe2;
			if ((exe2 = text2) == null)
			{
				exe2 = (MainWindowViewModel.GetHeuristicExe() ?? "");
			}
			this.Exe = exe2;
			List<string> list = RandomizerOptions.ReadExtraDlls();
			if (list != null)
			{
				this.Dlls = ImmutableList.CreateRange<string>(list);
			}
			this.SyncDllOptions(false);
			this.PrevMod = (this.Mod = (RandomizerOptions.ReadMod() ?? ""));
			this.launchEnabled = this.WhenAnyValue((MainWindowViewModel x) => x.CanLaunch, (bool c) => c);
			IObservable<ValueTuple<bool, bool, bool, bool, bool, bool>> first = this.WhenAnyValue((MainWindowViewModel x) => x.Simpleasylum, (MainWindowViewModel x) => x.Simpletaurus, (MainWindowViewModel x) => x.Scaleup, (MainWindowViewModel x) => x.Antighost, (MainWindowViewModel x) => x.Impolite, (MainWindowViewModel x) => x.Editnames);
			IObservable<ValueTuple<bool, bool, bool, bool, bool>> second = this.WhenAnyValue((MainWindowViewModel x) => x.Permagravelord, (MainWindowViewModel x) => x.Permavagrant, (MainWindowViewModel x) => x.Crashfix, (MainWindowViewModel x) => x.Mergegame, (MainWindowViewModel x) => x.Loose);
			IObservable<ValueTuple<bool, bool, bool, bool, bool>> second2 = this.WhenAnyValue((MainWindowViewModel x) => x.Nogravelord, (MainWindowViewModel x) => x.Noscale, (MainWindowViewModel x) => x.Scaleup, (MainWindowViewModel x) => x.Earlyscale, (MainWindowViewModel x) => x.Limitvagrant);
			(from _ in first.CombineLatest(second).CombineLatest(second2)
			select this.MakeOptions()).Subscribe(delegate(RandomizerOptions opt)
			{
				this.Options = opt;
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Options).Subscribe(delegate(RandomizerOptions _)
			{
				string text3 = "Created by thefifthmatt";
				if (this.Options != null)
				{
					text3 = text3 + ". Current config hash: " + this.Options.ConfigHash();
				}
				this.SetStatus(text3, false, false);
				string error;
				this.DoBasicCheck(out error);
				this.SetError(error);
				RandomizerOptions.SaveOptions(this.OptionsWithSeed());
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Seed).Subscribe(delegate(string _)
			{
				uint num;
				bool flag = uint.TryParse(this.Seed, out num) && num > 0U;
				this.MustRerollSeed = (this.RerollSeed = !flag);
				string error;
				this.DoBasicCheck(out error);
				this.SetError(error);
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.RerollSeed, (MainWindowViewModel x) => x.Working).Subscribe(delegate(ValueTuple<bool, bool> _)
			{
				this.RandomizeText = (this.Working ? "Running..." : (this.RerollSeed ? "Randomize!" : "Run with set seed"));
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Crashfix).Subscribe(delegate(bool _)
			{
				ImmutableList<string> dlls = this.Dlls;
				this.SyncDllOptions(false);
				if (!this.Dlls.SequenceEqual(dlls))
				{
					ModRunner modRunner = this.runner;
					if (modRunner != null)
					{
						modRunner.DeleteLaunchFile();
					}
				}
				this.UpdateCanLaunch();
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Dlls).Subscribe(delegate(ImmutableList<string> _)
			{
				string dllStr;
				if (this.Dlls.Count != 0)
				{
					string str = "Using ";
					string separator = ", ";
					IEnumerable<string> dlls = this.Dlls;
					Func<string, string> selector;
					if ((selector = MainWindowViewModel.<>O.<0>__GetFileName) == null)
					{
						selector = (MainWindowViewModel.<>O.<0>__GetFileName = new Func<string, string>(Path.GetFileName));
					}
					dllStr = str + string.Join(separator, dlls.Select(selector).Distinct<string>());
				}
				else
				{
					dllStr = "";
				}
				this.DllStr = dllStr;
				RandomizerOptions.SaveExtraDlls(this.Dlls);
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Exe).Subscribe(delegate(string exe)
			{
				if (MainWindowViewModel.IsValidExe(exe))
				{
					RandomizerOptions.SaveExe(exe);
				}
			});
			this.WhenAnyValue((MainWindowViewModel x) => x.Mod).Subscribe(delegate(string mod)
			{
				if (mod == "" || File.Exists(mod) || Directory.Exists(mod))
				{
					RandomizerOptions.SaveMod(mod);
				}
				if (mod != this.PrevMod)
				{
					this.PrevMod = mod;
					ModRunner modRunner = this.runner;
					if (modRunner != null)
					{
						modRunner.DeleteLaunchFile();
					}
					this.UpdateCanLaunch();
				}
			});
			Observable.FromEventPattern(this.runner, "StartRunning").Subscribe(delegate(EventPattern<object> _)
			{
				this.Launching = true;
			});
			Observable.FromEventPattern(this.runner, "DoneRunning").Subscribe(delegate(EventPattern<object> _)
			{
				this.Launching = false;
			});
			Observable.FromEventPattern(this.runner, "FailedToStart").Subscribe(delegate(EventPattern<object> _)
			{
				MainWindowViewModel.<<-ctor>b__57_37>d <<-ctor>b__57_37>d;
				<<-ctor>b__57_37>d.<>t__builder = AsyncVoidMethodBuilder.Create();
				<<-ctor>b__57_37>d.<>4__this = this;
				<<-ctor>b__57_37>d.<>1__state = -1;
				<<-ctor>b__57_37>d.<>t__builder.Start<MainWindowViewModel.<<-ctor>b__57_37>d>(ref <<-ctor>b__57_37>d);
			});
			this.UpdateCanLaunch();
		}

		// Token: 0x0600003F RID: 63 RVA: 0x0000513C File Offset: 0x0000333C
		private bool DoBasicCheck(out string err)
		{
			if (!MiscSetup.CheckRequiredDS1Files(this.messages, out err))
			{
				return false;
			}
			if (this.Loose && this.Mergegame)
			{
				err = "Error: Randomizer can't currently merge from the game directory and also output to the game directory. You must disable game directory output and use the built-in launcher to merge other mods.";
			}
			else
			{
				if (!this.Loose && MainWindowViewModel.IsValidExe(this.Exe))
				{
					DirectoryInfo directory = new FileInfo(this.Exe).Directory;
					if (((directory != null) ? directory.FullName : null) == Directory.GetCurrentDirectory())
					{
						err = "Error: Do not run randomizer from the game directory, unless the option is explicitly selected to output to the game directory. Randomizer should be installed in a separate directory.";
						goto IL_8D;
					}
				}
				if (!string.IsNullOrWhiteSpace(this.Seed) && this.MustRerollSeed)
				{
					err = "Warning: Seeds must be positive integers, or else they will be randomly rerolled";
					return true;
				}
			}
			IL_8D:
			return err == null;
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000051DC File Offset: 0x000033DC
		private void SetOptionsFromString(string optStr, bool clearEmptySeed = false)
		{
			RandomizerOptions randomizerOptions = RandomizerOptions.Parse(optStr.Split(' ', StringSplitOptions.None), GameSpec.FromGame.DS1R, null);
			if (randomizerOptions.StoredVersion < 2)
			{
				randomizerOptions["earlyscale"] = true;
			}
			if (randomizerOptions.Seed != 0U)
			{
				this.RunSeed = randomizerOptions.Seed;
				this.Seed = this.RunSeed.ToString();
			}
			else if (clearEmptySeed)
			{
				this.RunSeed = 0U;
				this.Seed = "";
			}
			this.SetOptions(randomizerOptions);
		}

		// Token: 0x06000041 RID: 65 RVA: 0x00005258 File Offset: 0x00003458
		private void SetOptions(RandomizerOptions opt)
		{
			this.Simpleasylum = opt["simpleasylum"];
			this.Simpletaurus = opt["simpletaurus"];
			this.Earlyscale = opt["earlyscale"];
			this.Noscale = opt["noscale"];
			this.Scaleup = opt["scaleup"];
			this.Scale_default = (!opt["scaleup"] && !opt["noscale"]);
			this.Antighost = opt["antighost"];
			this.Impolite = opt["impolite"];
			this.Editnames = opt["editnames"];
			this.Nogravelord = opt["nogravelord"];
			this.Permagravelord = opt["permagravelord"];
			this.Gravelord_default = (!opt["permagravelord"] && !opt["nogravelord"]);
			this.Vagrant_default = !opt["permavagrant"];
			this.Permavagrant = opt["permavagrant"];
			this.Limitvagrant = opt["limitvagrant"];
			this.Crashfix = opt["crashfix"];
			this.Mergegame = opt["mergegame"];
			this.Loose = opt["loose"];
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000053C0 File Offset: 0x000035C0
		private RandomizerOptions MakeOptions()
		{
			RandomizerOptions randomizerOptions = new RandomizerOptions(GameSpec.FromGame.DS1R);
			randomizerOptions["simpleasylum"] = this.Simpleasylum;
			randomizerOptions["simpletaurus"] = this.Simpletaurus;
			randomizerOptions["earlyscale"] = this.Earlyscale;
			randomizerOptions["noscale"] = this.Noscale;
			randomizerOptions["scaleup"] = this.Scaleup;
			randomizerOptions["antighost"] = this.Antighost;
			randomizerOptions["impolite"] = this.Impolite;
			randomizerOptions["editnames"] = this.Editnames;
			randomizerOptions["nogravelord"] = this.Nogravelord;
			randomizerOptions["permagravelord"] = this.Permagravelord;
			randomizerOptions["permavagrant"] = this.Permavagrant;
			randomizerOptions["limitvagrant"] = this.Limitvagrant;
			randomizerOptions["crashfix"] = this.Crashfix;
			randomizerOptions["mergegame"] = this.Mergegame;
			randomizerOptions["loose"] = this.Loose;
			return randomizerOptions;
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000054D2 File Offset: 0x000036D2
		private RandomizerOptions OptionsWithSeed()
		{
			RandomizerOptions randomizerOptions = this.Options.Copy();
			randomizerOptions.Seed = this.RunSeed;
			return randomizerOptions;
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000054EC File Offset: 0x000036EC
		[ReactiveCommand]
		public void Randomize()
		{
			MainWindowViewModel.<Randomize>d__63 <Randomize>d__;
			<Randomize>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<Randomize>d__.<>4__this = this;
			<Randomize>d__.<>1__state = -1;
			<Randomize>d__.<>t__builder.Start<MainWindowViewModel.<Randomize>d__63>(ref <Randomize>d__);
		}

		// Token: 0x06000045 RID: 69 RVA: 0x00005523 File Offset: 0x00003723
		private void SetStatus(string status, bool success = false, bool failure = false)
		{
			this.Status = status;
			this.StatusSuccess = success;
			this.StatusFailure = failure;
		}

		// Token: 0x06000046 RID: 70 RVA: 0x0000553A File Offset: 0x0000373A
		[NullableContext(2)]
		private void SetError(string err = null)
		{
			this.Message = (err ?? "");
			this.MessageError = (err != null);
		}

		// Token: 0x06000047 RID: 71 RVA: 0x00005558 File Offset: 0x00003758
		[ReactiveCommand(CanExecute = "launchEnabled")]
		public void LaunchGame()
		{
			MainWindowViewModel.<LaunchGame>d__69 <LaunchGame>d__;
			<LaunchGame>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<LaunchGame>d__.<>4__this = this;
			<LaunchGame>d__.<>1__state = -1;
			<LaunchGame>d__.<>t__builder.Start<MainWindowViewModel.<LaunchGame>d__69>(ref <LaunchGame>d__);
		}

		// Token: 0x06000048 RID: 72 RVA: 0x0000558F File Offset: 0x0000378F
		private void UpdateCanLaunch()
		{
			this.CanLaunch = (this.runner.IsValid() && !this.Loose);
		}

		// Token: 0x06000049 RID: 73 RVA: 0x000055B0 File Offset: 0x000037B0
		[ReactiveCommand]
		public void Uninstall()
		{
			MainWindowViewModel.<Uninstall>d__71 <Uninstall>d__;
			<Uninstall>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<Uninstall>d__.<>4__this = this;
			<Uninstall>d__.<>1__state = -1;
			<Uninstall>d__.<>t__builder.Start<MainWindowViewModel.<Uninstall>d__71>(ref <Uninstall>d__);
		}

		// Token: 0x0600004A RID: 74 RVA: 0x000055E7 File Offset: 0x000037E7
		private static bool IsValidExe(string path)
		{
			return File.Exists(path) && Path.GetFileName(path).Equals(MainWindowViewModel.ExeName, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x0600004B RID: 75 RVA: 0x00005604 File Offset: 0x00003804
		[ReactiveCommand]
		public void SelectExe()
		{
			MainWindowViewModel.<SelectExe>d__74 <SelectExe>d__;
			<SelectExe>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<SelectExe>d__.<>4__this = this;
			<SelectExe>d__.<>1__state = -1;
			<SelectExe>d__.<>t__builder.Start<MainWindowViewModel.<SelectExe>d__74>(ref <SelectExe>d__);
		}

		// Token: 0x0600004C RID: 76 RVA: 0x0000563C File Offset: 0x0000383C
		[ReactiveCommand]
		public void MergeMods()
		{
			MainWindowViewModel.<MergeMods>d__75 <MergeMods>d__;
			<MergeMods>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<MergeMods>d__.<>4__this = this;
			<MergeMods>d__.<>1__state = -1;
			<MergeMods>d__.<>t__builder.Start<MainWindowViewModel.<MergeMods>d__75>(ref <MergeMods>d__);
		}

		// Token: 0x0600004D RID: 77 RVA: 0x00005674 File Offset: 0x00003874
		[ReactiveCommand]
		public void AddDll()
		{
			MainWindowViewModel.<AddDll>d__76 <AddDll>d__;
			<AddDll>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<AddDll>d__.<>4__this = this;
			<AddDll>d__.<>1__state = -1;
			<AddDll>d__.<>t__builder.Start<MainWindowViewModel.<AddDll>d__76>(ref <AddDll>d__);
		}

		// Token: 0x0600004E RID: 78 RVA: 0x000056AC File Offset: 0x000038AC
		private void SyncDllOptions(bool dllsAuthoritative)
		{
			bool flag = this.Dlls.Any((string f) => f.Equals(this.helperPath, StringComparison.OrdinalIgnoreCase));
			if (dllsAuthoritative)
			{
				this.Crashfix = flag;
				return;
			}
			if (flag && !this.Crashfix)
			{
				this.Dlls = this.Dlls.RemoveAll((string f) => f.Equals(this.helperPath, StringComparison.OrdinalIgnoreCase));
				return;
			}
			if (!flag && this.Crashfix)
			{
				this.Dlls = this.Dlls.Add(this.helperPath);
			}
		}

		// Token: 0x0600004F RID: 79 RVA: 0x00005727 File Offset: 0x00003927
		[ReactiveCommand]
		public void CheckUpdates()
		{
			Process.Start(new ProcessStartInfo("https://www.nexusmods.com/darksoulsremastered/mods/922")
			{
				UseShellExecute = true
			});
		}

		// Token: 0x06000050 RID: 80 RVA: 0x00005740 File Offset: 0x00003940
		[ReactiveCommand]
		public void ToggleDarkMode()
		{
			if (Application.Current.ActualThemeVariant == ThemeVariant.Light)
			{
				Application.Current.RequestedThemeVariant = ThemeVariant.Dark;
				return;
			}
			Application.Current.RequestedThemeVariant = ThemeVariant.Light;
		}

		// Token: 0x06000051 RID: 81 RVA: 0x00005778 File Offset: 0x00003978
		[ReactiveCommand]
		public void ImportOptions()
		{
			MainWindowViewModel.<ImportOptions>d__80 <ImportOptions>d__;
			<ImportOptions>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<ImportOptions>d__.<>4__this = this;
			<ImportOptions>d__.<>1__state = -1;
			<ImportOptions>d__.<>t__builder.Start<MainWindowViewModel.<ImportOptions>d__80>(ref <ImportOptions>d__);
		}

		// Token: 0x06000052 RID: 82 RVA: 0x000057B0 File Offset: 0x000039B0
		[ReactiveCommand]
		public void ResetOptions()
		{
			MainWindowViewModel.<ResetOptions>d__81 <ResetOptions>d__;
			<ResetOptions>d__.<>t__builder = AsyncVoidMethodBuilder.Create();
			<ResetOptions>d__.<>4__this = this;
			<ResetOptions>d__.<>1__state = -1;
			<ResetOptions>d__.<>t__builder.Start<MainWindowViewModel.<ResetOptions>d__81>(ref <ResetOptions>d__);
		}

		// Token: 0x06000053 RID: 83 RVA: 0x000057E8 File Offset: 0x000039E8
		[NullableContext(2)]
		private static string GetHeuristicExe()
		{
			FileInfo fileInfo;
			for (DirectoryInfo directoryInfo = new DirectoryInfo("."); directoryInfo != null; directoryInfo = directoryInfo.Parent)
			{
				fileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, MainWindowViewModel.ExeName));
				if (fileInfo.Exists)
				{
					return fileInfo.FullName;
				}
			}
			fileInfo = new FileInfo(MainWindowViewModel.DefaultExePath);
			if (fileInfo.Exists)
			{
				return fileInfo.FullName;
			}
			return null;
		}

		// Token: 0x1700000E RID: 14
		// (get) Token: 0x06000054 RID: 84 RVA: 0x0000584C File Offset: 0x00003A4C
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> RandomizeCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._randomizeCommand) == null)
				{
					result = (this._randomizeCommand = ReactiveCommand.Create(new Action(this.Randomize), null, null));
				}
				return result;
			}
		}

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x06000055 RID: 85 RVA: 0x00005880 File Offset: 0x00003A80
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> LaunchGameCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._launchGameCommand) == null)
				{
					result = (this._launchGameCommand = ReactiveCommand.Create(new Action(this.LaunchGame), this.launchEnabled, null));
				}
				return result;
			}
		}

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000056 RID: 86 RVA: 0x000058B8 File Offset: 0x00003AB8
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> UninstallCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._uninstallCommand) == null)
				{
					result = (this._uninstallCommand = ReactiveCommand.Create(new Action(this.Uninstall), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000057 RID: 87 RVA: 0x000058EC File Offset: 0x00003AEC
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> SelectExeCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._selectExeCommand) == null)
				{
					result = (this._selectExeCommand = ReactiveCommand.Create(new Action(this.SelectExe), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000058 RID: 88 RVA: 0x00005920 File Offset: 0x00003B20
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> MergeModsCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._mergeModsCommand) == null)
				{
					result = (this._mergeModsCommand = ReactiveCommand.Create(new Action(this.MergeMods), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000059 RID: 89 RVA: 0x00005954 File Offset: 0x00003B54
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> AddDllCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._addDllCommand) == null)
				{
					result = (this._addDllCommand = ReactiveCommand.Create(new Action(this.AddDll), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600005A RID: 90 RVA: 0x00005988 File Offset: 0x00003B88
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> CheckUpdatesCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._checkUpdatesCommand) == null)
				{
					result = (this._checkUpdatesCommand = ReactiveCommand.Create(new Action(this.CheckUpdates), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x0600005B RID: 91 RVA: 0x000059BC File Offset: 0x00003BBC
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> ToggleDarkModeCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._toggleDarkModeCommand) == null)
				{
					result = (this._toggleDarkModeCommand = ReactiveCommand.Create(new Action(this.ToggleDarkMode), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600005C RID: 92 RVA: 0x000059F0 File Offset: 0x00003BF0
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> ImportOptionsCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._importOptionsCommand) == null)
				{
					result = (this._importOptionsCommand = ReactiveCommand.Create(new Action(this.ImportOptions), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600005D RID: 93 RVA: 0x00005A24 File Offset: 0x00003C24
		[ExcludeFromCodeCoverage]
		public ReactiveCommand<Unit, Unit> ResetOptionsCommand
		{
			get
			{
				ReactiveCommand<Unit, Unit> result;
				if ((result = this._resetOptionsCommand) == null)
				{
					result = (this._resetOptionsCommand = ReactiveCommand.Create(new Action(this.ResetOptions), null, null));
				}
				return result;
			}
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600005E RID: 94 RVA: 0x00005A57 File Offset: 0x00003C57
		// (set) Token: 0x0600005F RID: 95 RVA: 0x00005A5F File Offset: 0x00003C5F
		[GeneratedCode("ReactiveUI.SourceGenerators.ReactiveGenerator", "2.1.0.0")]
		[ExcludeFromCodeCoverage]
		public RandomizerOptions Options
		{
			get
			{
				return this._options;
			}
			[MemberNotNull("_options")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._options, value, "Options");
			}
		}

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000060 RID: 96 RVA: 0x00005A74 File Offset: 0x00003C74
		// (set) Token: 0x06000061 RID: 97 RVA: 0x00005A7C File Offset: 0x00003C7C
		[ExcludeFromCodeCoverage]
		public string Title
		{
			get
			{
				return this._title;
			}
			[MemberNotNull("_title")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._title, value, "Title");
			}
		}

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000062 RID: 98 RVA: 0x00005A91 File Offset: 0x00003C91
		// (set) Token: 0x06000063 RID: 99 RVA: 0x00005A99 File Offset: 0x00003C99
		[ExcludeFromCodeCoverage]
		public bool Simpleasylum
		{
			get
			{
				return this._simpleasylum;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._simpleasylum, value, "Simpleasylum");
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000064 RID: 100 RVA: 0x00005AAE File Offset: 0x00003CAE
		// (set) Token: 0x06000065 RID: 101 RVA: 0x00005AB6 File Offset: 0x00003CB6
		[ExcludeFromCodeCoverage]
		public bool Simpletaurus
		{
			get
			{
				return this._simpletaurus;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._simpletaurus, value, "Simpletaurus");
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000066 RID: 102 RVA: 0x00005ACB File Offset: 0x00003CCB
		// (set) Token: 0x06000067 RID: 103 RVA: 0x00005AD3 File Offset: 0x00003CD3
		[ExcludeFromCodeCoverage]
		public bool Earlyscale
		{
			get
			{
				return this._earlyscale;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._earlyscale, value, "Earlyscale");
			}
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000068 RID: 104 RVA: 0x00005AE8 File Offset: 0x00003CE8
		// (set) Token: 0x06000069 RID: 105 RVA: 0x00005AF0 File Offset: 0x00003CF0
		[ExcludeFromCodeCoverage]
		public bool Scale_default
		{
			get
			{
				return this._scale_default;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._scale_default, value, "Scale_default");
			}
		}

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600006A RID: 106 RVA: 0x00005B05 File Offset: 0x00003D05
		// (set) Token: 0x0600006B RID: 107 RVA: 0x00005B0D File Offset: 0x00003D0D
		[ExcludeFromCodeCoverage]
		public bool Noscale
		{
			get
			{
				return this._noscale;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._noscale, value, "Noscale");
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600006C RID: 108 RVA: 0x00005B22 File Offset: 0x00003D22
		// (set) Token: 0x0600006D RID: 109 RVA: 0x00005B2A File Offset: 0x00003D2A
		[ExcludeFromCodeCoverage]
		public bool Scaleup
		{
			get
			{
				return this._scaleup;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._scaleup, value, "Scaleup");
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600006E RID: 110 RVA: 0x00005B3F File Offset: 0x00003D3F
		// (set) Token: 0x0600006F RID: 111 RVA: 0x00005B47 File Offset: 0x00003D47
		[ExcludeFromCodeCoverage]
		public bool Antighost
		{
			get
			{
				return this._antighost;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._antighost, value, "Antighost");
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000070 RID: 112 RVA: 0x00005B5C File Offset: 0x00003D5C
		// (set) Token: 0x06000071 RID: 113 RVA: 0x00005B64 File Offset: 0x00003D64
		[ExcludeFromCodeCoverage]
		public bool Impolite
		{
			get
			{
				return this._impolite;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._impolite, value, "Impolite");
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000072 RID: 114 RVA: 0x00005B79 File Offset: 0x00003D79
		// (set) Token: 0x06000073 RID: 115 RVA: 0x00005B81 File Offset: 0x00003D81
		[ExcludeFromCodeCoverage]
		public bool Editnames
		{
			get
			{
				return this._editnames;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._editnames, value, "Editnames");
			}
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000074 RID: 116 RVA: 0x00005B96 File Offset: 0x00003D96
		// (set) Token: 0x06000075 RID: 117 RVA: 0x00005B9E File Offset: 0x00003D9E
		[ExcludeFromCodeCoverage]
		public bool Gravelord_default
		{
			get
			{
				return this._gravelord_default;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._gravelord_default, value, "Gravelord_default");
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000076 RID: 118 RVA: 0x00005BB3 File Offset: 0x00003DB3
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00005BBB File Offset: 0x00003DBB
		[ExcludeFromCodeCoverage]
		public bool Nogravelord
		{
			get
			{
				return this._nogravelord;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._nogravelord, value, "Nogravelord");
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000078 RID: 120 RVA: 0x00005BD0 File Offset: 0x00003DD0
		// (set) Token: 0x06000079 RID: 121 RVA: 0x00005BD8 File Offset: 0x00003DD8
		[ExcludeFromCodeCoverage]
		public bool Permagravelord
		{
			get
			{
				return this._permagravelord;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._permagravelord, value, "Permagravelord");
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x0600007A RID: 122 RVA: 0x00005BED File Offset: 0x00003DED
		// (set) Token: 0x0600007B RID: 123 RVA: 0x00005BF5 File Offset: 0x00003DF5
		[ExcludeFromCodeCoverage]
		public bool Vagrant_default
		{
			get
			{
				return this._vagrant_default;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._vagrant_default, value, "Vagrant_default");
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x0600007C RID: 124 RVA: 0x00005C0A File Offset: 0x00003E0A
		// (set) Token: 0x0600007D RID: 125 RVA: 0x00005C12 File Offset: 0x00003E12
		[ExcludeFromCodeCoverage]
		public bool Permavagrant
		{
			get
			{
				return this._permavagrant;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._permavagrant, value, "Permavagrant");
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x0600007E RID: 126 RVA: 0x00005C27 File Offset: 0x00003E27
		// (set) Token: 0x0600007F RID: 127 RVA: 0x00005C2F File Offset: 0x00003E2F
		[ExcludeFromCodeCoverage]
		public bool Limitvagrant
		{
			get
			{
				return this._limitvagrant;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._limitvagrant, value, "Limitvagrant");
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000080 RID: 128 RVA: 0x00005C44 File Offset: 0x00003E44
		// (set) Token: 0x06000081 RID: 129 RVA: 0x00005C4C File Offset: 0x00003E4C
		[ExcludeFromCodeCoverage]
		public bool Crashfix
		{
			get
			{
				return this._crashfix;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._crashfix, value, "Crashfix");
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000082 RID: 130 RVA: 0x00005C61 File Offset: 0x00003E61
		// (set) Token: 0x06000083 RID: 131 RVA: 0x00005C69 File Offset: 0x00003E69
		[ExcludeFromCodeCoverage]
		public bool Mergegame
		{
			get
			{
				return this._mergegame;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._mergegame, value, "Mergegame");
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000084 RID: 132 RVA: 0x00005C7E File Offset: 0x00003E7E
		// (set) Token: 0x06000085 RID: 133 RVA: 0x00005C86 File Offset: 0x00003E86
		[ExcludeFromCodeCoverage]
		public bool Loose
		{
			get
			{
				return this._loose;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._loose, value, "Loose");
			}
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000086 RID: 134 RVA: 0x00005C9B File Offset: 0x00003E9B
		// (set) Token: 0x06000087 RID: 135 RVA: 0x00005CA3 File Offset: 0x00003EA3
		[ExcludeFromCodeCoverage]
		public string Message
		{
			get
			{
				return this._message;
			}
			[MemberNotNull("_message")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._message, value, "Message");
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x06000088 RID: 136 RVA: 0x00005CB8 File Offset: 0x00003EB8
		// (set) Token: 0x06000089 RID: 137 RVA: 0x00005CC0 File Offset: 0x00003EC0
		[ExcludeFromCodeCoverage]
		public bool MessageError
		{
			get
			{
				return this._messageError;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._messageError, value, "MessageError");
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x0600008A RID: 138 RVA: 0x00005CD5 File Offset: 0x00003ED5
		// (set) Token: 0x0600008B RID: 139 RVA: 0x00005CDD File Offset: 0x00003EDD
		[ExcludeFromCodeCoverage]
		public string Status
		{
			get
			{
				return this._status;
			}
			[MemberNotNull("_status")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._status, value, "Status");
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x0600008C RID: 140 RVA: 0x00005CF2 File Offset: 0x00003EF2
		// (set) Token: 0x0600008D RID: 141 RVA: 0x00005CFA File Offset: 0x00003EFA
		[ExcludeFromCodeCoverage]
		public bool StatusFailure
		{
			get
			{
				return this._statusFailure;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._statusFailure, value, "StatusFailure");
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00005D0F File Offset: 0x00003F0F
		// (set) Token: 0x0600008F RID: 143 RVA: 0x00005D17 File Offset: 0x00003F17
		[ExcludeFromCodeCoverage]
		public bool StatusSuccess
		{
			get
			{
				return this._statusSuccess;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._statusSuccess, value, "StatusSuccess");
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000090 RID: 144 RVA: 0x00005D2C File Offset: 0x00003F2C
		// (set) Token: 0x06000091 RID: 145 RVA: 0x00005D34 File Offset: 0x00003F34
		[ExcludeFromCodeCoverage]
		public string Exe
		{
			get
			{
				return this._exe;
			}
			[MemberNotNull("_exe")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._exe, value, "Exe");
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00005D49 File Offset: 0x00003F49
		// (set) Token: 0x06000093 RID: 147 RVA: 0x00005D51 File Offset: 0x00003F51
		[ExcludeFromCodeCoverage]
		public string Mod
		{
			get
			{
				return this._mod;
			}
			[MemberNotNull("_mod")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._mod, value, "Mod");
			}
		}

		// Token: 0x17000033 RID: 51
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00005D66 File Offset: 0x00003F66
		// (set) Token: 0x06000095 RID: 149 RVA: 0x00005D6E File Offset: 0x00003F6E
		[ExcludeFromCodeCoverage]
		public string DllStr
		{
			get
			{
				return this._dllStr;
			}
			[MemberNotNull("_dllStr")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._dllStr, value, "DllStr");
			}
		}

		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000096 RID: 150 RVA: 0x00005D83 File Offset: 0x00003F83
		// (set) Token: 0x06000097 RID: 151 RVA: 0x00005D8B File Offset: 0x00003F8B
		[ExcludeFromCodeCoverage]
		public ImmutableList<string> Dlls
		{
			get
			{
				return this._dlls;
			}
			[MemberNotNull("_dlls")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._dlls, value, "Dlls");
			}
		}

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000098 RID: 152 RVA: 0x00005DA0 File Offset: 0x00003FA0
		// (set) Token: 0x06000099 RID: 153 RVA: 0x00005DA8 File Offset: 0x00003FA8
		[ExcludeFromCodeCoverage]
		public string Seed
		{
			get
			{
				return this._seed;
			}
			[MemberNotNull("_seed")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._seed, value, "Seed");
			}
		}

		// Token: 0x17000036 RID: 54
		// (get) Token: 0x0600009A RID: 154 RVA: 0x00005DBD File Offset: 0x00003FBD
		// (set) Token: 0x0600009B RID: 155 RVA: 0x00005DC5 File Offset: 0x00003FC5
		[ExcludeFromCodeCoverage]
		public bool RerollSeed
		{
			get
			{
				return this._rerollSeed;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._rerollSeed, value, "RerollSeed");
			}
		}

		// Token: 0x17000037 RID: 55
		// (get) Token: 0x0600009C RID: 156 RVA: 0x00005DDA File Offset: 0x00003FDA
		// (set) Token: 0x0600009D RID: 157 RVA: 0x00005DE2 File Offset: 0x00003FE2
		[ExcludeFromCodeCoverage]
		public bool MustRerollSeed
		{
			get
			{
				return this._mustRerollSeed;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._mustRerollSeed, value, "MustRerollSeed");
			}
		}

		// Token: 0x17000038 RID: 56
		// (get) Token: 0x0600009E RID: 158 RVA: 0x00005DF7 File Offset: 0x00003FF7
		// (set) Token: 0x0600009F RID: 159 RVA: 0x00005DFF File Offset: 0x00003FFF
		[ExcludeFromCodeCoverage]
		public string RandomizeText
		{
			get
			{
				return this._randomizeText;
			}
			[MemberNotNull("_randomizeText")]
			set
			{
				this.RaiseAndSetIfChanged(ref this._randomizeText, value, "RandomizeText");
			}
		}

		// Token: 0x17000039 RID: 57
		// (get) Token: 0x060000A0 RID: 160 RVA: 0x00005E14 File Offset: 0x00004014
		// (set) Token: 0x060000A1 RID: 161 RVA: 0x00005E1C File Offset: 0x0000401C
		[ExcludeFromCodeCoverage]
		public bool Working
		{
			get
			{
				return this._working;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._working, value, "Working");
			}
		}

		// Token: 0x1700003A RID: 58
		// (get) Token: 0x060000A2 RID: 162 RVA: 0x00005E31 File Offset: 0x00004031
		// (set) Token: 0x060000A3 RID: 163 RVA: 0x00005E39 File Offset: 0x00004039
		[ExcludeFromCodeCoverage]
		public bool Launching
		{
			get
			{
				return this._launching;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._launching, value, "Launching");
			}
		}

		// Token: 0x1700003B RID: 59
		// (get) Token: 0x060000A4 RID: 164 RVA: 0x00005E4E File Offset: 0x0000404E
		// (set) Token: 0x060000A5 RID: 165 RVA: 0x00005E56 File Offset: 0x00004056
		[ExcludeFromCodeCoverage]
		public bool CanLaunch
		{
			get
			{
				return this._canLaunch;
			}
			set
			{
				this.RaiseAndSetIfChanged(ref this._canLaunch, value, "CanLaunch");
			}
		}

		// Token: 0x0400002A RID: 42
		private static readonly string ExeName = "DarkSoulsRemastered.exe";

		// Token: 0x0400002B RID: 43
		private static readonly string DefaultExePath = "C:\\Program Files (x86)\\Steam\\steamapps\\common\\DARK SOULS REMASTERED\\" + MainWindowViewModel.ExeName;

		// Token: 0x0400002C RID: 44
		private readonly Messages messages;

		// Token: 0x0400002D RID: 45
		private readonly ModRunner runner;

		// Token: 0x0400002E RID: 46
		private readonly string helperPath;

		// Token: 0x0400002F RID: 47
		[Reactive]
		private RandomizerOptions _options = new RandomizerOptions(GameSpec.FromGame.DS1R);

		// Token: 0x04000031 RID: 49
		[Reactive]
		private string _title = "Matt's DS1 Enemy Randomizer " + Randomizer.DS1Version;

		// Token: 0x04000032 RID: 50
		[Reactive]
		private bool _simpleasylum;

		// Token: 0x04000033 RID: 51
		[Reactive]
		private bool _simpletaurus;

		// Token: 0x04000034 RID: 52
		[Reactive]
		private bool _earlyscale;

		// Token: 0x04000035 RID: 53
		[Reactive]
		private bool _scale_default;

		// Token: 0x04000036 RID: 54
		[Reactive]
		private bool _noscale;

		// Token: 0x04000037 RID: 55
		[Reactive]
		private bool _scaleup;

		// Token: 0x04000038 RID: 56
		[Reactive]
		private bool _antighost;

		// Token: 0x04000039 RID: 57
		[Reactive]
		private bool _impolite;

		// Token: 0x0400003A RID: 58
		[Reactive]
		private bool _editnames;

		// Token: 0x0400003B RID: 59
		[Reactive]
		private bool _gravelord_default;

		// Token: 0x0400003C RID: 60
		[Reactive]
		private bool _nogravelord;

		// Token: 0x0400003D RID: 61
		[Reactive]
		private bool _permagravelord;

		// Token: 0x0400003E RID: 62
		[Reactive]
		private bool _vagrant_default;

		// Token: 0x0400003F RID: 63
		[Reactive]
		private bool _permavagrant;

		// Token: 0x04000040 RID: 64
		[Reactive]
		private bool _limitvagrant;

		// Token: 0x04000041 RID: 65
		[Reactive]
		private bool _crashfix;

		// Token: 0x04000042 RID: 66
		[Reactive]
		private bool _mergegame;

		// Token: 0x04000043 RID: 67
		[Reactive]
		private bool _loose;

		// Token: 0x04000044 RID: 68
		[Reactive]
		private string _message = "";

		// Token: 0x04000045 RID: 69
		[Reactive]
		private bool _messageError;

		// Token: 0x04000046 RID: 70
		[Reactive]
		private string _status = "";

		// Token: 0x04000047 RID: 71
		[Reactive]
		private bool _statusFailure;

		// Token: 0x04000048 RID: 72
		[Reactive]
		private bool _statusSuccess;

		// Token: 0x04000049 RID: 73
		[Reactive]
		private string _exe = "";

		// Token: 0x0400004A RID: 74
		[Reactive]
		private string _mod = "";

		// Token: 0x0400004B RID: 75
		private string PrevMod = "";

		// Token: 0x0400004C RID: 76
		[Reactive]
		private string _dllStr = "";

		// Token: 0x0400004D RID: 77
		[Reactive]
		private ImmutableList<string> _dlls = ImmutableList.Create<string>(default(ReadOnlySpan<string>));

		// Token: 0x0400004E RID: 78
		[Reactive]
		private string _seed = "";

		// Token: 0x0400004F RID: 79
		[Reactive]
		private bool _rerollSeed;

		// Token: 0x04000050 RID: 80
		[Reactive]
		private bool _mustRerollSeed;

		// Token: 0x04000051 RID: 81
		[Reactive]
		private string _randomizeText = "Randomize!";

		// Token: 0x04000052 RID: 82
		[Reactive]
		private bool _working;

		// Token: 0x04000053 RID: 83
		[Reactive]
		private bool _launching;

		// Token: 0x04000054 RID: 84
		[Reactive]
		private bool _canLaunch;

		// Token: 0x04000055 RID: 85
		private IObservable<bool> launchEnabled;

		// Token: 0x04000059 RID: 89
		private readonly string DefaultOpts = "antighost crashfix editnames simpleasylum simpletaurus";

		// Token: 0x0400005A RID: 90
		[Messages.Localize(null)]
		private static readonly Messages.Text deleteCrashfixSuccess = new Messages.Text("Deleted DS1CrashFix.dll at {0}", "EldenForm_deleteCrashfixSuccess", null);

		// Token: 0x0400005B RID: 91
		[Messages.Localize(null)]
		private static readonly Messages.Text restoreFileSuccess = new Messages.Text("Restored from {0}", "EldenForm_restoreFileSuccess", null);

		// Token: 0x0400005C RID: 92
		[Messages.Localize(null)]
		private static readonly Messages.Text restoreFileFailure = new Messages.Text("Failed to restore from {0}!", "EldenForm_restoreFileFailure", null);

		// Token: 0x0400005D RID: 93
		private static readonly FilePickerFileType ExeType = new FilePickerFileType("Dark Souls Remastered")
		{
			Patterns = new <>z__ReadOnlySingleElementList<string>(MainWindowViewModel.ExeName)
		};

		// Token: 0x0400005E RID: 94
		[Nullable(2)]
		[GeneratedCode("ReactiveUI.SourceGenerators.ReactiveCommandGenerator", "2.1.0.0")]
		private ReactiveCommand<Unit, Unit> _randomizeCommand;

		// Token: 0x0400005F RID: 95
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _launchGameCommand;

		// Token: 0x04000060 RID: 96
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _uninstallCommand;

		// Token: 0x04000061 RID: 97
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _selectExeCommand;

		// Token: 0x04000062 RID: 98
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _mergeModsCommand;

		// Token: 0x04000063 RID: 99
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _addDllCommand;

		// Token: 0x04000064 RID: 100
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _checkUpdatesCommand;

		// Token: 0x04000065 RID: 101
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _toggleDarkModeCommand;

		// Token: 0x04000066 RID: 102
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _importOptionsCommand;

		// Token: 0x04000067 RID: 103
		[Nullable(2)]
		private ReactiveCommand<Unit, Unit> _resetOptionsCommand;

		// Token: 0x02000015 RID: 21
		[CompilerGenerated]
		private static class <>O
		{
			// Token: 0x0400006C RID: 108
			[Nullable(0)]
			public static Func<string, string> <0>__GetFileName;
		}
	}
}
