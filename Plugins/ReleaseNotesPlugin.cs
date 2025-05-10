using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;

namespace SemanticKernelPlayground.Plugins;

public class ReleaseNotesPlugin
{
	[KernelFunction("generate_release_notes")]
	[Description("Generate release notes from a list of commit messages")]
	public string GenerateReleaseNotes(
		[Description("List of commit messages.")] List<string> commitMessages,
		[Description("Version number.")] string version = "1.0.0",
		[Description("Release date.")] string releaseDate = "2023-10-01")
	{
		StringBuilder releaseNotes = new StringBuilder();
		releaseNotes.AppendLine($"# Release Notes for Version {version}");
		releaseNotes.AppendLine($"Release Date: {releaseDate}");
		releaseNotes.AppendLine();
		releaseNotes.AppendLine("## Changes:");

		foreach (var message in commitMessages)
		{
			releaseNotes.AppendLine($"- {message}");
		}
		return releaseNotes.ToString();
	}
}
