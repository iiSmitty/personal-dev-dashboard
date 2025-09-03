namespace PersonalDevDashboard.McpServer.Models
{
    public class RepoInfo
    {
        public string Name { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string? Language { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long Size { get; set; } // Changed from int to long
        public bool IsStatic { get; set; }
        public string HtmlUrl { get; set; } = string.Empty;
    }
}