// Custom entry point that bypasses MTP to avoid VSTest/MTP conflicts from transitive TUnit dependencies
return Xunit.Runner.InProc.SystemConsole.ConsoleRunner.Run(args).GetAwaiter().GetResult();
