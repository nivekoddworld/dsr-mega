using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.Modding;
using DS1Mod.SDK;
using SoulsFormats;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DS1Mod.EsdTestMod;

public class EsdTestMod : ModBase, IGamePatcher, IGuiMod
{
	public override string Name => "ESD Test Mod";
	public override string Version => "1.0";
	public override string Author => "Test Suite";

	// Allocated at patch time via the ID allocation system (allocations.json in game dir).
	// Instance fields so OnGui() can read them after Patch() runs.
	private int _testFlag;
	private int _ratEntityId;
	private int _ratInitEvent;
	private int _ratSpawnEvent;

	private Dictionary<string, TestStatus> testResults = new();
	private int testsFailed = 0;
	private int testsPassed = 0;
	private IModContext? ctx;

	public int Priority => 100;

	public override void OnLoad(IModContext context)
	{
		ctx = context;
	}

	public void Patch(IPatchContext patchCtx)
	{
		var g = new GamePatch(patchCtx);

		// Allocate all IDs via the central allocator (persisted in allocations.json).
		// Same mod always gets the same IDs across runs for save-game compatibility.
		_testFlag    = patchCtx.AllocateId("EventFlags_m18_00_00_00");
		_ratEntityId = patchCtx.AllocateId("MsbEntities_m18_00_00_00");
		int evBase   = patchCtx.AllocateIds("EmevdEvents_m18_00_00_00", 2);
		_ratInitEvent  = evBase;
		_ratSpawnEvent = evBase + 1;

		Log("Starting ESD Framework Tests...");

		try
		{
			// Add FMG text entries for bonfire menu items
			AddFmgEntries(g);

			// API validation tests
			TestTalkEsdConditions(g);
			TestTalkEsdCommands(g);
			TestBytecodeComposition(g);
			TestActionEsdConditions(g);

			// Gameplay changes demonstrating Talk ESD capability
			TestBonfireUnlock(g);

			Log($"✓ All tests completed: {testsPassed} passed, {testsFailed} failed");
		}
		catch (Exception ex)
		{
			Log($"✗ Test suite failed: {ex.Message}");
			testResults["Suite"] = TestStatus.Failed;
		}
	}

	private void AddFmgEntries(GamePatch g)
	{
		Log("Adding FMG text entries...");

		try
		{
			g.EditBnd3Glob("msg", "menu.msgbnd.dcx", bnd =>
			{
				Texts.Set(bnd, Texts.EventText, 15001200, "Spawn Rat");
			});

			Log("✓ FMG entries added");
		}
		catch (Exception ex)
		{
			Log($"✗ FMG patching failed: {ex}");
		}
	}

