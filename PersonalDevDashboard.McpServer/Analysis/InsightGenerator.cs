using PersonalDevDashboard.McpServer.Data;
using PersonalDevDashboard.McpServer.Models;
using Microsoft.EntityFrameworkCore;

namespace PersonalDevDashboard.McpServer.Analysis
{
    public class InsightGenerator
    {
        private readonly AnalysisContext _context;

        public InsightGenerator(AnalysisContext context)
        {
            _context = context;
        }

        public async Task<PortfolioInsights> GeneratePortfolioInsightsAsync(string username)
        {
            var latestSession = await _context.AnalysisSessions
                .Where(s => s.GitHubUsername == username)
                .OrderByDescending(s => s.CreatedAt)
                .Include(s => s.RepositoryAnalyses)
                    .ThenInclude(ra => ra.FileAnalyses)
                .FirstOrDefaultAsync();

            if (latestSession == null)
                return new PortfolioInsights { Message = "No analysis data found." };

            var insights = new PortfolioInsights
            {
                GeneratedAt = DateTime.UtcNow,
                Username = username,
                AnalysisDate = latestSession.CreatedAt
            };

            GenerateOverviewInsights(insights, latestSession);
            await GenerateSemanticInsights(insights, latestSession);
            GenerateAccessibilityInsights(insights, latestSession);
            GenerateStructureInsights(insights, latestSession);
            await GenerateTrendInsights(insights, username);
            GenerateRecommendations(insights, latestSession);

            return insights;
        }

        private void GenerateOverviewInsights(PortfolioInsights insights, AnalysisSession session)
        {
            var allFiles = session.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();

            insights.TotalRepositories = session.TotalRepositories;
            insights.TotalHtmlFiles = allFiles.Count;
            insights.AvgSemanticRatio = allFiles.Any() ? allFiles.Average(f => f.SemanticRatio) : 0;
            insights.AvgAltCoverage = allFiles.Where(f => f.TotalImages > 0).DefaultIfEmpty().Average(f => f?.AltTagCoverage ?? 0);

            // Calculate quality score (0-100)
            var structureScore = allFiles.Average(f => CalculateStructureScore(f));
            var semanticScore = Math.Min(insights.AvgSemanticRatio * 2, 100); // Cap at 100
            var accessibilityScore = insights.AvgAltCoverage;

            insights.OverallQualityScore = (structureScore + semanticScore + accessibilityScore) / 3;
        }

        private double CalculateStructureScore(FileAnalysisRecord file)
        {
            var score = 0.0;
            if (file.HasDoctype) score += 10;
            if (file.HasLangAttribute) score += 10;
            if (file.HasMetaCharset) score += 15;
            if (file.HasMetaViewport) score += 10;
            if (file.HasMetaDescription) score += 15;
            if (file.HasTitle) score += 15;
            if (file.HasProperHeadingHierarchy) score += 25;
            return score;
        }

        private async Task GenerateSemanticInsights(PortfolioInsights insights, AnalysisSession session)
        {
            var allFiles = session.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();

            insights.SemanticInsights = new SemanticInsights
            {
                FilesUsingMainElement = allFiles.Count(f => f.UsesMainElement),
                FilesUsingNavElement = allFiles.Count(f => f.UsesNavElement),
                FilesUsingHeaderElement = allFiles.Count(f => f.UsesHeaderElement),
                FilesUsingFooterElement = allFiles.Count(f => f.UsesFooterElement),
                AvgSemanticElementsPerFile = allFiles.Any() ? allFiles.Average(f => f.SemanticElementsCount) : 0,
                SemanticAdoptionTrend = await CalculateSemanticTrendAsync(session.GitHubUsername)
            };
        }

        private void GenerateAccessibilityInsights(PortfolioInsights insights, AnalysisSession session)
        {
            var allFiles = session.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();
            var filesWithImages = allFiles.Where(f => f.TotalImages > 0).ToList();

            insights.AccessibilityInsights = new AccessibilityInsights
            {
                TotalImages = allFiles.Sum(f => f.TotalImages),
                ImagesWithAltText = allFiles.Sum(f => f.TotalImages - f.ImagesWithoutAlt),
                FilesWithPerfectAltCoverage = filesWithImages.Count(f => f.AltTagCoverage >= 100),
                FilesWithProperHeadings = allFiles.Count(f => f.HasProperHeadingHierarchy),
                AccessibilityScore = CalculateAccessibilityScore(allFiles)
            };
        }

