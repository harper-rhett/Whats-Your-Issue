using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using Octokit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;

Console.WriteLine("Welcome to What's Your Issue!");

GitHubClient client = new(new ProductHeaderValue("WhatsYourIssue"));

string personalAccessToken = UserInterface.RequestPersonalAccessToken(client);
client.Credentials = new(personalAccessToken);

string repositoryOwner = UserInterface.RequestRepositoriesOwner(client);

RepositoryIssueRequest issueRequest = new()
{
	State = ItemStateFilter.All
};

while (true)
{
	string repositoryName = UserInterface.RequestRepositoryName(client, repositoryOwner);

	Console.WriteLine("\nFetching repository issues:");
	IReadOnlyList<Issue> issues = await client.Issue.GetAllForRepository(repositoryOwner, repositoryName, issueRequest);
	List<Issue> sanitizedIssues = new();
	foreach (Issue issue in issues)
	{
		if (issue.PullRequest != null || issue.State == ItemState.Closed) continue;
		Console.WriteLine($"- {issue.Title}");
		sanitizedIssues.Add(issue);
	}

	string outputPath = $"{Directory.GetCurrentDirectory()}\\Output";
	Directory.CreateDirectory(outputPath);

	Console.WriteLine("\nBeginning markdown export.");
	string markdownText = await Helper.IssuesToMarkdown(client, repositoryOwner, repositoryName, sanitizedIssues);
	string markdownFileName = $"{repositoryName}_Issues.md";
	string markdownFilePath = $"{outputPath}\\{markdownFileName}";
	File.WriteAllText(markdownFilePath, markdownText);
	Console.WriteLine($"Exported markdown to {markdownFilePath}");

	Console.WriteLine("\nBeginning PDF export.");

	QuestPDF.Settings.License = LicenseType.Community;
	Document document = Helper.MarkdownToPDF(markdownText);

	string pdfFileName = $"{repositoryName}_Issues.pdf";
	string pdfFilePath = $"{outputPath}\\{pdfFileName}";
	document.GeneratePdf(pdfFilePath);
	Console.WriteLine($"Exported PDF to {pdfFilePath}");

	Console.WriteLine("\nOpen output folder?");
	Console.Write("Y/N: ");
	string openFolderInput = Console.ReadLine();
	if (openFolderInput.ToUpper() == "Y")
		Process.Start("explorer.exe", outputPath);
}