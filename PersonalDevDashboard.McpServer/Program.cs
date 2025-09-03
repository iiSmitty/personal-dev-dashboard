using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalDevDashboard.McpServer.Services;
using PersonalDevDashboard.McpServer.Models;
using PersonalDevDashboard.McpServer.Data;
using PersonalDevDashboard.McpServer.Analysis;
using Microsoft.EntityFrameworkCore;
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

            // Register all services
            builder.Services.AddSingleton(githubClient);
            builder.Services.AddSingleton<GitHubService>(provider => new GitHubService(githubToken, githubUsername));
            builder.Services.AddSingleton<FileDownloadService>();
            builder.Services.AddSingleton<HtmlAnalysisService>();
            builder.Services.AddDbContext<AnalysisContext>();
            builder.Services.AddSingleton<DataPersistenceService>();
            builder.Services.AddSingleton<InsightGenerator>();

            var host = builder.Build();

            // Ensure database is created
            using (var scope = host.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AnalysisContext>();
                context.Database.EnsureCreated();
            }

            Console.WriteLine("🚀 Personal Dev Dashboard - Phase 2: Advanced Insights & Reporting");
            Console.WriteLine("Performing comprehensive analysis with persistent storage...\n");

            var githubService = host.Services.GetRequiredService<GitHubService>();
            var fileDownloadService = host.Services.GetRequiredService<FileDownloadService>();
            var htmlAnalysisService = host.Services.GetRequiredService<HtmlAnalysisService>();
            var persistenceService = host.Services.GetRequiredService<DataPersistenceService>();
            var insightGenerator = host.Services.GetRequiredService<InsightGenerator>();

            // Get repositories and perform analysis
            var repos = await githubService.GetPublicRepositoriesAsync();
            var staticRepos = repos.Where(r => r.IsStatic).Take(5).ToList(); // Analyze more repos now

            if (!staticRepos.Any())
            {
                Console.WriteLine("No static repositories found, analyzing all repositories...");
                staticRepos = repos.Take(3).ToList();
            }

            var allAnalysisResults = new List<HtmlAnalysisResult>();

            Console.WriteLine($"📊 Analyzing {staticRepos.Count} repositories for comprehensive insights...\n");

            foreach (var repo in staticRepos)
            {
                Console.WriteLine($"📁 Processing {repo.Name}...");

                var files = await fileDownloadService.GetWebFilesFromRepositoryAsync(githubUsername, repo.Name);
                var htmlFiles = files.Where(f => f.Type == FileType.Html).ToList();

                if (!htmlFiles.Any())
                {
                    Console.WriteLine($"   ⚠️  No HTML files found\n");
                    continue;
                }

                Console.WriteLine($"   Analyzing {htmlFiles.Count} HTML file(s)...");

                foreach (var htmlFile in htmlFiles)
                {
                    var analysis = htmlAnalysisService.AnalyzeHtmlFile(htmlFile);
                    allAnalysisResults.Add(analysis);
                }

                Console.WriteLine($"   ✅ Completed analysis of {repo.Name}\n");
            }

            // Save all analysis data
            Console.WriteLine("💾 Saving analysis data to database...");
            await persistenceService.SaveAnalysisSessionAsync(githubUsername, staticRepos, allAnalysisResults);

            // Generate advanced insights
            Console.WriteLine("\n🧠 Generating advanced insights...");
            var portfolioInsights = await insightGenerator.GeneratePortfolioInsightsAsync(githubUsername);

            // Display comprehensive report
            DisplayAdvancedReport(portfolioInsights);

            Console.WriteLine("\n🎉 Phase 2 Complete! Your Personal Development Dashboard is fully functional.");
            Console.WriteLine("💡 Data is now stored locally and will track your progress over time.");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }

        static void DisplayAdvancedReport(PortfolioInsights insights)
        {
            Console.WriteLine(new string('=', 80));
            Console.WriteLine("📊 PERSONAL DEVELOPMENT DASHBOARD - COMPREHENSIVE REPORT");
            Console.WriteLine(new string('=', 80));
            Console.WriteLine($"Generated: {insights.GeneratedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"Developer: {insights.Username}");
            Console.WriteLine($"Analysis Date: {insights.AnalysisDate:yyyy-MM-dd}\n");

            // Quality Score Header
            var scoreColor = insights.OverallQualityScore >= 80 ? "🟢" :
                           insights.OverallQualityScore >= 60 ? "🟡" : "🔴";
            Console.WriteLine($"{scoreColor} OVERALL QUALITY SCORE: {insights.OverallQualityScore:F1}/100");
            Console.WriteLine(new string('─', 50));

            // Portfolio Overview
            Console.WriteLine("📈 PORTFOLIO OVERVIEW");
            Console.WriteLine($"   Repositories Analyzed: {insights.TotalRepositories}");
            Console.WriteLine($"   HTML Files Processed: {insights.TotalHtmlFiles}");
            Console.WriteLine($"   Average Semantic Ratio: {insights.AvgSemanticRatio:F1}%");
            Console.WriteLine($"   Average Alt Text Coverage: {insights.AvgAltCoverage:F1}%\n");

            // Semantic HTML Insights
            Console.WriteLine("🏷️ SEMANTIC HTML ANALYSIS");
            var semantic = insights.SemanticInsights;
            Console.WriteLine($"   <main> element usage: {semantic.FilesUsingMainElement}/{insights.TotalHtmlFiles} files ({(double)semantic.FilesUsingMainElement / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   <nav> element usage: {semantic.FilesUsingNavElement}/{insights.TotalHtmlFiles} files ({(double)semantic.FilesUsingNavElement / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   <header> element usage: {semantic.FilesUsingHeaderElement}/{insights.TotalHtmlFiles} files ({(double)semantic.FilesUsingHeaderElement / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   <footer> element usage: {semantic.FilesUsingFooterElement}/{insights.TotalHtmlFiles} files ({(double)semantic.FilesUsingFooterElement / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Avg semantic elements per file: {semantic.AvgSemanticElementsPerFile:F1}");
            Console.WriteLine($"   Trend: {semantic.SemanticAdoptionTrend}\n");

            // Accessibility Insights
            Console.WriteLine("♿ ACCESSIBILITY ANALYSIS");
            var accessibility = insights.AccessibilityInsights;
            Console.WriteLine($"   Total Images: {accessibility.TotalImages}");
            Console.WriteLine($"   Images with Alt Text: {accessibility.ImagesWithAltText}/{accessibility.TotalImages} ({(accessibility.TotalImages > 0 ? (double)accessibility.ImagesWithAltText / accessibility.TotalImages * 100 : 0):F1}%)");
            Console.WriteLine($"   Files with Perfect Alt Coverage: {accessibility.FilesWithPerfectAltCoverage}");
            Console.WriteLine($"   Files with Proper Headings: {accessibility.FilesWithProperHeadings}/{insights.TotalHtmlFiles}");
            Console.WriteLine($"   Accessibility Score: {accessibility.AccessibilityScore:F1}/100\n");

            // Structure Insights
            Console.WriteLine("🏗️ DOCUMENT STRUCTURE ANALYSIS");
            var structure = insights.StructureInsights;
            Console.WriteLine($"   DOCTYPE declarations: {structure.FilesWithDoctype}/{insights.TotalHtmlFiles} ({(double)structure.FilesWithDoctype / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Lang attributes: {structure.FilesWithLangAttribute}/{insights.TotalHtmlFiles} ({(double)structure.FilesWithLangAttribute / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Viewport meta tags: {structure.FilesWithMetaViewport}/{insights.TotalHtmlFiles} ({(double)structure.FilesWithMetaViewport / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Meta descriptions: {structure.FilesWithMetaDescription}/{insights.TotalHtmlFiles} ({(double)structure.FilesWithMetaDescription / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Title tags: {structure.FilesWithTitle}/{insights.TotalHtmlFiles} ({(double)structure.FilesWithTitle / insights.TotalHtmlFiles * 100:F1}%)");
            Console.WriteLine($"   Structural Consistency Score: {structure.StructuralConsistencyScore:F1}/100\n");

            // Trend Analysis
            Console.WriteLine("📈 TREND ANALYSIS");
            var trends = insights.TrendInsights;
            Console.WriteLine($"   Semantic Ratio Change: {trends.SemanticRatioChange:+0.0;-0.0;0.0}%");
            Console.WriteLine($"   Accessibility Change: {trends.AccessibilityChange:+0.0;-0.0;0.0}%");
            Console.WriteLine($"   Overall Trend: {trends.OverallTrend}\n");

            // Top Recommendations
            Console.WriteLine("💡 TOP RECOMMENDATIONS");
            for (int i = 0; i < insights.TopRecommendations.Count; i++)
            {
                Console.WriteLine($"   {i + 1}. {insights.TopRecommendations[i]}");
            }

            Console.WriteLine("\n" + new string('═', 80));

            // Progress Motivation
            if (insights.OverallQualityScore >= 80)
            {
                Console.WriteLine("🏆 OUTSTANDING! Your HTML quality is exceptional. Keep up the excellent work!");
            }
            else if (insights.OverallQualityScore >= 60)
            {
                Console.WriteLine("👍 GOOD PROGRESS! You're on the right track. Focus on the recommendations above.");
            }
            else
            {
                Console.WriteLine("📈 GREAT POTENTIAL! Every expert was once a beginner. Use these insights to level up!");
            }

            Console.WriteLine("💾 All data saved locally - run again to track your improvement over time!");
        }
    }
}