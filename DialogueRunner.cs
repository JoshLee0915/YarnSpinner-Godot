using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Node = Yarn.Node;
using Object = Godot.Object;

namespace YarnSpinnerGodot
{
    public class DialogueRunner: Godot.Node, ILineLocalisationProvider
    {
        [Signal]
        public delegate void OnNodeStart(string nodeName);

        [Signal]
        public delegate void OnNodeComplete(string nodeName);
        
        [Signal]
        public delegate void OnDialogueComplete();

        public delegate void CommandHandler(IEnumerable<string> parameters);
        public delegate void BlockingCommandHandler(IEnumerable<string> parameters, Action onComplete);
        
        private bool _wasCompleteCalled = false;
        private readonly Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();
        private readonly Dictionary<string, BlockingCommandHandler> _blockingCommandHandlers =
            new Dictionary<string, BlockingCommandHandler>();

        [Export(PropertyHint.ResourceType, "YarnProgram")] 
        public YarnProgram[] yarnPrograms;

        [Export]
        public NodePath variableStorageNode;

        [Export] 
        public NodePath uiNode;
        
        [Export]
        public string startNode = Yarn.Dialogue.DEFAULT_START;
        
        [Export] 
        public bool startAutomatically = false;
        
        public bool IsDialogueRunning { get; protected set; }

        public string CurrentNodeName => Dialogue.currentNode;

        private IVariableStorage _variableStorage;

        public IVariableStorage VariableStorage
        {
            get
            {
                if (_variableStorage == null)
                {
                    Godot.Node node = GetNode(variableStorageNode);
                    if (node is IVariableStorage)
                        _variableStorage = node as IVariableStorage;
                    else
                        _variableStorage = new GDVariableStorage(node);
                }

                return _variableStorage;
            }
        }
            

        private IDialogueUI _dialogueUi;
        public IDialogueUI DialogueUi => _dialogueUi ?? (_dialogueUi = GetNode<IDialogueUI>(uiNode));

        private Dialogue _dialogue;
        public Dialogue Dialogue => _dialogue ?? (_dialogue = _CreateDialogue());

        public override void _Ready()
        {
            VariableStorage.ResetToDefaults();
            
            if (yarnPrograms != null && yarnPrograms.Length > 0) 
            {
                List<Program> compiledPrograms = new List<Program>();
                foreach (var program in yarnPrograms) 
                    compiledPrograms.Add(program.Program);

                Program combinedProgram = Program.Combine(compiledPrograms.ToArray());
                Dialogue.SetProgram(combinedProgram);
            }

            if (startAutomatically)
                Start();
        }

        public void Add(YarnProgram compiledYarnProgram)
        {
            Dialogue.AddProgram(compiledYarnProgram.Program);
        }

        public void Start()
        {
            Start(startNode);
        }
        
        public void Start(string startNode)
        {
            Dialogue.SetNode(startNode);
            IsDialogueRunning = true;
            ContinueDialogue();
        }

        public void Stop()
        {
            Dialogue.Stop();
            IsDialogueRunning = false;
        }

        public void ResetDialogue()
        {
            VariableStorage.ResetToDefaults();
            Start();
        }

        public void Clear()
        {
            if(IsDialogueRunning)
                throw new ApplicationException("You cannot clear the dialogue system while a dialogue is running.");
            Dialogue.UnloadAll();
        }

        public bool NodeExists(string nodeName)
        {
            return Dialogue.NodeExists(nodeName);
        }

        public IEnumerable<string> GetTagsForNode(string nodeName)
        {
            return Dialogue.GetTagsForNode(nodeName);
        }
        
        public string GetLocalisedTextForLine(Line line)
        {
            string localizedString = Tr(line.ID);
            if(localizedString == line.ID)
                return String.Empty;
            
            for (int i = 0; i < line.Substitutions.Length; i++) {
                string substitution = line.Substitutions[i];
                localizedString = localizedString.Replace("{" + i + "}", substitution);
            }

            // Apply in-line format functions
            localizedString = Dialogue.ExpandFormatFunctions(localizedString, localizedString);
            return localizedString;
        }
        
        protected void ContinueDialogue()
        {
            _wasCompleteCalled = true;
            Dialogue.Continue();
        }

        protected void SelectOption(int option)
        {
            Dialogue.SetSelectedOption(option);
            ContinueDialogue();
        }

        protected Dialogue.HandlerExecutionType ExecuteCommand(Command command)
        {
            _wasCompleteCalled = false;
            List<string> tokens = command.Text.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            if (tokens.Count == 0)
                return Dialogue.HandlerExecutionType.ContinueExecution;

            string commandToken = tokens[0];
            tokens.RemoveAt(0);
            
            // Standard functions take priority, so try to match them first
            (bool wasValidCommand, Dialogue.HandlerExecutionType executionType) = _TryDispatchCommand(commandToken, tokens);
            if (wasValidCommand)
                return executionType;

            // Check if we have enough tokens to try to look for a registered object
            if (tokens.Count > 1)
            {
                // Append the objects name to the command to create the new command name
                commandToken = $"{tokens[0]}::{commandToken}";
                tokens.RemoveAt(0);
                
                (wasValidCommand, executionType) = _TryDispatchCommand(commandToken, tokens);
                if (wasValidCommand)
                    return executionType;
            }
            
            return DialogueUi.ExecuteCommand(command, ContinueDialogue);
        }

        private (bool commandFound, Dialogue.HandlerExecutionType handlerExecutionType) _TryDispatchCommand(
            string commandName, IEnumerable<string> args)
        {
            if (_commandHandlers.TryGetValue(commandName, out CommandHandler call))
            {
                call(args);
                return (true, Dialogue.HandlerExecutionType.ContinueExecution);
            }

            if (_blockingCommandHandlers.TryGetValue(commandName, out BlockingCommandHandler blockingCall))
            {
                blockingCall(args, ContinueDialogue);
                return (true, Dialogue.HandlerExecutionType.PauseExecution);
            }

            return (false, Dialogue.HandlerExecutionType.ContinueExecution);
        }

        private Dialogue _CreateDialogue()
        {
            Dialogue dialogue = new Dialogue(VariableStorage)
            {
                LogDebugMessage = (message) => GD.Print(message),
                LogErrorMessage = (message) => GD.PrintErr(message),
                lineHandler = (line) => DialogueUi.DisplayLine(line, this, ContinueDialogue),
                commandHandler = ExecuteCommand,
                optionsHandler = (options) => DialogueUi.DisplayOptions(options, this, SelectOption),
                nodeStartHandler = (node) =>
                {
                    EmitSignal(nameof(OnNodeStart), node);
                    return Dialogue.HandlerExecutionType.ContinueExecution;
                },
                nodeCompleteHandler = (node) =>
                {
                    EmitSignal(nameof(OnNodeComplete), node);
                    return Dialogue.HandlerExecutionType.ContinueExecution;
                },
                dialogueCompleteHandler = () =>
                {
                    IsDialogueRunning = false;
                    EmitSignal(nameof(OnDialogueComplete));
                }
            };

            return dialogue;
        }
    }
}