using System;
using System.Linq;

if (args.Contains("--verify-analyser"))
{
    Environment.ExitCode = Centauri64.Analysis.AnalyserVerification.Run();
    return;
}

using var game = new Centauri64.Game1();
game.Run();
