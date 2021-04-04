using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;
using Yarn;
using YarnSpinnerGodot.Attributes;
using YarnSpinnerGodot.GDScript;
using YarnSpinnerGodot.Interfaces;
using Node = Godot.Node;
using Object = Godot.Object;

namespace YarnSpinnerGodot
{
	public class DialogueRunner: Node, ILineLocalisationProvider
	{
		[Signal]
		public delegate void OnNodeStart(string nodeName);

		[Signal]
		public delegate void OnNodeComplete(string nodeName);
		
		[Signal]
		public delegate void OnDialogueComplete();

		public delegate void CommandHandler(IEnumerable<string> parameters);
		public delegate void BlockingCommandHandler(IEnumerable<string> parameters, Action onComplete);
		
		private readonly Dictionary<string, CommandHandler> _commandHandlers = new Dictionary<string, CommandHandler>();
		private readonly Dictionary<string, BlockingCommandHandler> _blockingCommandHandlers =
			new Dictionary<string, BlockingCommandHandler>();

		[Export(PropertyHint.ResourceType, "YarnProgram")] 
		public YarnProgram[] yarnPrograms;

		[Export]
		public NodePath variableStorageNode;

		[Export] 
		public NodePath dialogueManagerNode;
		
		[Export]
		public string startNode = Yarn.Dialogue.DEFAULT_START;
		
		[Export] 
		public bool startAutomatically = false;

		[Export]
		public bool scanForCommandsOnStart = false;
		
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
			

		private IDialogueManager _dialogueManager;
		public IDialogueManager DialogueManager
		{
			get
			{
				if (_dialogue == null)
				{
					Godot.Node node = GetNode(dialogueManagerNode);
					_dialogueManager = node as IDialogueManager;
					if (_dialogueManager == null)
						_dialogueManager = new GDDialogueManager(node);

					Connect(nameof(OnNodeStart), (Object)_dialogueManager, nameof(_dialogueManager.DialogueStarted));
					Connect(nameof(OnDialogueComplete), (Object) _dialogueManager,
						nameof(_dialogueManager.DialogueComplete));
				}
				
				return _dialogueManager;
			}
		}

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
			
			if(scanForCommandsOnStart)
				ScanTreeForCommands();

			if (startAutomatically)
				Start();
		}

		public void Add(YarnProgram compiledYarnProgram)
		{
			Dialogue.AddProgram(compiledYarnProgram.Program);
		}

		public bool RegisterCommand(string commandName, CommandHandler command)
		{
			if (_commandHandlers.ContainsKey(commandName))
				return false;
			
			_commandHandlers.Add(commandName, command);
			return true;
		}
		
		public bool RegisterCommand(string commandName, string objectName, CommandHandler command)
		{
			commandName = $"{objectName.Replace(" ", string.Empty)}::${commandName}";
			if (_commandHandlers.ContainsKey(commandName))
				return false;
			
			_commandHandlers.Add(commandName, command);
			return true;
		}

		public bool RegisterCommand(string commandName, BlockingCommandHandler command)
		{
			if (_blockingCommandHandlers.ContainsKey(commandName))
				return false;
			
			_blockingCommandHandlers.Add(commandName, command);
			return true;
		}
		
		public bool RegisterCommand(string commandName, string objectName, BlockingCommandHandler command)
		{
			commandName = $"{objectName.Replace(" ", string.Empty)}::${commandName}";
			if (_blockingCommandHandlers.ContainsKey(commandName))
				return false;
			
			_blockingCommandHandlers.Add(commandName, command);
			return true;
		}

