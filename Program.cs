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

// Loop for requesting data
while (true)
{
	// Request repository name
	string repositoryName = UserInterface.RequestRepositoryName(client, repositoryOwner);

	// Request content type
	UserInterface.Content contentType = UserInterface.RequestContentType();

	// Fetch repository issues
	Console.Clear();
	string markdownText = "Something went wrong...";
	if (contentType is UserInterface.Content.Issues)
	{
		List<Issue> issues = Helper.FetchIssues(client, repositoryOwner, repositoryName);
		markdownText = Helper.IssuesToMarkdown(client, repositoryOwner, repositoryName, issues);
	}
	else if (contentType is UserInterface.Content.Milestones)
	{
		List<Milestone> milestones = Helper.FetchMilestones(client, repositoryOwner, repositoryName);
		markdownText = Helper.MilestonesToMarkdown(client, repositoryOwner, repositoryName, milestones);
	}

	// Initialize output path
	string outputPath = $"{Directory.GetCurrentDirectory()}\\Output";
	Directory.CreateDirectory(outputPath);

	// Export markdown
	Console.WriteLine("\nBeginning markdown export.");
	string markdownFileName = $"{repositoryName}_{contentType.ToString()}.md";
	string markdownFilePath = $"{outputPath}\\{markdownFileName}";
	File.WriteAllText(markdownFilePath, markdownText);
	Console.WriteLine($"Exported markdown to {markdownFilePath}");

	// Export PDF
	Console.WriteLine("\nBeginning PDF export.");
	QuestPDF.Settings.License = LicenseType.Community;
	Document document = Helper.MarkdownToPDF(markdownText);
	string pdfFileName = $"{repositoryName}_{contentType.ToString()}.pdf";
	string pdfFilePath = $"{outputPath}\\{pdfFileName}";
	document.GeneratePdf(pdfFilePath);
	Console.WriteLine($"Exported PDF to {pdfFilePath}");

	// Open output folder
	Console.WriteLine("\nOpen output folder?");
	Console.Write("Y/N: ");
	string openFolderInput = Console.ReadLine().ToUpper();
	if (openFolderInput == "Y")
		Process.Start("explorer.exe", outputPath);
}