        private double CalculateAccessibilityScore(List<FileAnalysisRecord> files)
        {
            if (!files.Any()) return 0;

            var altScore = files.Where(f => f.TotalImages > 0).DefaultIfEmpty().Average(f => f?.AltTagCoverage ?? 100);
            var headingScore = (double)files.Count(f => f.HasProperHeadingHierarchy) / files.Count * 100;
            var structureScore = files.Average(f => (f.HasLangAttribute ? 50 : 0) + (f.HasTitle ? 50 : 0));

            return (altScore + headingScore + structureScore) / 3;
        }

        private void GenerateStructureInsights(PortfolioInsights insights, AnalysisSession session)
        {
            var allFiles = session.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();

            insights.StructureInsights = new StructureInsights
            {
                FilesWithDoctype = allFiles.Count(f => f.HasDoctype),
                FilesWithLangAttribute = allFiles.Count(f => f.HasLangAttribute),
                FilesWithMetaViewport = allFiles.Count(f => f.HasMetaViewport),
                FilesWithMetaDescription = allFiles.Count(f => f.HasMetaDescription),
                FilesWithTitle = allFiles.Count(f => f.HasTitle),
                StructuralConsistencyScore = CalculateConsistencyScore(allFiles)
            };
        }

        private double CalculateConsistencyScore(List<FileAnalysisRecord> files)
        {
            if (!files.Any()) return 100;

            var doctypeConsistency = files.Count(f => f.HasDoctype) / (double)files.Count * 100;
            var langConsistency = files.Count(f => f.HasLangAttribute) / (double)files.Count * 100;
            var viewportConsistency = files.Count(f => f.HasMetaViewport) / (double)files.Count * 100;
            var titleConsistency = files.Count(f => f.HasTitle) / (double)files.Count * 100;

            return (doctypeConsistency + langConsistency + viewportConsistency + titleConsistency) / 4;
        }

        private async Task<string> CalculateSemanticTrendAsync(string username)
        {
            var recentSessions = await _context.AnalysisSessions
                .Where(s => s.GitHubUsername == username)
                .OrderByDescending(s => s.CreatedAt)
                .Take(3)
                .Include(s => s.RepositoryAnalyses)
                    .ThenInclude(ra => ra.FileAnalyses)
                .ToListAsync();

            if (recentSessions.Count < 2) return "Insufficient data";

            var latest = recentSessions[0].RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).Average(f => f.SemanticRatio);
            var previous = recentSessions[1].RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).Average(f => f.SemanticRatio);

            var change = latest - previous;

