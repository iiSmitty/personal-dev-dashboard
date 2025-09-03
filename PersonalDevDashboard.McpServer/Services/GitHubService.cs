using Octokit;
using PersonalDevDashboard.McpServer.Models;

namespace PersonalDevDashboard.McpServer.Services
{
    public class GitHubService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly string _username;

        public GitHubService(string token, string username)
        {
            _gitHubClient = new GitHubClient(new ProductHeaderValue("PersonalDevDashboard"));
            _gitHubClient.Credentials = new Credentials(token);
            _username = username;
        }

        public async Task<List<RepoInfo>> GetPublicRepositoriesAsync()
        {
            try
            {
                var repositories = await _gitHubClient.Repository.GetAllForUser(_username);

                return repositories
                    .Where(repo => !repo.Private) // Only public repos
                    .Select(repo => new RepoInfo
                    {
                        Name = repo.Name,
                        FullName = repo.FullName,
                        Language = repo.Language,
                        Description = repo.Description,
                        UpdatedAt = repo.UpdatedAt.DateTime, // Fixed: removed the ? operator
                        Size = repo.Size, // Fixed: Size is now long in RepoInfo
                        HtmlUrl = repo.HtmlUrl,
                        IsStatic = IsStaticWebsite(repo)
                    })
                    .OrderByDescending(r => r.UpdatedAt)
                    .ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching repositories: {ex.Message}");
                return new List<RepoInfo>();
            }
        }

        private static bool IsStaticWebsite(Repository repo)
        {
            // Simple heuristic to detect static websites
            var staticIndicators = new[] { "html", "css", "javascript", "typescript" };
            return staticIndicators.Contains(repo.Language?.ToLower()) ||
                   repo.Name.Contains("website") ||
                   repo.Name.Contains("portfolio") ||
                   repo.HasPages == true;
        }
    }
}