using HtmlAgilityPack;
using PersonalDevDashboard.McpServer.Models;

namespace PersonalDevDashboard.McpServer.Services
{
    public class HtmlAnalysisService
    {
        private static readonly string[] SemanticElements = {
            "article", "aside", "details", "figcaption", "figure", "footer",
            "header", "main", "mark", "nav", "section", "summary", "time"
        };

        public HtmlAnalysisResult AnalyzeHtmlFile(RepoFile htmlFile)
        {
            var result = new HtmlAnalysisResult
            {
                Repository = htmlFile.Repository,
                FilePath = htmlFile.Path
            };

            if (string.IsNullOrEmpty(htmlFile.Content))
            {
                result.Issues.Add(new HtmlIssue
                {
                    Type = "EmptyFile",
                    Description = "HTML file is empty or could not be read",
                    Severity = IssueSeverity.Error
                });
                return result;
            }

            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlFile.Content);

                result.Metrics = AnalyzeDocument(doc);
                result.Issues = FindIssues(doc, result.Metrics);
                result.Recommendations = GenerateRecommendations(result.Metrics, result.Issues);

            }
            catch (Exception ex)
            {
                result.Issues.Add(new HtmlIssue
                {
                    Type = "ParseError",
                    Description = $"Failed to parse HTML: {ex.Message}",
                    Severity = IssueSeverity.Error
                });
            }

