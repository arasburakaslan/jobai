using System.Text.RegularExpressions;
using JobRag.Application.Abstractions;
using JobRag.Domain.Jobs;
using Microsoft.Extensions.Logging;

namespace JobRag.Infrastructure.Crawlers;

/// <summary>
/// Extracts structured metadata from job descriptions using rule-based heuristics.
/// Can be extended with AI/NLP models later.
/// </summary>
public partial class JobMetadataExtractor : IJobMetadataExtractor
{
	private readonly ILogger<JobMetadataExtractor> _logger;

	public JobMetadataExtractor(ILogger<JobMetadataExtractor> logger)
	{
		_logger = logger;
	}

	public Task<Job> ExtractMetadataAsync(Job job, CancellationToken ct = default)
	{
		var text = $"{job.Title} {job.Description}".ToLowerInvariant();

		job.Industry = ClassifyIndustry(text);
		job.LanguageRequired = DetectLanguage(text);
		job.VisaSponsorship = DetectVisaSponsorship(text);
		job.Remote = DetectRemote(text);

		var (min, max, currency) = ParseSalary(job.Description);
		job.SalaryMin = min;
		job.SalaryMax = max;
		job.Currency = currency;

		_logger.LogDebug("Extracted metadata for job {Title}: Industry={Industry}, Remote={Remote}",
			job.Title, job.Industry, job.Remote);

		return Task.FromResult(job);
	}

	private static string? ClassifyIndustry(string text)
	{
		if (text.Contains("software") || text.Contains("developer") || text.Contains("engineer") ||
			text.Contains("c#") || text.Contains(".net") || text.Contains("java") ||
			text.Contains("python") || text.Contains("devops") || text.Contains("cloud"))
			return "IT";

		if (text.Contains("nurse") || text.Contains("doctor") || text.Contains("healthcare") ||
			text.Contains("medical") || text.Contains("pharma"))
			return "Healthcare";

		if (text.Contains("finance") || text.Contains("banking") || text.Contains("accounting"))
			return "Finance";

		if (text.Contains("marketing") || text.Contains("sales") || text.Contains("advertising"))
			return "Marketing";

		if (text.Contains("logistics") || text.Contains("warehouse") || text.Contains("supply chain"))
			return "Logistics";

		return null;
	}

	private static string? DetectLanguage(string text)
	{
		if (text.Contains("german") && text.Contains("fluent"))
			return "German (fluent)";
		if (text.Contains("german") && (text.Contains("b2") || text.Contains("b1")))
			return "German (B1-B2)";
		if (text.Contains("deutsch"))
			return "German";
		if (text.Contains("english only") || text.Contains("no german"))
			return "English only";

		return null;
	}

	private static bool DetectVisaSponsorship(string text)
	{
		return text.Contains("visa sponsorship") ||
			   text.Contains("work permit") ||
			   text.Contains("relocation support") ||
			   text.Contains("relocation package");
	}

	private static bool DetectRemote(string text)
	{
		return text.Contains("remote") ||
			   text.Contains("work from home") ||
			   text.Contains("home office") ||
			   text.Contains("homeoffice");
	}

	private static (int? Min, int? Max, string? Currency) ParseSalary(string text)
	{
		var match = SalaryRangeRegex().Match(text);
		if (match.Success)
		{
			if (int.TryParse(match.Groups[1].Value.Replace(",", "").Replace(".", ""), out var min) &&
				int.TryParse(match.Groups[2].Value.Replace(",", "").Replace(".", ""), out var max))
			{
				if (min < 1000) min *= 1000;
				if (max < 1000) max *= 1000;

				return (min, max, "EUR");
			}
		}

		return (null, null, null);
	}

	[GeneratedRegex(@"(?:€|EUR)\s*(\d[\d,\.]*k?)\s*[-–]\s*(?:€|EUR)?\s*(\d[\d,\.]*k?)", RegexOptions.IgnoreCase)]
	private static partial Regex SalaryRangeRegex();
}