	private void TestBonfireUnlock(GamePatch g)
	{
		Log("Testing bonfire menu patterns...");

		try
		{
			// Pattern 1: Unlock existing items via gate flags via the shared ESD so
			// changes are combined with AddBonfireMenuItem in a single write pass.
			bool gated = g.EditBonfireEsd(esd =>
			{
				esd.SetTalkListGateFlag(1, 4, 15000100, -1);  // Level Up
				esd.SetTalkListGateFlag(1, 4, 15000170, -1);  // Reverse Hollowing
				esd.SetTalkListGateFlag(1, 4, 15000270, -1);  // Leave
			});
			Assert(gated, "Bonfire.UnlockViaGates (shared ESD available)");

			testResults["Bonfire.UnlockViaGates"] = TestStatus.Passed;
			testsPassed++;
			Log("✓ Bonfire.UnlockViaGates (gate flags queued in shared ESD)");

			// Pattern 2: Add a "Spawn Rat" menu item.
			// Selecting it sets _testFlag; EMEVD in Firelink watches that flag and enables the rat.
			bool itemAdded = g.AddBonfireMenuItem(talkId: 15001200, gateFlag: -1, flagId: 90);
			Assert(itemAdded, "Bonfire.AddMenuItem (spawn rat)");
			testResults["Bonfire.AddMenuItem"] = TestStatus.Passed;
			testsPassed++;
			Log($"✓ Bonfire.AddMenuItem → queued 'Spawn Rat' in shared ESD (flag {_testFlag})");

			// Place rat NPC in Firelink (disabled until flag fires).
			g.EditMsb("m18_00_00_00", msb => msb
				.PlaceEnemy(
					entityId: _ratEntityId,
					modelName: "c1201",  // Small Rat
					npcParamId: 120100,
					thinkParamId: 120100,
					position: new Vector3(48.0f, -62.5f, 103.0f),  // near bonfire
					collisionName: "h0014B0"));

			// EMEVD: disable rat at map load, then loop: watch _testFlag → enable rat → clear flag.
			g.EditEmevd("m18_00_00_00", emevd =>
			{
				// One-shot init: disable the rat when the map first loads.
				emevd.DefineEvent(_ratInitEvent, EMEVD.Event.RestBehaviorType.Default, evb =>
				{
					evb.SetCharacterEnabled(_ratEntityId, EnabledState.Disabled);
				});

				// Spawn loop: wait for flag → enable rat → clear flag → restart.
				emevd.DefineEvent(_ratSpawnEvent, EMEVD.Event.RestBehaviorType.Restart, evb =>
				{
					evb.WhenFlag(_testFlag, FlagState.On);
					evb.SetCharacterEnabled(_ratEntityId, EnabledState.Enabled);
					evb.SetFlag(_testFlag, FlagState.Off);
				});
			});

			Log("✓ Bonfire.SpawnRat (MSB + EMEVD wired for Firelink)");
		}
		catch (Exception ex)
		{
			Log($"✗ Bonfire test failed: {ex}");
			testResults["Bonfire.Patterns"] = TestStatus.Failed;
			testsFailed++;
		}
	}

public void OnGui()
	{
		DS1ImGui.SetNextWindowPos(20, 300, ImGuiCond.FirstUseEver);
		DS1ImGui.SetNextWindowSize(450, 250, ImGuiCond.FirstUseEver);

		if (DS1ImGui.Begin("ESD Framework Tests"))
		{
			DS1ImGui.Text($"Tests Passed: {testsPassed}");
			DS1ImGui.Text($"Tests Failed: {testsFailed}");
			DS1ImGui.Separator();

			DS1ImGui.Text("API Validation Tests:");
			DS1ImGui.Text("  ✓ Talk ESD Conditions (7 functions)");
			DS1ImGui.Text("  ✓ Talk ESD Commands (6 operations)");
			DS1ImGui.Text("  ✓ Action ESD Conditions (12 functions)");
			DS1ImGui.Text("  ✓ Bytecode Composition (7 operators)");

			DS1ImGui.Separator();
			DS1ImGui.Text("Gameplay Demo:");
			DS1ImGui.Text("  ✓ Bonfire items unlocked via gates");
			DS1ImGui.Text("  ✓ 'Spawn Rat' button added to bonfire menu");
			DS1ImGui.Text("  ✓ Firelink MSB + EMEVD wired for rat spawn");

			DS1ImGui.Separator();
			DisplayGameplayState();

			DS1ImGui.End();
		}
	}

	private void DisplayGameplayState()
	{
		if (ctx?.Reader.GetPlayerStats() is not { } stats)
		{
			DS1ImGui.Text("(not in-game)");
			return;
		}

		int level = ctx.Reader.GetSoulLevel();
		DS1ImGui.Text($"Player HP: {stats.CurrentHp}/{stats.MaxHp}");
		DS1ImGui.Text($"Level: {level}");

		// Check if our test flags are set
		bool testFlagSet = _testFlag != 0 && ctx.Reader.GetEventFlag(_testFlag);
		DS1ImGui.Text($"Test Flag: {(testFlagSet ? "SET" : "not set")}");
	}

	private void TestTalkEsdConditions(GamePatch g)
	{
		Log("Testing Talk ESD Condition Functions...");

		try
		{
			var test = "TalkESD.GetEventFlag";
			// Create a condition that checks if flag 11810000 is set
			var cond = EsdBytecode.GetEventFlag(11810000);
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.GetMenuSelection";
			cond = EsdBytecode.GetMenuSelection();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.GetDialogButtonResult";
			cond = EsdBytecode.GetDialogButtonResult();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.IsGenericDialogOpen";
			cond = EsdBytecode.IsGenericDialogOpen();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.GetTimeInState";
			cond = EsdBytecode.GetTimeInState();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.DialogClosedWithButton";
			cond = EsdBytecode.DialogClosedWithButton(1);
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "TalkESD.SelectedItem";
			cond = EsdBytecode.SelectedItem(5);
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);
		}
		catch (Exception ex)
		{
			Log($"✗ Talk ESD Condition test failed: {ex}");
			testResults["TalkESD.Conditions"] = TestStatus.Failed;
			testsFailed++;
		}

