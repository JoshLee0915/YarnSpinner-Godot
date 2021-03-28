using System;
using Yarn;

namespace YarnSpinnerGodot.Interfaces
{
    public interface IDialogueUI
    {
        void DialogueStarted();
        void DialogueComplete();
        void DisplayOptions(OptionSet options, ILineLocalisationProvider localisationProvider, Action<int> onSelected);
        Dialogue.HandlerExecutionType DisplayLine(Line line, ILineLocalisationProvider localisationProvider, Action onComplete);
        Dialogue.HandlerExecutionType ExecuteCommand(Command command, Action onComplete);
    }
}