using JobRag.Agents.Abstractions;
using System.Text.RegularExpressions;

namespace JobRag.Agents.Guardrails;

/// <summary>
/// Scans generated content for PII patterns (emails, phone numbers, addresses, IDs)
/// and either redacts them or blocks the output.
/// 
/// Applied to: Cover letters, any user-facing LLM output.
/// </summary>
public partial class PiiGuardrail : IGuardrail<string>
{
    public string Name => "PII Detection";

    public Task<GuardrailResult<string>> ValidateAsync(string content, CancellationToken ct = default)
    {
        var issues = new List<string>();

        // Check for email addresses
        if (EmailPattern().IsMatch(content))
            issues.Add("Contains email address");

        // Check for phone numbers (international formats)
        if (PhonePattern().IsMatch(content))
            issues.Add("Contains phone number");

        // Check for German social security numbers (Sozialversicherungsnummer)
        if (GermanSsnPattern().IsMatch(content))
            issues.Add("Contains German social security number pattern");

        // Check for IBAN numbers
        if (IbanPattern().IsMatch(content))
            issues.Add("Contains IBAN number");

        if (issues.Count > 0)
        {
            return Task.FromResult(
                GuardrailResult<string>.Block($"PII detected: {string.Join(", ", issues)}"));
        }

        return Task.FromResult(GuardrailResult<string>.Pass(content));
    }

    [GeneratedRegex(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", RegexOptions.Compiled)]
    private static partial Regex EmailPattern();

    [GeneratedRegex(@"(\+?\d{1,3}[-.\s]?)?\(?\d{2,4}\)?[-.\s]?\d{3,4}[-.\s]?\d{3,4}", RegexOptions.Compiled)]
    private static partial Regex PhonePattern();

    [GeneratedRegex(@"\d{2}\s?\d{6}\s?[A-Z]\s?\d{3}", RegexOptions.Compiled)]
    private static partial Regex GermanSsnPattern();

    [GeneratedRegex(@"[A-Z]{2}\d{2}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{4}\s?\d{0,2}", RegexOptions.Compiled)]
    private static partial Regex IbanPattern();
}
