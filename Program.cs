using System.Collections;
using System.IO;
using System.Text;
using Octokit;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QuestPDF.Markdown;

Console.WriteLine("Welcome to What's Your Issue!");

GitHubClient client = new(new ProductHeaderValue("WhatsYourIssue"));

string personalAccessToken = null;
try
{
	byte[] bytes = File.ReadAllBytes("PersonalAccessToken.bin");
	personalAccessToken = Encoding.UTF8.GetString(bytes);
	Console.WriteLine("\nGitHub Personal Access Token found on disc.");
}
catch (Exception)
{
	Console.WriteLine("\nNo GitHub Personal Access Token found on disc.");
}

while (personalAccessToken == null)
{
	Console.WriteLine("Please enter a GitHub Personal Access Token with \"repo\" scope enabled. You can generate one here:\nhttps://github.com/settings/tokens");
	Console.Write("Personal Access Token: ");
	string userInput = Console.ReadLine();
	try
	{
		client.Credentials = new(userInput);
		personalAccessToken = userInput;
		User user = await client.User.Current();
		Console.WriteLine("Personal Access Token accepted. Saving to disc...");
		byte[] bytes = Encoding.UTF8.GetBytes(personalAccessToken);
		File.WriteAllBytes("PersonalAccessToken.bin", bytes);
	}
	catch (Exception)
	{
		Console.WriteLine("Something is wrong. Try again.");
	}
}

client.Credentials = new(personalAccessToken);

string repositoryOwner = null;
while (repositoryOwner == null)
{
	Console.WriteLine("\nPlease enter the owner (user or organization) of the repositories you would like to export issues from.");
	Console.Write("Owner Name: ");
	string userInput = Console.ReadLine();
	try
	{
		await client.User.Get(userInput);
		repositoryOwner = userInput;
	}
	catch (Exception)
	{
		Console.WriteLine("Something is wrong. Try again.");
	}
}

RepositoryIssueRequest issueRequest = new()
{
	State = ItemStateFilter.All
};

while (true)
{
	string repositoryName = null;
	while (repositoryName == null)
	{
		Console.WriteLine("\nPlease enter the name of a repository you would like to export issues from.");
		Console.Write("Repository Name: ");
		string userInput = Console.ReadLine();
		try
		{
			await client.Repository.Get(repositoryOwner, userInput);
			repositoryName = userInput;
		}
		catch (Exception)
		{
			Console.WriteLine("Something is wrong. Try again.");
		}
	}

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
}