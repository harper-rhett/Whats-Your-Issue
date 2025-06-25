using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal static class UserInterface
{
	public static string RequestPersonalAccessToken(GitHubClient client)
	{
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
				User user = client.User.Current().Result;
				Console.WriteLine("Personal Access Token accepted. Saving to disc...");
				byte[] bytes = Encoding.UTF8.GetBytes(personalAccessToken);
				File.WriteAllBytes("PersonalAccessToken.bin", bytes);
			}
			catch (Exception)
			{
				Console.WriteLine("Something is wrong. Try again.");
			}
		}

		return personalAccessToken;
	}

	public static string RequestRepositoriesOwner(GitHubClient client)
	{
		string repositoryOwner = null;
		while (repositoryOwner == null)
		{
			Console.WriteLine("\nPlease enter the owner (user or organization) of the repositories you would like to export data from.");
			Console.Write("Owner Name: ");
			string userInput = Console.ReadLine();
			try
			{
				User user = client.User.Get(userInput).Result;
				repositoryOwner = userInput;
			}
			catch (Exception)
			{
				Console.WriteLine("Something is wrong. Try again.");
			}
		}

		return repositoryOwner;
	}

	public static string RequestRepositoryName(GitHubClient client, string repositoryOwner)
	{
		string repositoryName = null;
		while (repositoryName == null)
		{
			Console.WriteLine("\nPlease enter the name of a repository you would like to export issues from.");
			Console.Write("Repository Name: ");
			string userInput = Console.ReadLine();
			try
			{
				Repository repository = client.Repository.Get(repositoryOwner, userInput).Result;
				repositoryName = userInput;
			}
			catch (Exception)
			{
				Console.WriteLine("Something is wrong. Try again.");
			}
		}
		return repositoryName;
	}
}
