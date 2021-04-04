using System;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Object = Godot.Object;

namespace YarnSpinnerGodot.GDScript
{
    public class GDDialogueManager: GDObjectWrapper, IDialogueManager
    {
        protected override string[] Interface => new[]
        {
            nameof(DialogueStarted), nameof(DialogueComplete), nameof(DisplayOptions), nameof(DisplayLine),
            nameof(ExecuteCommand)
        };
        
        public GDDialogueManager(Object gdObject): base(gdObject)
        {}
        
        public void DialogueStarted()
        {
            gdObject.Call(nameof(DialogueStarted));
        }

        public void DialogueComplete()
        {
            gdObject.Call(nameof(DialogueComplete));
        }

        public void DisplayOptions(OptionSet options, ILineLocalisationProvider localisationProvider, Action<int> onSelected)
        {
            gdObject.Call(nameof(DisplayOptions), options, localisationProvider, onSelected);
        }

        public Dialogue.HandlerExecutionType DisplayLine(Line line, ILineLocalisationProvider localisationProvider, Action onComplete)
        {
            return (Dialogue.HandlerExecutionType)gdObject.Call(nameof(DisplayLine), line, localisationProvider, onComplete);
        }

        public Dialogue.HandlerExecutionType ExecuteCommand(Command command, Action onComplete)
        {
            return (Dialogue.HandlerExecutionType) gdObject.Call(nameof(ExecuteCommand), command, onComplete);
        }
    }
}