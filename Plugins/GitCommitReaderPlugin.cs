using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibGit2Sharp;
using Microsoft.SemanticKernel;
using Semver;

namespace SemanticKernelPlayground.Plugins;

public class GitCommitReaderPlugin
{
	private string _path = "";

	[KernelFunction("get_latest_commits")]
	[Description("Get the last N commits from a Git repository")]
	public List<string> GetLatestCommits(
		[Description("The number of commits to retrieve. Default is 5.")] int numberOfCommits = 5)
	{
		if (ValidatePath(_path))
		{
			using var repo = new Repository(_path);
			var branch = repo.Branches["master"] ?? repo.Head;

			List<string> commitMessages = [];

			foreach (var commit in branch.Commits.Take(numberOfCommits))
			{
				commitMessages.Add((commit.Author.When.DateTime + ": " + commit.Message).TrimEnd());
			}

			// debug output
			Console.WriteLine($"Last {numberOfCommits} commits:");
			foreach (var commitMessage in commitMessages)
			{
				Console.WriteLine(commitMessage);
			}

			return commitMessages;
		}
		else
		{
			Console.WriteLine("Path is not valid. See the output above.");
			return null;
		}
	}

	[KernelFunction("set_repositry_path")]
	[Description("Set the repo path for the GitCommitReaderPlugin")]
	public void SetRepositryPath(string path)
	{
		if (ValidatePath(path))
		{
			_path = path;
			Console.WriteLine($"Repository path set to: {_path}");
		}
		else
		{
			Console.WriteLine($"Path is not valid. See the output above.");
		}
	}

	private static bool ValidatePath(string path)
	{
		// check if path eists
		if (!System.IO.Directory.Exists(path))
		{
			Console.WriteLine($"Path does not exist: {path}");
			return false;
		}

		// check if path is a valid path
		if (string.IsNullOrWhiteSpace(path) || path.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
		{
			Console.WriteLine($"Path is not valid: {path}");
			return false;
		}

		// check if path is a directory
		if (!System.IO.Directory.Exists(path))
		{
			Console.WriteLine($"Path is not a directory: {path}");
			return false;
		}

		// check if path is a git repository
		if (!System.IO.Directory.Exists(System.IO.Path.Combine(path, ".git")))
		{
			Console.WriteLine($"Path is not a git repository: {path}");
			return false;
		}
		// check if path is a valid git repository
		try
		{
			using var repo = new Repository(path);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Path is not a valid git repository: {path}");
			Console.WriteLine(ex.Message);
			return false;
		}

		Console.WriteLine($"Path is valid: {path}");
		return true;
	}

	[KernelFunction("bump_semver_patch")]
	[Description("Searches for the latest SemVer tag in the repository, increments the patch number, and creates a new tag.")]
	public string BumpSemVerPatch()
	{
		using var repo = new Repository(_path);

		var semverTags = repo.Tags
			.Select(t =>
			{
				if (SemVersion.TryParse(t.FriendlyName, SemVersionStyles.Any, out var v))
					return v;
				return null;
			})
			.Where(v => v != null)
			.Cast<SemVersion>()
			.OrderByDescending(v => v)
			.ToList();

		SemVersion next;
		if (semverTags.Any())
		{
			var current = semverTags.First();
			next = new SemVersion(current.Major, current.Minor, current.Patch + 1);
		}
		else
		{
			next = new SemVersion(0, 0, 1);
		}

		repo.ApplyTag(next.ToString());
		var remote = repo.Network.Remotes["origin"];

		repo.Network.Push(remote, $"refs/tags/{next}", new PushOptions());

		return next.ToString();
	}
}
