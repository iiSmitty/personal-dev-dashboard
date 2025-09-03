using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalDevDashboard.McpServer.Services;
using PersonalDevDashboard.McpServer.Models;
using Octokit;

namespace PersonalDevDashboard.McpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            builder.Configuration.AddJsonFile("appsettings.json", optional: false);

            var githubToken = builder.Configuration["GitHub:Token"];
            var githubUsername = builder.Configuration["GitHub:Username"];

            if (string.IsNullOrEmpty(githubToken) || string.IsNullOrEmpty(githubUsername))
            {
                Console.WriteLine("Please configure GitHub token and username in appsettings.json");
                return;
            }

            var githubClient = new GitHubClient(new ProductHeaderValue("PersonalDevDashboard"));
            githubClient.Credentials = new Credentials(githubToken);

            builder.Services.AddSingleton(githubClient);
            builder.Services.AddSingleton<GitHubService>(provider => new GitHubService(githubToken, githubUsername));
            builder.Services.AddSingleton<FileDownloadService>();
            builder.Services.AddSingleton<HtmlAnalysisService>();

            var host = builder.Build();

            Console.WriteLine("🚀 Personal Dev Dashboard - Phase 2: HTML Analysis Engine");
            Console.WriteLine("Analyzing HTML structure and generating insights...\n");

            var githubService = host.Services.GetRequiredService<GitHubService>();
            var fileDownloadService = host.Services.GetRequiredService<FileDownloadService>();
            var htmlAnalysisService = host.Services.GetRequiredService<HtmlAnalysisService>();

            // Get repositories and analyze HTML files
            var repos = await githubService.GetPublicRepositoriesAsync();
            var staticRepos = repos.Where(r => r.IsStatic).Take(3).ToList();

            if (!staticRepos.Any())
            {
                staticRepos = repos.Take(2).ToList();
            }

            var allAnalysisResults = new List<HtmlAnalysisResult>();

            foreach (var repo in staticRepos)
            {
                Console.WriteLine($"📁 Analyzing HTML in {repo.Name}...");

                var files = await fileDownloadService.GetWebFilesFromRepositoryAsync(githubUsername, repo.Name);
                var htmlFiles = files.Where(f => f.Type == FileType.Html).ToList();

                if (!htmlFiles.Any())
                {
                    Console.WriteLine($"   ⚠️  No HTML files found in {repo.Name}\n");
                    continue;
                }

                Console.WriteLine($"   Found {htmlFiles.Count} HTML file(s):");

                foreach (var htmlFile in htmlFiles)
                {
                    Console.WriteLine($"   📄 Analyzing {htmlFile.Name}...");

                    var analysis = htmlAnalysisService.AnalyzeHtmlFile(htmlFile);
                    allAnalysisResults.Add(analysis);

                    // Display key metrics
                    var m = analysis.Metrics;
                    Console.WriteLine($"      Semantic Elements: {m.SemanticElementsUsed.Count} types used ({string.Join(", ", m.SemanticElementsUsed)})");
                    Console.WriteLine($"      Semantic Ratio: {m.SemanticRatio:F1}%");
                    Console.WriteLine($"      Alt Text Coverage: {m.AltTagCoverage:F1}% ({m.TotalImages - m.ImagesWithoutAlt}/{m.TotalImages} images)");
                    Console.WriteLine($"      Has Main Element: {(m.UsesMainElement ? "✅" : "❌")}");
                    Console.WriteLine($"      Proper Heading Hierarchy: {(m.HasProperHeadingHierarchy ? "✅" : "❌")}");

                    // Show top issues
                    if (analysis.Issues.Any())
                    {
                        Console.WriteLine($"      Issues Found: {analysis.Issues.Count}");
                        foreach (var issue in analysis.Issues.Take(3))
                        {
                            var icon = issue.Severity switch
                            {
                                IssueSeverity.Critical => "🔴",
                                IssueSeverity.Error => "🟠",
                                IssueSeverity.Warning => "🟡",
                                _ => "🔵"
                            };
                            Console.WriteLine($"        {icon} {issue.Description}");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"      Issues Found: 0 ✅");
                    }

                    Console.WriteLine();
                }
            }

            // Generate overall insights
            if (allAnalysisResults.Any())
            {
                Console.WriteLine("📊 OVERALL INSIGHTS ACROSS ALL REPOSITORIES:");
                GenerateOverallInsights(allAnalysisResults);
            }

            Console.WriteLine("\n🎉 Step 2 Complete! HTML Analysis Engine is working.");
            Console.WriteLine("Ready for Step 3: Advanced Insights & Reporting");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void GenerateOverallInsights(List<HtmlAnalysisResult> results)
        {
            var htmlFiles = results.Count;
            var avgSemanticRatio = results.Average(r => r.Metrics.SemanticRatio);
            var avgAltCoverage = results.Where(r => r.Metrics.TotalImages > 0).DefaultIfEmpty().Average(r => r?.Metrics.AltTagCoverage ?? 0);
            var filesWithMainElement = results.Count(r => r.Metrics.UsesMainElement);
            var filesWithProperHeadings = results.Count(r => r.Metrics.HasProperHeadingHierarchy);

            var allSemanticElements = results
                .SelectMany(r => r.Metrics.SemanticElementsUsed)
                .GroupBy(e => e)
                .OrderByDescending(g => g.Count())
                .Take(5)
                .ToList();

            Console.WriteLine($"   📈 Analyzed {htmlFiles} HTML files across {results.Select(r => r.Repository).Distinct().Count()} repositories");
            Console.WriteLine($"   📊 Average Semantic HTML Usage: {avgSemanticRatio:F1}%");
            Console.WriteLine($"   🖼️  Average Alt Text Coverage: {avgAltCoverage:F1}%");
            Console.WriteLine($"   🎯 Files using <main> element: {filesWithMainElement}/{htmlFiles} ({(double)filesWithMainElement / htmlFiles * 100:F1}%)");
            Console.WriteLine($"   📚 Files with proper heading hierarchy: {filesWithProperHeadings}/{htmlFiles} ({(double)filesWithProperHeadings / htmlFiles * 100:F1}%)");

            if (allSemanticElements.Any())
            {
                Console.WriteLine($"   🏷️  Most used semantic elements:");
                foreach (var element in allSemanticElements)
                {
                    Console.WriteLine($"      • {element.Key}: used in {element.Count()} files");
                }
            }

            // Generate actionable recommendations
            Console.WriteLine($"\n💡 TOP RECOMMENDATIONS:");
            if (avgSemanticRatio < 30)
                Console.WriteLine($"   🎯 Focus on semantic HTML - currently at {avgSemanticRatio:F1}%, aim for 40%+");

            if (avgAltCoverage < 90)
                Console.WriteLine($"   ♿ Improve accessibility - alt text coverage at {avgAltCoverage:F1}%, aim for 100%");

            if (filesWithMainElement < htmlFiles)
                Console.WriteLine($"   🎯 Add <main> elements - missing in {htmlFiles - filesWithMainElement} files");

            if (filesWithProperHeadings < htmlFiles)
                Console.WriteLine($"   📚 Fix heading hierarchy in {htmlFiles - filesWithProperHeadings} files");
        }
    }
}