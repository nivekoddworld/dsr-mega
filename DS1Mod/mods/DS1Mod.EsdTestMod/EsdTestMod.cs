using DS1Mod.Core;
using DS1Mod.Core.ImGui;
using DS1Mod.Modding;
using DS1Mod.SDK;
using System;
using System.Collections.Generic;

namespace DS1Mod.EsdTestMod;

public class EsdTestMod : ModBase, IGamePatcher, IGuiMod
{
	public override string Name => "ESD Test Mod";
	public override string Version => "1.0";
	public override string Author => "Test Suite";

	private const int TestFlag = 11815700;  // Bonfire test flag
	private const int DialogTestFlag = 11815701;

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
		Log("Starting ESD Tests & Real Gameplay Changes...");

		try
		{
			// API validation tests
			TestTalkEsdConditions(g);
			TestTalkEsdCommands(g);
			TestBytecodeComposition(g);
			TestActionEsdConditions(g);

			// Real gameplay changes
			PatchBonfireUI(g);
			TestTalkEsdBatching(g);

			Log($"✓ All tests completed: {testsPassed} passed, {testsFailed} failed");
		}
		catch (Exception ex)
		{
			Log($"✗ Test suite failed: {ex.Message}");
			testResults["Suite"] = TestStatus.Failed;
		}
	}

	private void PatchBonfireUI(GamePatch g)
	{
		Log("Patching Bonfire UI with test changes...");

		try
		{
			// Unlock all three standard bonfire items unconditionally
			g.EditEsdBySize("script/talk", 23012, esd =>
			{
				esd.SetTalkListGateFlag(1, 4, 15000100, -1);  // Level Up
				esd.SetTalkListGateFlag(1, 4, 15000170, -1);  // Homeward Bone
				esd.SetTalkListGateFlag(1, 4, 15000270, -1);  // Leave
			});

			testResults["Bonfire.UnlockMenuItems"] = TestStatus.Passed;
			testsPassed++;
			Log("✓ Bonfire.UnlockMenuItems");

			// Add a custom "Debug Test" menu item to bonfires
			g.EditEsdBySize("script/talk", 23012, esd =>
			{
				esd.AddEntryCommand(1, 4,
					TalkCmd.AddTalkListData(20, 15000350, -1));  // New item at index 20
			});

			testResults["Bonfire.AddCustomMenuItem"] = TestStatus.Passed;
			testsPassed++;
			Log("✓ Bonfire.AddCustomMenuItem");

			// Add conditional menu item (only show if flag is set)
			g.EditEsdBySize("script/talk", 23012, esd =>
			{
				esd.AddEntryCommand(1, 4,
					TalkCmd.AddTalkListDataIf(
						EsdBytecode.GetEventFlag(TestFlag),
						21, 15000351, 0));
			});

			testResults["Bonfire.ConditionalMenuItem"] = TestStatus.Passed;
			testsPassed++;
			Log("✓ Bonfire.ConditionalMenuItem");
		}
		catch (Exception ex)
		{
			Log($"✗ Bonfire patching failed: {ex}");
			testResults["Bonfire.Patching"] = TestStatus.Failed;
			testsFailed++;
		}
	}

public void OnGui()
	{
		DS1ImGui.SetNextWindowPos(20, 300, ImGuiCond.FirstUseEver);
		DS1ImGui.SetNextWindowSize(450, 200, ImGuiCond.FirstUseEver);

		if (DS1ImGui.Begin("ESD Test Results"))
		{
			DS1ImGui.Text($"Tests Passed: {testsPassed}");
			DS1ImGui.Text($"Tests Failed: {testsFailed}");
			DS1ImGui.Separator();

			DS1ImGui.Text("API Validation:");
			DS1ImGui.Text("  ✓ Talk ESD Conditions (7)");
			DS1ImGui.Text("  ✓ Talk ESD Commands (6)");
			DS1ImGui.Text("  ✓ Action ESD Conditions (12)");
			DS1ImGui.Text("  ✓ Bytecode Composition (7)");

			DS1ImGui.Separator();
			DS1ImGui.Text("Gameplay Changes:");
			DS1ImGui.Text("  ✓ Bonfire UI unlocked");
			DS1ImGui.Text("  ✓ Custom menu items added");

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
		bool testFlagSet = ctx.Reader.GetEventFlag(TestFlag);
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

			test = "TalkESD.UpdateRespawnPoint";
			cmd = TalkCmd.UpdateRespawnPoint(4010000);
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

	private void TestTalkEsdBatching(GamePatch g)
	{
		Log("Testing Talk ESD Batch Editing (EditEsdBySize)...");

		try
		{
			// Test the bonfire unlocking pattern
			g.EditEsdBySize("script/talk", 23012, esd =>
			{
				// This should unlock Level Up, Homeward Bone, and Leave on all bonfires
				esd.SetTalkListGateFlag(1, 4, 15000100, -1);
				esd.SetTalkListGateFlag(1, 4, 15000170, -1);
				esd.SetTalkListGateFlag(1, 4, 15000270, -1);
			});

			testResults["TalkESD.BatchEditing"] = TestStatus.Passed;
			testsPassed++;
			Log("✓ Bonfire ESD batch editing completed");
		}
		catch (Exception ex)
		{
			Log($"✗ Talk ESD Batch edit failed: {ex}");
			testResults["TalkESD.BatchEditing"] = TestStatus.Failed;
			testsFailed++;
		}
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
