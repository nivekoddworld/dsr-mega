namespace DS1MegaRando.Verification;

public class VerificationResult
{
    public bool              Passed { get; set; }
    public List<string>      Issues { get; set; } = new();

    public static VerificationResult Success() => new() { Passed = true };

    public static VerificationResult Failure(IEnumerable<string> issues) =>
        new() { Passed = false, Issues = issues.ToList() };
}
