# SmartCommandLineParser

Helper for C# to parse command-line arguments. Support preparing help message.

Install nuget `SmartCommandLineParser`.



```c#

using System;
using SmartCommandLineParser;

class Program
{
	public static void Main(string[] args)
	{
		var options = new CommandLineOptions();

		options.AddOptional<bool>("isRecursive", false, new[] { "-r", "--recursive" }, "Search recursive. Default is false."); // several keys
		options.AddRequired<int>("count", "--count", "This is count."); // one key
		options.AddRepeatable<string>("emails", new[] { "-e", "--email" }, "User's email. Can be specified several times.");
		options.AddOptional<string>("file", "bin", help: "File path. Default is 'bin'."); // no keys

		try
		{
			options.Parse(args); // args example: [ "test", "-c", "10", "-r" ]
			RunProcess
			(
				options.Get<bool>("isRecursive"),
				options.Get<int>("count"),
				options.Get<List<string>>("emails")
			);
		}
		catch (Exception e)
		{
			Console.WriteLine("Error: " + e.GetMessage());
			Console.WriteLine(options.GetHelpMessage());
		}
	}
}

```


Help messsage for above example:
```
	-r, --recursive         Search recursive. Default is false.
	
	--count                 This is count.
	
	-e, --email             User's email. Can be specified several times.
	
	file                    File path. Default is 'bin'.
	
```
