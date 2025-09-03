using Octokit;
using PersonalDevDashboard.McpServer.Models;
using System.Text;

namespace PersonalDevDashboard.McpServer.Services
{
    public class FileDownloadService
    {
        private readonly GitHubClient _gitHubClient;
        private readonly HttpClient _httpClient;

        public FileDownloadService(GitHubClient gitHubClient)
        {
            _gitHubClient = gitHubClient;
            _httpClient = new HttpClient();
        }

        public async Task<List<RepoFile>> GetWebFilesFromRepositoryAsync(string owner, string repoName)
        {
            var files = new List<RepoFile>();

            try
            {
                // Get repository contents recursively
                var contents = await GetAllFilesRecursivelyAsync(owner, repoName, "");

                // Filter for web-related files
                var webFiles = contents.Where(IsWebFile).ToList();

                Console.WriteLine($"Found {webFiles.Count} web files in {repoName}");

                foreach (var file in webFiles.Take(10)) // Limit to 10 files per repo for now
                {
                    try
                    {
                        var content = await DownloadFileContentAsync(file.DownloadUrl);

                        files.Add(new RepoFile
                        {
                            Name = file.Name,
                            Path = file.Path,
                            Content = content,
                            Repository = repoName,
                            Type = GetFileType(file.Name),
                            Size = file.Size,
                            LastModified = DateTime.UtcNow // GitHub API doesn't provide last modified for contents
                        });

                        // Small delay to be nice to GitHub's API
                        await Task.Delay(100);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error downloading {file.Path}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing repository {repoName}: {ex.Message}");
            }

            return files;
        }

        private async Task<List<RepositoryContent>> GetAllFilesRecursivelyAsync(string owner, string repoName, string path)
        {
            var allFiles = new List<RepositoryContent>();

            try
            {
                // Get contents - use different method for root vs subdirectories
                IReadOnlyList<RepositoryContent> contents;
                if (string.IsNullOrEmpty(path))
                {
                    contents = await _gitHubClient.Repository.Content.GetAllContents(owner, repoName);
                }
                else
                {
                    contents = await _gitHubClient.Repository.Content.GetAllContents(owner, repoName, path);
                }

                foreach (var item in contents)
                {
                    if (item.Type.Value == ContentType.File)
                    {
                        allFiles.Add(item);
                    }
                    else if (item.Type.Value == ContentType.Dir && !ShouldSkipDirectory(item.Name))
                    {
                        // Recursively get files from subdirectories
                        var subFiles = await GetAllFilesRecursivelyAsync(owner, repoName, item.Path);
                        allFiles.AddRange(subFiles);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting contents from {path}: {ex.Message}");
            }

            return allFiles;
        }

        private async Task<string> DownloadFileContentAsync(string downloadUrl)
        {
            try
            {
                var response = await _httpClient.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var bytes = await response.Content.ReadAsByteArrayAsync();

                // Try to decode as UTF-8, fallback to ASCII if needed
                try
                {
                    return Encoding.UTF8.GetString(bytes);
                }
                catch
                {
                    return Encoding.ASCII.GetString(bytes);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading file content: {ex.Message}");
                return string.Empty;
            }
        }

        private static bool IsWebFile(RepositoryContent file)
        {
            if (file.Type.Value != ContentType.File) return false;

            var extension = Path.GetExtension(file.Name).ToLower();
            var webExtensions = new[] { ".html", ".htm", ".css", ".js", ".jsx", ".ts", ".tsx" };

            return webExtensions.Contains(extension);
        }

        private static bool ShouldSkipDirectory(string dirName)
        {
            var skipDirs = new[] { "node_modules", ".git", "dist", "build", ".next", ".nuxt", "vendor" };
            return skipDirs.Contains(dirName.ToLower());
        }

        private static FileType GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLower();

            return extension switch
            {
                ".html" or ".htm" => FileType.Html,
                ".css" => FileType.Css,
                ".js" or ".jsx" or ".ts" or ".tsx" => FileType.JavaScript,
                _ => FileType.Other
            };
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}