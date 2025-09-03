using Microsoft.EntityFrameworkCore;
using PersonalDevDashboard.McpServer.Models;

namespace PersonalDevDashboard.McpServer.Data
{
    public class AnalysisContext : DbContext
    {
        public DbSet<AnalysisSession> AnalysisSessions { get; set; }
        public DbSet<RepositoryAnalysis> RepositoryAnalyses { get; set; }
        public DbSet<FileAnalysisRecord> FileAnalysisRecords { get; set; }
        public DbSet<MetricSnapshot> MetricSnapshots { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=personal_dev_dashboard.db");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships
            modelBuilder.Entity<RepositoryAnalysis>()
                .HasOne(ra => ra.Session)
                .WithMany(s => s.RepositoryAnalyses)
                .HasForeignKey(ra => ra.SessionId);

            modelBuilder.Entity<FileAnalysisRecord>()
                .HasOne(far => far.RepositoryAnalysis)
                .WithMany(ra => ra.FileAnalyses)
                .HasForeignKey(far => far.RepositoryAnalysisId);

            modelBuilder.Entity<MetricSnapshot>()
                .HasOne(ms => ms.FileAnalysis)
                .WithMany(fa => fa.MetricSnapshots)
                .HasForeignKey(ms => ms.FileAnalysisId);
        }
    }

    public class AnalysisSession
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string GitHubUsername { get; set; } = string.Empty;
        public int TotalRepositories { get; set; }
        public int TotalHtmlFiles { get; set; }
        public List<RepositoryAnalysis> RepositoryAnalyses { get; set; } = new();
    }

    public class RepositoryAnalysis
    {
        public int Id { get; set; }
        public int SessionId { get; set; }
        public string RepositoryName { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public DateTime LastUpdated { get; set; }
        public bool IsStatic { get; set; }
        public int HtmlFilesCount { get; set; }

        // Navigation properties
        public AnalysisSession Session { get; set; } = null!;
        public List<FileAnalysisRecord> FileAnalyses { get; set; } = new();
    }

    public class FileAnalysisRecord
    {
        public int Id { get; set; }
        public int RepositoryAnalysisId { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime AnalyzedAt { get; set; }

        // Metrics (flattened for easier querying)
        public bool HasDoctype { get; set; }
        public bool HasLangAttribute { get; set; }
        public bool HasMetaCharset { get; set; }
        public bool HasMetaViewport { get; set; }
        public bool HasMetaDescription { get; set; }
        public bool HasTitle { get; set; }

        public int SemanticElementsCount { get; set; }
        public double SemanticRatio { get; set; }
        public bool UsesMainElement { get; set; }
        public bool UsesNavElement { get; set; }
        public bool UsesHeaderElement { get; set; }
        public bool UsesFooterElement { get; set; }

        public int TotalImages { get; set; }
        public int ImagesWithoutAlt { get; set; }
        public double AltTagCoverage { get; set; }

        public int TotalHeadings { get; set; }
        public bool HasProperHeadingHierarchy { get; set; }

        public int TotalElements { get; set; }
        public int IssuesCount { get; set; }
        public int CriticalIssues { get; set; }
        public int WarningIssues { get; set; }

        // Navigation properties
        public RepositoryAnalysis RepositoryAnalysis { get; set; } = null!;
        public List<MetricSnapshot> MetricSnapshots { get; set; } = new();
    }

    public class MetricSnapshot
    {
        public int Id { get; set; }
        public int FileAnalysisId { get; set; }
        public string MetricName { get; set; } = string.Empty;
        public double MetricValue { get; set; }
        public string? StringValue { get; set; }
        public DateTime RecordedAt { get; set; }

        // Navigation property
        public FileAnalysisRecord FileAnalysis { get; set; } = null!;
    }
}