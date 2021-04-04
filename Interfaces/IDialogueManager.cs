using System;
using Yarn;

namespace YarnSpinnerGodot.Interfaces
{
    public interface IDialogueManager
    {
        void DialogueStarted(string nodeName);
        void DialogueComplete();
        void DisplayOptions(OptionSet options, ILineLocalisationProvider localisationProvider, Action<int> onSelected);
        Dialogue.HandlerExecutionType DisplayLine(Line line, ILineLocalisationProvider localisationProvider, Action onComplete);
        Dialogue.HandlerExecutionType ExecuteCommand(Command command, Action onComplete);
    }
}