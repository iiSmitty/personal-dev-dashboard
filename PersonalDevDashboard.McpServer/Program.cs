using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalDevDashboard.McpServer.Services;
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

            // Create GitHub client
            var githubClient = new GitHubClient(new ProductHeaderValue("PersonalDevDashboard"));
            githubClient.Credentials = new Credentials(githubToken);

            // Register services
            builder.Services.AddSingleton(githubClient);
            builder.Services.AddSingleton<GitHubService>(provider => new GitHubService(githubToken, githubUsername));
            builder.Services.AddSingleton<FileDownloadService>();

            var host = builder.Build();

            Console.WriteLine("🚀 Personal Dev Dashboard - Phase 2: File Analysis");
            Console.WriteLine("Testing file download capabilities...\n");

            var githubService = host.Services.GetRequiredService<GitHubService>();
            var fileDownloadService = host.Services.GetRequiredService<FileDownloadService>();

            // Get repositories
            var repos = await githubService.GetPublicRepositoriesAsync();
            var staticRepos = repos.Where(r => r.IsStatic).Take(3).ToList(); // Test with first 3 static sites

            if (!staticRepos.Any())
            {
                Console.WriteLine("No static website repositories found. Using first 2 repositories instead.");
                staticRepos = repos.Take(2).ToList();
            }

            Console.WriteLine($"Analyzing files from {staticRepos.Count} repositories:\n");

            foreach (var repo in staticRepos)
            {
                Console.WriteLine($"📁 Analyzing {repo.Name}...");

                var files = await fileDownloadService.GetWebFilesFromRepositoryAsync(githubUsername, repo.Name);

                Console.WriteLine($"   Found {files.Count} web files:");

                foreach (var file in files)
                {
                    var sizeKB = file.Size / 1024.0;
                    var contentPreview = file.Content.Length > 100
                        ? file.Content.Substring(0, 100) + "..."
                        : file.Content;

                    Console.WriteLine($"   📄 {file.Name} ({file.Type}) - {sizeKB:F1}KB");
                    Console.WriteLine($"       Path: {file.Path}");
                    Console.WriteLine($"       Preview: {contentPreview.Replace('\n', ' ').Replace('\r', ' ')}\n");
                }

                if (files.Any())
                {
                    Console.WriteLine($"   ✅ Successfully downloaded {files.Count} files from {repo.Name}");
                }
                else
                {
                    Console.WriteLine($"   ⚠️  No web files found in {repo.Name}");
                }

                Console.WriteLine();
            }

            Console.WriteLine("🎉 Step 1 Complete! File download service is working.");
            Console.WriteLine("Ready for Step 2: HTML Analysis Engine");
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}