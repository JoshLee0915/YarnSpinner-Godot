using System;
using Godot;
using Godot.Collections;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Node = Godot.Node;

namespace YarnSpinnerGodot
{
    public class DialogueEventDispatcher: Node, IDialogueManager
    {
        [Signal]
        public delegate void OnDialogueStarted(string nodeName);

        [Signal]
        public delegate void OnDialogueComplete();

        [Signal]
        public delegate void OnDisplayOptions(Dictionary<int, string> options);

        [Signal]
        public delegate void OnDisplayLine(string line);

        private Action<int> _selectionCallback = null;
        private Action _continueCallback = null;
        
        public void DialogueStarted(string name)
        {
            EmitSignal(nameof(OnDialogueStarted), name);
        }

        public void DialogueComplete()
        {
            EmitSignal(nameof(OnDialogueComplete));
        }

        public void DisplayOptions(OptionSet options, ILineLocalisationProvider localisationProvider, Action<int> onSelected)
        {
            _continueCallback = null;
            Dictionary<int, string> optionsDictionary = new Dictionary<int, string>();
            foreach (OptionSet.Option option in options.Options)
                optionsDictionary.Add(option.ID, localisationProvider.GetLocalisedTextForLine(option.Line));

            _selectionCallback = onSelected;
            EmitSignal(nameof(OnDisplayOptions), optionsDictionary);
        }

        public Dialogue.HandlerExecutionType DisplayLine(Line line, ILineLocalisationProvider localisationProvider, Action onComplete)
        {
            _continueCallback = onComplete;
            string lineText = localisationProvider.GetLocalisedTextForLine(line);
            EmitSignal(nameof(OnDisplayLine), lineText);

            return Dialogue.HandlerExecutionType.PauseExecution;
        }

        public virtual Dialogue.HandlerExecutionType ExecuteCommand(Command command, Action onComplete)
        {
            throw new NotImplementedException();
        }

        public bool Continue()
        {
            if (_continueCallback == null)
                return false;

            _continueCallback();
            return true;
        }

        public bool SelectOption(int optionId)
        {
            if (_selectionCallback == null)
                return false;

            _selectionCallback(optionId);
            return true;
        }
    }
}