		public void RegisterCommands(Node node, string objectName = "")
		{
			string keyPrefix = string.IsNullOrEmpty(objectName) ? string.Empty : $"{objectName}::";
			
			// If the node implements IYarnCommandsProvider or has a similar interface prefer that
			IYarnCommandsProvider provider = ((object)node) as IYarnCommandsProvider;
			if (provider != null)
			{
				RegisterCommands(provider);
				return;
			}

			try
			{
				provider = new GDYarnCommandProvider(node);
				RegisterCommands(provider);
				return;
			}
			catch (NotImplementedException)
			{}

			// If the object does not implement IYarnCommandsProvider use reflection to looked for marked methods
			foreach (MethodInfo method in node.GetType().GetMethods())
			{
				YarnCommandAttribute[] attributes = (YarnCommandAttribute[])method.GetCustomAttributes(typeof(YarnCommandAttribute), true);
				foreach (YarnCommandAttribute yarnCommandAttribute in attributes)
				{
					string key = $"{keyPrefix}{yarnCommandAttribute.CommandString}";
					if (_IsMethodCompatibleWithDelegate<BlockingCommandHandler>(method) &&
						!_blockingCommandHandlers.ContainsKey(key))
						_blockingCommandHandlers.Add(key,
							((parameters, complete) => method.Invoke(node, new object[] {parameters, complete})));
					else if (_IsMethodCompatibleWithDelegate<CommandHandler>(method) &&
							 !_commandHandlers.ContainsKey(key))
						_commandHandlers.Add(key, (parameters => method.Invoke(node, new object[] {parameters})));
				}
			}
		}

		public void RegisterCommands(IYarnCommandsProvider provider)
		{
			RegisterCommands(provider.YarnCommandHandlers);
			RegisterCommands(provider.YarnBlockingCommandHandlers);
		}

		public void RegisterCommands(Dictionary<string, CommandHandler> commands)
		{
			foreach (KeyValuePair<string, CommandHandler> command in commands)
				RegisterCommand(command.Key, command.Value);
		}

		public void RegisterCommands(Dictionary<string, BlockingCommandHandler> commands)
		{
			foreach (KeyValuePair<string, BlockingCommandHandler> command in commands)
				RegisterCommand(command.Key, command.Value);
		}

		public bool RemoveCommand(string command) => _commandHandlers.Remove(command);
		public bool RemoveBlockingCommand(string command) => _blockingCommandHandlers.Remove(command);

		public void ScanTreeForCommands()
		{
			ScanTreeForCommands(GetTree().Root);
		}

		public void ScanTreeForCommands(Node node)
		{
			RegisterCommands(node);
			foreach (Node child in node.GetChildren())
				ScanTreeForCommands(child);
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
			Dialogue.Continue();
		}

		protected void SelectOption(int option)
		{
			Dialogue.SetSelectedOption(option);
			ContinueDialogue();
		}

		protected Dialogue.HandlerExecutionType ExecuteCommand(Command command)
		{
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
			
			return DialogueManager.ExecuteCommand(command, ContinueDialogue);
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
				lineHandler = (line) => DialogueManager.DisplayLine(line, this, ContinueDialogue),
				commandHandler = ExecuteCommand,
				optionsHandler = (options) => DialogueManager.DisplayOptions(options, this, SelectOption),
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

			// Yarn Spinner defines two built-in commands: "wait", and "stop". Stop is defined inside the Virtual
			// Machine (the compiler traps it and makes it a special case.) Wait is defined here in Godot.
			RegisterCommand("wait", (async (parameters, complete) =>
			{
				if (parameters?.Count() != 1) 
				{
					GD.PrintErr("<<wait>> command expects one parameter.");
					complete();
					return;
				}
				
				string[] args = parameters as string[] ?? parameters.ToArray();
				string durationString = args[0];

				if (float.TryParse(durationString,
					System.Globalization.NumberStyles.AllowDecimalPoint,
					System.Globalization.CultureInfo.InvariantCulture,
					out var duration) == false) 
				{

					GD.PrintErr($"<<wait>> failed to parse duration {durationString}");
					complete();
				}

				if (duration > 0)
				{
					Timer timer = new Timer();
					timer.OneShot = true;
					AddChild(timer);
					timer.Start(duration);
					await ToSignal(timer, "timeout");
					RemoveChild(timer);
				}
				
				complete();
			}));

			return dialogue;
		}
		
		private bool _IsMethodCompatibleWithDelegate<T>(MethodInfo method) where T : class
		{
			Type delegateType = typeof(T);
			MethodInfo delegateSignature = delegateType.GetMethod("Invoke");

			bool parametersEqual = delegateSignature
				.GetParameters()
				.Select(x => x.ParameterType)
				.SequenceEqual(method.GetParameters()
					.Select(x => x.ParameterType));

			return delegateSignature.ReturnType == method.ReturnType &&
				   parametersEqual;
		}
	}
}