            return change switch
            {
                > 5 => "📈 Improving",
                < -5 => "📉 Declining",
                _ => "➡️ Stable"
            };
        }

        private async Task GenerateTrendInsights(PortfolioInsights insights, string username)
        {
            var sessions = await _context.AnalysisSessions
                .Where(s => s.GitHubUsername == username)
                .OrderByDescending(s => s.CreatedAt)
                .Take(5)
                .Include(s => s.RepositoryAnalyses)
                    .ThenInclude(ra => ra.FileAnalyses)
                .ToListAsync();

            insights.TrendInsights = new TrendInsights();

            if (sessions.Count >= 2)
            {
                var latest = sessions[0];
                var previous = sessions[1];

                var latestFiles = latest.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();
                var previousFiles = previous.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();

                insights.TrendInsights.SemanticRatioChange = latestFiles.Average(f => f.SemanticRatio) -
                                                          previousFiles.Average(f => f.SemanticRatio);

                insights.TrendInsights.AccessibilityChange = latestFiles.Where(f => f.TotalImages > 0).DefaultIfEmpty()
                    .Average(f => f?.AltTagCoverage ?? 0) -
                    previousFiles.Where(f => f.TotalImages > 0).DefaultIfEmpty().Average(f => f?.AltTagCoverage ?? 0);

                insights.TrendInsights.OverallTrend = insights.TrendInsights.SemanticRatioChange +
                                                    insights.TrendInsights.AccessibilityChange > 10 ? "Improving" :
                                                    insights.TrendInsights.SemanticRatioChange +
                                                    insights.TrendInsights.AccessibilityChange < -10 ? "Declining" : "Stable";
            }
        }

        private void GenerateRecommendations(PortfolioInsights insights, AnalysisSession session)
        {
            var recommendations = new List<string>();
            var allFiles = session.RepositoryAnalyses.SelectMany(ra => ra.FileAnalyses).ToList();

            if (insights.AvgSemanticRatio < 25)
                recommendations.Add($"🎯 Priority: Increase semantic HTML usage (currently {insights.AvgSemanticRatio:F1}%, target: 40%+)");

            if (insights.AvgAltCoverage < 90)
                recommendations.Add($"♿ Priority: Improve alt text coverage (currently {insights.AvgAltCoverage:F1}%, target: 100%)");

            var filesWithoutMain = allFiles.Count(f => !f.UsesMainElement);
            if (filesWithoutMain > 0)
                recommendations.Add($"📝 Add <main> elements to {filesWithoutMain} files for better structure");

            var filesWithoutMeta = allFiles.Count(f => !f.HasMetaDescription);
            if (filesWithoutMeta > 0)
                recommendations.Add($"🔍 Add meta descriptions to {filesWithoutMeta} files for SEO");

            var filesWithHeadingIssues = allFiles.Count(f => !f.HasProperHeadingHierarchy);
            if (filesWithHeadingIssues > 0)
                recommendations.Add($"📚 Fix heading hierarchy in {filesWithHeadingIssues} files");

            if (insights.OverallQualityScore >= 80)
                recommendations.Add("🏆 Excellent work! Your HTML quality is above average");
            else if (insights.OverallQualityScore >= 60)
                recommendations.Add("👍 Good progress! Focus on the recommendations above to reach excellence");
            else
                recommendations.Add("📈 Room for improvement! Start with semantic HTML and accessibility basics");

            insights.TopRecommendations = recommendations;
        }
    }

    // Insight Models
    public class PortfolioInsights
    {
        public DateTime GeneratedAt { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime AnalysisDate { get; set; }
        public string Message { get; set; } = string.Empty;

        // Overview
        public int TotalRepositories { get; set; }
        public int TotalHtmlFiles { get; set; }
        public double AvgSemanticRatio { get; set; }
        public double AvgAltCoverage { get; set; }
        public double OverallQualityScore { get; set; }

        // Detailed Insights
        public SemanticInsights SemanticInsights { get; set; } = new();
        public AccessibilityInsights AccessibilityInsights { get; set; } = new();
        public StructureInsights StructureInsights { get; set; } = new();
        public TrendInsights TrendInsights { get; set; } = new();

        public List<string> TopRecommendations { get; set; } = new();
    }

    public class SemanticInsights
    {
        public int FilesUsingMainElement { get; set; }
        public int FilesUsingNavElement { get; set; }
        public int FilesUsingHeaderElement { get; set; }
        public int FilesUsingFooterElement { get; set; }
        public double AvgSemanticElementsPerFile { get; set; }
        public string SemanticAdoptionTrend { get; set; } = string.Empty;
    }

    public class AccessibilityInsights
    {
        public int TotalImages { get; set; }
        public int ImagesWithAltText { get; set; }
        public int FilesWithPerfectAltCoverage { get; set; }
        public int FilesWithProperHeadings { get; set; }
        public double AccessibilityScore { get; set; }
    }

    public class StructureInsights
    {
        public int FilesWithDoctype { get; set; }
        public int FilesWithLangAttribute { get; set; }
        public int FilesWithMetaViewport { get; set; }
        public int FilesWithMetaDescription { get; set; }
        public int FilesWithTitle { get; set; }
        public double StructuralConsistencyScore { get; set; }
    }

    public class TrendInsights
    {
        public double SemanticRatioChange { get; set; }
        public double AccessibilityChange { get; set; }
        public string OverallTrend { get; set; } = "No data";
    }
}