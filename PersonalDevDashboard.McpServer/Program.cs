using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalDevDashboard.McpServer.Services;
using System.Text.Json;

namespace PersonalDevDashboard.McpServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);

            // Add configuration
            builder.Configuration.AddJsonFile("appsettings.json", optional: false);

            // Get GitHub settings
            var githubToken = builder.Configuration["GitHub:Token"];
            var githubUsername = builder.Configuration["GitHub:Username"];

            if (string.IsNullOrEmpty(githubToken) || string.IsNullOrEmpty(githubUsername))
            {
                Console.WriteLine("Please configure GitHub token and username in appsettings.json");
                return;
            }

            // Register services
            builder.Services.AddSingleton(new GitHubService(githubToken, githubUsername));

            var host = builder.Build();

            Console.WriteLine("🚀 Personal Dev Dashboard MCP Server starting...");
            Console.WriteLine("Testing GitHub connection...");

            // Test GitHub connection
            var githubService = host.Services.GetRequiredService<GitHubService>();
            var repos = await githubService.GetPublicRepositoriesAsync();

            Console.WriteLine($"✅ Successfully found {repos.Count} public repositories!");
            Console.WriteLine("\nYour repositories:");

            foreach (var repo in repos.Take(5)) // Show first 5
            {
                Console.WriteLine($"📁 {repo.Name}");
                Console.WriteLine($"   Language: {repo.Language ?? "N/A"}");
                Console.WriteLine($"   Updated: {repo.UpdatedAt:yyyy-MM-dd}");
                Console.WriteLine($"   Static Site: {(repo.IsStatic ? "Yes" : "Probably not")}");
                if (!string.IsNullOrEmpty(repo.Description))
                    Console.WriteLine($"   Description: {repo.Description}");
                Console.WriteLine();
            }

            if (repos.Count > 5)
            {
                Console.WriteLine($"... and {repos.Count - 5} more repositories");
            }

            Console.WriteLine("\n✨ Phase 1 Complete! Ready for Phase 2.");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}