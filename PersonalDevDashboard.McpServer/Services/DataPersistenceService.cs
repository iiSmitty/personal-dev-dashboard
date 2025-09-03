using PersonalDevDashboard.McpServer.Data;
using PersonalDevDashboard.McpServer.Models;

namespace PersonalDevDashboard.McpServer.Services
{
    public class DataPersistenceService
    {
        private readonly AnalysisContext _context;

        public DataPersistenceService(AnalysisContext context)
        {
            _context = context;
        }

        public async Task SaveAnalysisSessionAsync(string username, List<RepoInfo> repositories, List<HtmlAnalysisResult> analysisResults)
        {
            var session = new AnalysisSession
            {
                CreatedAt = DateTime.UtcNow,
                GitHubUsername = username,
                TotalRepositories = repositories.Count,
                TotalHtmlFiles = analysisResults.Count
            };

            _context.AnalysisSessions.Add(session);
            await _context.SaveChangesAsync();

            // Group analysis results by repository
            var resultsByRepo = analysisResults.GroupBy(r => r.Repository).ToList();

            foreach (var repoGroup in resultsByRepo)
            {
                var repoInfo = repositories.FirstOrDefault(r => r.Name == repoGroup.Key);
                if (repoInfo == null) continue;

                var repoAnalysis = new RepositoryAnalysis
                {
                    SessionId = session.Id,
                    RepositoryName = repoInfo.Name,
                    Language = repoInfo.Language ?? "Unknown",
                    LastUpdated = repoInfo.UpdatedAt,
                    IsStatic = repoInfo.IsStatic,
                    HtmlFilesCount = repoGroup.Count()
                };

                _context.RepositoryAnalyses.Add(repoAnalysis);
                await _context.SaveChangesAsync();

                foreach (var analysis in repoGroup)
                {
                    var fileRecord = new FileAnalysisRecord
                    {
                        RepositoryAnalysisId = repoAnalysis.Id,
                        FilePath = analysis.FilePath,
                        FileName = Path.GetFileName(analysis.FilePath),
                        AnalyzedAt = analysis.AnalyzedAt,

                        // Flatten metrics for easy querying
                        HasDoctype = analysis.Metrics.HasDoctype,
                        HasLangAttribute = analysis.Metrics.HasLangAttribute,
                        HasMetaCharset = analysis.Metrics.HasMetaCharset,
                        HasMetaViewport = analysis.Metrics.HasMetaViewport,
                        HasMetaDescription = analysis.Metrics.HasMetaDescription,
                        HasTitle = analysis.Metrics.HasTitle,

                        SemanticElementsCount = analysis.Metrics.SemanticElementsCount,
                        SemanticRatio = analysis.Metrics.SemanticRatio,
                        UsesMainElement = analysis.Metrics.UsesMainElement,
                        UsesNavElement = analysis.Metrics.UsesNavElement,
                        UsesHeaderElement = analysis.Metrics.UsesHeaderElement,
                        UsesFooterElement = analysis.Metrics.UsesFooterElement,

                        TotalImages = analysis.Metrics.TotalImages,
                        ImagesWithoutAlt = analysis.Metrics.ImagesWithoutAlt,
                        AltTagCoverage = analysis.Metrics.AltTagCoverage,

                        TotalHeadings = analysis.Metrics.TotalHeadings,
                        HasProperHeadingHierarchy = analysis.Metrics.HasProperHeadingHierarchy,

                        TotalElements = analysis.Metrics.TotalElements,
                        IssuesCount = analysis.Issues.Count,
                        CriticalIssues = analysis.Issues.Count(i => i.Severity == IssueSeverity.Critical),
                        WarningIssues = analysis.Issues.Count(i => i.Severity == IssueSeverity.Warning)
                    };

                    _context.FileAnalysisRecords.Add(fileRecord);
                }
            }

            await _context.SaveChangesAsync();
            Console.WriteLine($"💾 Saved analysis data for {session.TotalHtmlFiles} files across {session.TotalRepositories} repositories");
        }
    }
}