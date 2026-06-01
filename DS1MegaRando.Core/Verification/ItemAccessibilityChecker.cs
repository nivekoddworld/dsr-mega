using DS1MegaRando.Core.Items;

namespace DS1MegaRando.Core.Verification;

public class ItemAccessibilityChecker
{
    /// <summary>
    /// Detects circular key item dependencies (item A requires area B requires item A).
    /// </summary>
    public VerificationResult Check(
        Dictionary<int, int> lotAssignments,
        Dictionary<string, string> lotToArea,
        Dictionary<string, string> areaRequiresItem)
    {
        var issues = new List<string>();

        foreach (var (lotId, itemId) in lotAssignments)
        {
            if (!lotToArea.TryGetValue(lotId.ToString(), out var area)) continue;
            if (!areaRequiresItem.TryGetValue(area, out var requiredItem)) continue;

            // Check if the item placed here is the same one required to reach here
            // (Simplified — full check would trace the full dependency chain)
            var reqItemLots = lotAssignments
                .Where(kvp => kvp.Value.ToString() == requiredItem)
                .Select(kvp => kvp.Key);

            foreach (var reqLot in reqItemLots)
            {
                if (lotToArea.TryGetValue(reqLot.ToString(), out var reqArea) &&
                    areaRequiresItem.TryGetValue(reqArea, out var reqReq) &&
                    reqReq == itemId.ToString())
                {
                    issues.Add($"Circular dependency detected: lot {lotId} (item {itemId}) " +
                               $"in {area} requires item in lot {reqLot}");
                }
            }
        }

        return issues.Count == 0
            ? VerificationResult.Success()
            : VerificationResult.Failure(issues);
    }
}
