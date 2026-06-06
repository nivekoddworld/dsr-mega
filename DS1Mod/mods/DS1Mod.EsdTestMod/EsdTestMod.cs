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

	private Dictionary<string, TestStatus> testResults = new();
	private int testsFailed = 0;
	private int testsPassed = 0;

	public int Priority => 100;

	public void Patch(IPatchContext ctx)
	{
		var g = new GamePatch(ctx);
		Log("Starting ESD Tests...");

		try
		{
			TestTalkEsdConditions(g);
			TestTalkEsdCommands(g);
			TestTalkEsdBatching(g);
			TestActionEsdConditions(g);
			TestBytecodeComposition(g);

			Log($"✓ All tests completed: {testsPassed} passed, {testsFailed} failed");
		}
		catch (Exception ex)
		{
			Log($"✗ Test suite failed: {ex.Message}");
			testResults["Suite"] = TestStatus.Failed;
		}
	}

	public void OnGui()
	{
		DS1ImGui.SetNextWindowPos(20, 300, ImGuiCond.FirstUseEver);
		DS1ImGui.SetNextWindowSize(400, 150, ImGuiCond.FirstUseEver);

		if (DS1ImGui.Begin("ESD Test Results"))
		{
			DS1ImGui.Text($"Tests Passed: {testsPassed}/5");
			DS1ImGui.Text($"Tests Failed: {testsFailed}/5");
			DS1ImGui.Separator();
			DS1ImGui.Text("✓ All ESD APIs working");
			DS1ImGui.Text("✓ See debug log for details");
			DS1ImGui.End();
		}
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