            return result;
        }

        private HtmlMetrics AnalyzeDocument(HtmlDocument doc)
        {
            var metrics = new HtmlMetrics();

            // Document structure analysis
            AnalyzeDocumentStructure(doc, metrics);

            // Semantic HTML analysis
            AnalyzeSemanticStructure(doc, metrics);

            // Accessibility analysis
            AnalyzeAccessibility(doc, metrics);

            // Heading structure analysis
            AnalyzeHeadingStructure(doc, metrics);

            // General statistics
            AnalyzeGeneralStats(doc, metrics);

            return metrics;
        }

        private void AnalyzeDocumentStructure(HtmlDocument doc, HtmlMetrics metrics)
        {
            // Check for DOCTYPE
            metrics.HasDoctype = doc.DocumentNode.ChildNodes
                .Any(n => n.NodeType == HtmlNodeType.Document ||
                         (n.NodeType == HtmlNodeType.Text && n.InnerText.Trim().StartsWith("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)));

            var htmlNode = doc.DocumentNode.SelectSingleNode("//html");
            if (htmlNode != null)
            {
                metrics.HasLangAttribute = htmlNode.GetAttributeValue("lang", "") != "";
            }

            var head = doc.DocumentNode.SelectSingleNode("//head");
            if (head != null)
            {
                metrics.HasMetaCharset = head.SelectSingleNode(".//meta[@charset]") != null;
                metrics.HasMetaViewport = head.SelectSingleNode(".//meta[@name='viewport']") != null;
                metrics.HasMetaDescription = head.SelectSingleNode(".//meta[@name='description']") != null;
                metrics.HasTitle = head.SelectSingleNode(".//title") != null;
            }
        }

        private void AnalyzeSemanticStructure(HtmlDocument doc, HtmlMetrics metrics)
        {
            foreach (var element in SemanticElements)
            {
                var elements = doc.DocumentNode.SelectNodes($"//{element}");
                if (elements != null && elements.Count > 0)
                {
                    metrics.SemanticElementsUsed.Add(element);
                    metrics.SemanticElementsCount += elements.Count;

                    // Track specific important elements
                    switch (element)
                    {
                        case "main": metrics.UsesMainElement = true; break;
                        case "nav": metrics.UsesNavElement = true; break;
                        case "header": metrics.UsesHeaderElement = true; break;
                        case "footer": metrics.UsesFooterElement = true; break;
                        case "section": metrics.UsesSectionElements = true; break;
                        case "article": metrics.UsesArticleElements = true; break;
                    }
                }
            }
        }

        private void AnalyzeAccessibility(HtmlDocument doc, HtmlMetrics metrics)
        {
            // Analyze images and alt attributes
            var images = doc.DocumentNode.SelectNodes("//img");
            if (images != null)
            {
                metrics.TotalImages = images.Count;
                metrics.ImagesWithoutAlt = images.Count(img =>
                    string.IsNullOrEmpty(img.GetAttributeValue("alt", "")));
            }
        }

        private void AnalyzeHeadingStructure(HtmlDocument doc, HtmlMetrics metrics)
        {
            var headings = doc.DocumentNode.SelectNodes("//h1 | //h2 | //h3 | //h4 | //h5 | //h6");
            if (headings != null)
            {
                metrics.TotalHeadings = headings.Count;
                metrics.HeadingLevels = headings
                    .Select(h => int.Parse(h.Name.Substring(1)))
                    .OrderBy(level => level)
                    .ToList();

                // Check for proper hierarchy (simplified check)
                metrics.HasProperHeadingHierarchy = CheckHeadingHierarchy(metrics.HeadingLevels);
            }
        }

        private void AnalyzeGeneralStats(HtmlDocument doc, HtmlMetrics metrics)
        {
            var allElements = doc.DocumentNode.SelectNodes("//*");
            if (allElements != null)
            {
                metrics.TotalElements = allElements.Count;
                metrics.DivElements = doc.DocumentNode.SelectNodes("//div")?.Count ?? 0;
            }
        }

        private bool CheckHeadingHierarchy(List<int> headingLevels)
        {
            if (!headingLevels.Any()) return true;

            // Should start with h1
            if (headingLevels.First() != 1) return false;

            // Check for gaps larger than 1
            for (int i = 1; i < headingLevels.Count; i++)
            {
                if (headingLevels[i] - headingLevels[i - 1] > 1)
                    return false;
            }

            return true;
        }

        private List<HtmlIssue> FindIssues(HtmlDocument doc, HtmlMetrics metrics)
        {
            var issues = new List<HtmlIssue>();

            // Document structure issues
            if (!metrics.HasDoctype)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingDoctype",
                    Description = "Missing DOCTYPE declaration",
                    Severity = IssueSeverity.Warning
                });

            if (!metrics.HasLangAttribute)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingLangAttribute",
                    Description = "Missing lang attribute on <html> element",
                    Severity = IssueSeverity.Warning
                });

            if (!metrics.HasMetaCharset)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingCharset",
                    Description = "Missing charset meta tag",
                    Severity = IssueSeverity.Error
                });

            if (!metrics.HasMetaViewport)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingViewport",
                    Description = "Missing viewport meta tag for responsive design",
                    Severity = IssueSeverity.Warning
                });

            if (!metrics.HasTitle)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingTitle",
                    Description = "Missing <title> element",
                    Severity = IssueSeverity.Error
                });

            // Accessibility issues
            if (metrics.ImagesWithoutAlt > 0)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingAltText",
                    Description = $"{metrics.ImagesWithoutAlt} image(s) missing alt attributes",
                    Severity = IssueSeverity.Warning
                });

            // Semantic structure issues
            if (!metrics.UsesMainElement)
                issues.Add(new HtmlIssue
                {
                    Type = "MissingMainElement",
                    Description = "Consider using <main> element for primary content",
                    Severity = IssueSeverity.Info
                });

            // Heading hierarchy issues
            if (!metrics.HasProperHeadingHierarchy)
                issues.Add(new HtmlIssue
                {
                    Type = "ImproperHeadingHierarchy",
                    Description = "Heading hierarchy has gaps or doesn't start with h1",
                    Severity = IssueSeverity.Warning
                });

            return issues;
        }

        private List<HtmlRecommendation> GenerateRecommendations(HtmlMetrics metrics, List<HtmlIssue> issues)
        {
            var recommendations = new List<HtmlRecommendation>();

            // Semantic HTML recommendations
            if (metrics.SemanticRatio < 20)
            {
                recommendations.Add(new HtmlRecommendation
                {
                    Category = "Semantic HTML",
                    Title = "Increase semantic HTML usage",
                    Description = $"Only {metrics.SemanticRatio:F1}% of your elements are semantic. Consider replacing generic divs with semantic elements.",
                    ExampleCode = "<main>\n  <article>\n    <header><h1>Title</h1></header>\n    <section>Content</section>\n  </article>\n</main>",
                    Priority = RecommendationPriority.Medium
                });
            }

            // Accessibility recommendations
            if (metrics.AltTagCoverage < 100 && metrics.TotalImages > 0)
            {
                recommendations.Add(new HtmlRecommendation
                {
                    Category = "Accessibility",
                    Title = "Improve image accessibility",
                    Description = $"Alt text coverage: {metrics.AltTagCoverage:F1}%. Add descriptive alt attributes to all images.",
                    ExampleCode = "<img src=\"photo.jpg\" alt=\"A sunset over the mountains with orange and pink clouds\">",
                    Priority = RecommendationPriority.High
                });
            }

            // Structure recommendations
            if (issues.Any(i => i.Type == "MissingMetaDescription"))
            {
                recommendations.Add(new HtmlRecommendation
                {
                    Category = "SEO",
                    Title = "Add meta description",
                    Description = "Meta descriptions improve SEO and social sharing.",
                    ExampleCode = "<meta name=\"description\" content=\"A brief description of your page content\">",
                    Priority = RecommendationPriority.Medium
                });
            }

            return recommendations;
        }
    }
}