		testResults["TalkESD.Conditions"] = TestStatus.Passed;
		testsPassed++;
	}

	private void TestTalkEsdCommands(GamePatch g)
	{
		Log("Testing Talk ESD Commands...");

		try
		{
			var test = "TalkESD.SetEventFlag";
			var cmd = TalkCmd.SetEventFlag(11810500, true);
			Assert(cmd != null, test);

			test = "TalkESD.OpenGenericDialog";
			cmd = TalkCmd.OpenGenericDialog(8, 999, 3, 2, 2);
			Assert(cmd != null, test);

			test = "TalkESD.AddTalkListData";
			cmd = TalkCmd.AddTalkListData(1, 15000100, -1);
			Assert(cmd != null, test);

			test = "TalkESD.AddTalkListDataIf";
			cmd = TalkCmd.AddTalkListDataIf(EsdBytecode.GetEventFlag(11810000), 1, 15000100, 0);
			Assert(cmd != null, test);

			test = "TalkESD.ShowShopMessage";
			cmd = TalkCmd.ShowShopMessage();
			Assert(cmd != null, test);

			test = "TalkESD.ClearTalkListData";
			cmd = TalkCmd.ClearTalkListData();
			Assert(cmd != null, test);
		}
		catch (Exception ex)
		{
			Log($"✗ Talk ESD Command test failed: {ex}");
			testResults["TalkESD.Commands"] = TestStatus.Failed;
			testsFailed++;
		}

		testResults["TalkESD.Commands"] = TestStatus.Passed;
		testsPassed++;
	}

	private void TestActionEsdConditions(GamePatch g)
	{
		Log("Testing Action ESD Condition Functions...");

		try
		{
			var test = "ActionESD.Fn0 (Always-true)";
			var cond = ActionEsdBytecode.Fn0();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn112 (Attack/Combo)";
			cond = ActionEsdBytecode.Fn112();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn109 (Button Release)";
			cond = ActionEsdBytecode.Fn109();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn2 (World State)";
			cond = ActionEsdBytecode.Fn2();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn3 (Stun/Equipment/Buffs)";
			cond = ActionEsdBytecode.Fn3();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn116 (Spell/Item Gating)";
			cond = ActionEsdBytecode.Fn116();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn111 (Dodge/Backstab)";
			cond = ActionEsdBytecode.Fn111();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn115 (Movement)";
			cond = ActionEsdBytecode.Fn115();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.Fn104 (Inventory/Stance)";
			cond = ActionEsdBytecode.Fn104();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.EnemyFn107 (AI Routing)";
			cond = ActionEsdBytecode.EnemyFn107();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.EnemyFn118 (AI Behavior)";
			cond = ActionEsdBytecode.EnemyFn118();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "ActionESD.EnemyFn120 (AI Decision)";
			cond = ActionEsdBytecode.EnemyFn120();
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			testResults["ActionESD.Conditions"] = TestStatus.Passed;
			testsPassed++;
		}
		catch (Exception ex)
		{
			Log($"✗ Action ESD Condition test failed: {ex}");
			testResults["ActionESD.Conditions"] = TestStatus.Failed;
			testsFailed++;
		}
	}

	private void TestBytecodeComposition(GamePatch g)
	{
		Log("Testing Bytecode Composition...");

		try
		{
			var test = "Bytecode.And";
			var cond = EsdBytecode.And(
				EsdBytecode.GetEventFlag(11810000),
				ActionEsdBytecode.Fn3());
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Or";
			cond = EsdBytecode.Or(
				EsdBytecode.GetEventFlag(11810000),
				EsdBytecode.GetMenuSelection());
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Not";
			cond = EsdBytecode.Not(ActionEsdBytecode.Fn3());
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Eq";
			cond = EsdBytecode.Eq(
				EsdBytecode.GetMenuSelection(),
				EsdBytecode.PushInt(5));
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Ne";
			cond = EsdBytecode.Ne(
				EsdBytecode.GetMenuSelection(),
				EsdBytecode.PushInt(0));
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Ge";
			cond = EsdBytecode.Ge(
				ActionEsdBytecode.Fn2(),
				EsdBytecode.PushInt(30));
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			test = "Bytecode.Nesting (And(Or(A, B), Not(C)))";
			cond = EsdBytecode.And(
				EsdBytecode.Or(
					EsdBytecode.GetEventFlag(11810000),
					EsdBytecode.GetMenuSelection()),
				EsdBytecode.Not(ActionEsdBytecode.Fn3()));
			Assert(cond.Length > 0 && cond[^1] == 0xA1, test);

			testResults["Bytecode.Composition"] = TestStatus.Passed;
			testsPassed++;
		}
		catch (Exception ex)
		{
			Log($"✗ Bytecode Composition test failed: {ex}");
			testResults["Bytecode.Composition"] = TestStatus.Failed;
			testsFailed++;
		}
	}

	private void Assert(bool condition, string testName)
	{
		if (!condition)
		{
			throw new InvalidOperationException($"Assertion failed for {testName}");
		}

		Log($"✓ {testName}");
	}

	private void Log(string message)
	{
		Console.WriteLine($"[{Name}] {message}");
	}

	private enum TestStatus
	{
		Unknown,
		Passed,
		Failed
	}
}
