using System;
using System.Linq;

if (args.Contains("--verify-analyser"))
{
    Environment.ExitCode = Centauri64.Analysis.AnalyserVerification.Run();
    return;
}

if (args.Contains("--verify-cover"))
{
    Environment.ExitCode = Centauri64.Analysis.CoverVerification.Run();
    return;
}

if (args.Contains("--verify-ghoule"))
{
    Environment.ExitCode = Centauri64.Analysis.GhouleVerification.Run();
    return;
}

if (args.Contains("--verify-basic-ux"))
{
    Environment.ExitCode = Centauri64.Analysis.BasicUxVerification.Run();
    return;
}

if (args.Contains("--verify-presentation"))
{
    Environment.ExitCode = Centauri64.Analysis.PresentationVerification.Run();
    return;
}

using var game = new Centauri64.Game1();
game.Run();
