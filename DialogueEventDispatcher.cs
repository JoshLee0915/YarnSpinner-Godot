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
        public delegate void OnDialogueStarted();

        [Signal]
        public delegate void OnDialogueComplete();

        [Signal]
        public delegate void OnDisplayOptions(Dictionary<int, string> options);

        [Signal]
        public delegate void OnDisplayLine(string line);

        private Action<int> _selectionCallback = null;
        private Action _continueCallback = null;
        
        public void DialogueStarted()
        {
            EmitSignal(nameof(OnDialogueStarted));
        }

        public void DialogueComplete()
        {
            EmitSignal(nameof(OnDialogueComplete));
        }

        public void DisplayOptions(OptionSet options, ILineLocalisationProvider localisationProvider, Action<int> onSelected)
        {
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

            return Dialogue.HandlerExecutionType.ContinueExecution;
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
            _continueCallback = null;
            return true;
        }

        public bool SelectOption(int optionId)
        {
            if (_selectionCallback == null)
                return false;

            _selectionCallback(optionId);
            _selectionCallback = null;
            return true;
        }
    }
}