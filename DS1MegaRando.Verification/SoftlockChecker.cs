using DS1MegaRando.Core.Annotations;
using DS1MegaRando.Core.FogGate;
using DS1MegaRando.Core.Graph;
using DS1MegaRando.Core.Items;
using DS1MegaRando.Core.Settings;

namespace DS1MegaRando.Core.Verification;

public class SoftlockChecker
{
    // These areas must all be reachable for the game to be completable
    private static readonly string[] RequiredAreas =
    {
        "totg_nito",         // Nito
        "newlondo_fourkings",// Four Kings
        "demonruins_bedofchaos", // Bed of Chaos
        "dukes_seath2",      // Seath
        "kiln_gwyn",         // Gwyn (final boss)
        "parish_andre",      // Andre the Blacksmith (guarantee repair box access)
    };

    public VerificationResult Verify(
        FogGateResult? fogResult,
        ItemResult? itemResult,
        AnnotationData ann,
        MegaSettings settings)
    {
        if (fogResult == null) return VerificationResult.Success();

        var graph   = fogResult.ConnectedGraph;
        var checker = new GraphChecker();

        // Build item areas from key item placements
        var itemAreas = BuildItemAreas(ann, itemResult);

        var check  = checker.Check(graph, fogResult.StartArea, itemAreas);
        var issues = new List<string>();

        foreach (var required in RequiredAreas)
        {
            if (!check.Reachable.Contains(required))
                issues.Add($"Required area '{required}' is not reachable.");
        }

        // Verify Duke's Prison escape (archive_tower_giant_door_key must be obtainable before being required)
        if (!VerifyDukesPrisonEscape(check))
            issues.Add("Duke's Prison escape may be softlocked (archive_tower_giant_door_key unreachable before needed).");

        return issues.Count == 0
            ? VerificationResult.Success()
            : VerificationResult.Failure(issues);
    }

    private static Dictionary<string, List<string>> BuildItemAreas(AnnotationData ann, ItemResult? itemResult)
    {
        // Default to vanilla locations; item randomizer overrides when active
        var itemAreas = ann.KeyItems.ToDictionary(
            ki => ki.Name,
            ki => ki.Area != null ? new List<string> { ki.Area } : new List<string>());

        if (itemResult == null) return itemAreas;

        // TODO: override with randomized placements from itemResult.LotAssignments
        return itemAreas;
    }

    private static bool VerifyDukesPrisonEscape(GraphChecker.CheckResult check)
    {
        // If dukes_prison is reachable, archive_tower_giant_door_key must also be available
        // This is a simplified check — full check would trace key item dependency chains
        return !check.Reachable.Contains("dukes_prison") ||
               check.Available.Contains("archive_tower_giant_door_key");
    }
}
