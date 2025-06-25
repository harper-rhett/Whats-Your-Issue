using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Octokit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;

// Initialize
Console.WriteLine("Welcome to What's Your Issue!");
GitHubClient client = new(new ProductHeaderValue("WhatsYourIssue"));

// Set up personal access token
string personalAccessToken = UserInterface.RequestPersonalAccessToken(client);
client.Credentials = new(personalAccessToken);

// Request repository owner
string repositoryOwner = UserInterface.RequestRepositoriesOwner(client);

// Issue request settings
RepositoryIssueRequest issueRequest = new()
{
	State = ItemStateFilter.Open,
};

// Loop for requesting data
while (true)
{
	// Request repository name
	string repositoryName = UserInterface.RequestRepositoryName(client, repositoryOwner);

	// Fetch repository issues
	Console.WriteLine("\nFetching repository issues:");
	IReadOnlyList<Issue> issues = await client.Issue.GetAllForRepository(repositoryOwner, repositoryName, issueRequest);
	List<Issue> sanitizedIssues = new();
	foreach (Issue issue in issues)
	{
		if (issue.PullRequest != null) continue;
		Console.WriteLine($"- {issue.Title}");
		sanitizedIssues.Add(issue);
	}

	// Initialize output path
	string outputPath = $"{Directory.GetCurrentDirectory()}\\Output";
	Directory.CreateDirectory(outputPath);

	// Export markdown
	Console.WriteLine("\nBeginning markdown export.");
	string markdownText = await Helper.IssuesToMarkdown(client, repositoryOwner, repositoryName, sanitizedIssues);
	string markdownFileName = $"{repositoryName}_Issues.md";
	string markdownFilePath = $"{outputPath}\\{markdownFileName}";
	File.WriteAllText(markdownFilePath, markdownText);
	Console.WriteLine($"Exported markdown to {markdownFilePath}");

	// Export PDF
	Console.WriteLine("\nBeginning PDF export.");
	QuestPDF.Settings.License = LicenseType.Community;
	Document document = Helper.MarkdownToPDF(markdownText);
	string pdfFileName = $"{repositoryName}_Issues.pdf";
	string pdfFilePath = $"{outputPath}\\{pdfFileName}";
	document.GeneratePdf(pdfFilePath);
	Console.WriteLine($"Exported PDF to {pdfFilePath}");

	// Open output folder
	Console.WriteLine("\nOpen output folder?");
	Console.Write("Y/N: ");
	string openFolderInput = Console.ReadLine();
	if (openFolderInput.ToUpper() == "Y")
		Process.Start("explorer.exe", outputPath);
}