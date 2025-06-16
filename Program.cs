using System.Collections;
using System.IO;
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
	personalAccessToken = File.ReadAllText("Personal Access Token.txt");
	Console.WriteLine("\nGitHub Personal Access Token found on disc.");
}
catch (Exception)
{
	Console.WriteLine("\nNo GitHub Personal Access Token found on disc.");
}

while (personalAccessToken == null)
{
	Console.WriteLine("Please enter a GitHub Personal Access Token with \"repo\" scope enabled. You can generate one here: https://github.com/settings/tokens");
	string userInput = Console.ReadLine();
	try
	{
		client.Credentials = new(userInput);
		personalAccessToken = userInput;
		User user = await client.User.Current();
		Console.WriteLine("Personal Access Token accepted. Saving to disc...");
		File.WriteAllText("Personal Access Token.txt", personalAccessToken);
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

	Console.WriteLine("\nBeginning markdown export.");
	string markdownText = await Helper.IssuesToMarkdown(client, repositoryOwner, repositoryName, sanitizedIssues);
	File.WriteAllText($"{repositoryName} Issues.md", markdownText);
	Console.WriteLine("Finished markdown export.");

	Console.WriteLine("\nBeginning PDF export.");

	QuestPDF.Settings.License = LicenseType.Community;
	Document document = Helper.MarkdownToPDF(markdownText);

	document.GeneratePdf($"{repositoryName} Issues.pdf");
	Console.WriteLine("Finished PDF export.");
}