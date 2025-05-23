﻿using Microsoft.Extensions.Configuration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SemanticKernelPlayground.Plugins;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddJsonFile("appsettings.Development.json", optional: false, reloadOnChange: true)
    .Build();

var modelName = configuration["ModelName"] ?? throw new ApplicationException("ModelName not found");
var endpoint = configuration["Endpoint"] ?? throw new ApplicationException("Endpoint not found");
var apiKey = configuration["ApiKey"] ?? throw new ApplicationException("ApiKey not found");

var builder = Kernel.CreateBuilder()
    .AddAzureOpenAIChatCompletion(modelName, endpoint, apiKey);

builder.Plugins.AddFromType<GitCommitReaderPlugin>("GitCommitReaderPlugin");
builder.Plugins.AddFromPromptDirectory("C:\\Users\\Oleg\\source\\repos\\SemanticKernelPlayground\\Plugins\\PromptPlugins");

var kernel = builder.Build();

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

AzureOpenAIPromptExecutionSettings openAiPromptExecutionSettings = new()
{
    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
};

var history = new ChatHistory();
var systemPrompt = """
                    You are a console-based chat agent built on Microsoft Semantic Kernel that can load and invoke plugins — for example, 
                    to retrieve Git commits, generate new version and generate release notes. If a required plugin is not available, please politely inform the user and suggest an alternative.
                    """;

history.AddSystemMessage(systemPrompt);

#region debug methods
//var plugin = kernel.ImportPluginFromType<GitCommitReaderPlugin>("GitCommitReaderPlugin");
//if (plugin.TryGetFunction("set_repositry_path", out var setRepositryPath) && setRepositryPath != null)
//{
//    //await setRepositryPath.InvokeAsync(kernel, new KernelArguments { { "path", "c:\\Users\\Oleg\\source\\repos\\yolov10\\" } });
//}
//else
//{
//    throw new ApplicationException("Failed to retrieve the 'SetRepositryPath' function from the plugin.");
//}

//if (plugin.TryGetFunction("get_latest_commits", out var getLastCommitsFunction) && getLastCommitsFunction != null)
//{
//    //await getLastCommitsFunction.InvokeAsync(kernel, new KernelArguments { { "numberOfCommits", 5 } });
//}
//else
//{
//    throw new ApplicationException("Failed to retrieve the 'GetLastCommits' function from the plugin.");
//}
#endregion

do
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.Write("Me > ");
    Console.ResetColor();

	var userInput = Console.ReadLine();
    if (userInput == "exit")
    {
        break;
    }

    history.AddUserMessage(userInput!);

    var streamingResponse =
        chatCompletionService.GetStreamingChatMessageContentsAsync(
            history,
            openAiPromptExecutionSettings,
            kernel);

    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("Agent > ");
    Console.ResetColor();

    var fullResponse = "";
    await foreach (var chunk in streamingResponse)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write(chunk.Content);
        Console.ResetColor();
        fullResponse += chunk.Content;
    }
    Console.WriteLine();

    history.AddMessage(AuthorRole.Assistant, fullResponse);


} while (true);