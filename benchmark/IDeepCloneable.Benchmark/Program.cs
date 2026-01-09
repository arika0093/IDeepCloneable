using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Running;
using IDeepCloneable.Benchmark;

// Run the benchmarks and generate a markdown file with the results.
// Output: ../BenchmarkResult.md
// Contents:
// # Benchmark Results
// ## <Benchmark class name>
// <Benchmark results>
// (repeated for each benchmark)
// ## Benchmark Environment
// <Environment info produced by BenchmarkDotNet>

// Run the benchmarks
var benchmarkTypes = new[]
{
    typeof(CloneBenchmarks),
    // Add future benchmark classes here
};

var summaries = new List<BenchmarkDotNet.Reports.Summary>();
foreach (var benchmarkType in benchmarkTypes)
{
    var summary = BenchmarkRunner.Run(benchmarkType);
    summaries.Add(summary);
}

// Generate markdown report
GenerateBenchmarkReport(summaries, "../BenchmarkResult.md");

static void GenerateBenchmarkReport(List<BenchmarkDotNet.Reports.Summary> summaries, string outputPath)
{
    var sb = new StringBuilder();
    sb.AppendLine("# Benchmark Results");
    sb.AppendLine();

    // Output results per benchmark class
    foreach (var summary in summaries)
    {
        var benchmarkClassName = summary.BenchmarksCases.FirstOrDefault()?.Descriptor.Type.Name ?? "Unknown";
        sb.AppendLine($"## {benchmarkClassName}");
        sb.AppendLine();

        // Load the GitHub-formatted Markdown report
        var artifactsPath = Path.Combine("BenchmarkDotNet.Artifacts", "results");
        var reportFileName = $"{summary.Title}-report-github.md";
        var reportFilePath = Path.Combine(artifactsPath, reportFileName);

        if (File.Exists(reportFilePath))
        {
            var reportContent = File.ReadAllText(reportFilePath);
            // Remove parts outside code blocks and extract only the table section
            var lines = reportContent.Split('\n');
            var inCodeBlock = false;
            var tableStarted = false;
            
            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("```"))
                {
                    inCodeBlock = !inCodeBlock;
                    continue;
                }
                
                if (inCodeBlock)
                {
                    if (line.Trim().StartsWith('|') || line.Trim().StartsWith("BenchmarkDotNet"))
                    {
                        if (!tableStarted && line.Trim().StartsWith('|'))
                        {
                            tableStarted = true;
                        }
                        if (tableStarted || line.Trim().StartsWith("BenchmarkDotNet"))
                        {
                            sb.AppendLine(line);
                        }
                    }
                    else if (tableStarted && string.IsNullOrWhiteSpace(line))
                    {
                        // End of table
                        break;
                    }
                }
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"*Report file not found: {reportFilePath}*");
            sb.AppendLine();
        }
    }

    // Output environment information (from the first summary)
    if (summaries.Count > 0)
    {
        sb.AppendLine("## Benchmark Environment");
        sb.AppendLine();
        var summary = summaries[0];
        sb.AppendLine($"```");
        sb.AppendLine(string.Join(System.Environment.NewLine, summary.HostEnvironmentInfo.ToFormattedString()));
        sb.AppendLine($"```");
    }

    File.WriteAllText(outputPath, sb.ToString());
    System.Console.WriteLine($"Benchmark report generated: {Path.GetFullPath(outputPath)}");
}
