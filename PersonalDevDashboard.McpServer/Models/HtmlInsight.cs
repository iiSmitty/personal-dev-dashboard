namespace PersonalDevDashboard.McpServer.Models
{
    public class HtmlAnalysisResult
    {
        public string Repository { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public HtmlMetrics Metrics { get; set; } = new();
        public List<HtmlIssue> Issues { get; set; } = new();
        public List<HtmlRecommendation> Recommendations { get; set; } = new();
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }

    public class HtmlMetrics
    {
        // Document Structure
        public bool HasDoctype { get; set; }
        public bool HasLangAttribute { get; set; }
        public bool HasMetaCharset { get; set; }
        public bool HasMetaViewport { get; set; }
        public bool HasMetaDescription { get; set; }
        public bool HasTitle { get; set; }

        // Semantic HTML
        public int SemanticElementsCount { get; set; }
        public List<string> SemanticElementsUsed { get; set; } = new();
        public bool UsesMainElement { get; set; }
        public bool UsesNavElement { get; set; }
        public bool UsesHeaderElement { get; set; }
        public bool UsesFooterElement { get; set; }
        public bool UsesSectionElements { get; set; }
        public bool UsesArticleElements { get; set; }

        // Accessibility
        public int ImagesWithoutAlt { get; set; }
        public int TotalImages { get; set; }
        public double AltTagCoverage => TotalImages > 0 ? (double)(TotalImages - ImagesWithoutAlt) / TotalImages * 100 : 0;

        // Heading Structure
        public List<int> HeadingLevels { get; set; } = new();
        public bool HasProperHeadingHierarchy { get; set; }
        public int TotalHeadings { get; set; }

        // General Stats
        public int TotalElements { get; set; }
        public int DivElements { get; set; }
        public double SemanticRatio => TotalElements > 0 ? (double)SemanticElementsCount / TotalElements * 100 : 0;
    }

    public class HtmlIssue
    {
        public string Type { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public IssueSeverity Severity { get; set; }
        public string? ElementPath { get; set; }
        public int? LineNumber { get; set; }
    }

    public class HtmlRecommendation
    {
        public string Category { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ExampleCode { get; set; }
        public RecommendationPriority Priority { get; set; }
    }

    public enum IssueSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }
}