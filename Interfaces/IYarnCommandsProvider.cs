using System.Collections.Generic;

namespace YarnSpinnerGodot.Interfaces
{
    public interface IYarnCommandsProvider
    {
        Dictionary<string, DialogueRunner.CommandHandler> YarnCommandHandlers { get; }
        Dictionary<string, DialogueRunner.BlockingCommandHandler> YarnBlockingCommandHandlers { get; }
    }
}