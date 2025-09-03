namespace PersonalDevDashboard.McpServer.Models
{
    public class RepoFile
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Repository { get; set; } = string.Empty;
        public FileType Type { get; set; }
        public long Size { get; set; }
        public DateTime LastModified { get; set; }
    }

    public enum FileType
    {
        Html,
        Css,
        JavaScript,
        Other
    }

    public class FileAnalysisResult
    {
        public string Repository { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public FileType FileType { get; set; }
        public Dictionary<string, object> Metrics { get; set; } = new();
        public List<string> Issues { get; set; } = new();
        public List<string> Recommendations { get; set; } = new();
    }
}