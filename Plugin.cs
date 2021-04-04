#if TOOLS
using Godot;

namespace YarnSpinnerGodot
{
	[Tool]
	public class Plugin : EditorPlugin
	{
		private YarnSpinnerImporter _importer = null;
	
		public override void _EnterTree()
		{
			_importer = new YarnSpinnerImporter();
			AddImportPlugin(_importer);
			
			Texture yarnIcon = GD.Load<Texture>("res://addons/YarnSpinner-Godot/icon-yarn.png");
			Texture spinnerIcon = GD.Load<Texture>("res://addons/YarnSpinner-Godot/icon.png");

			Script programScript = GD.Load<Script>("res://addons/YarnSpinner-Godot/YarnProgram.cs");
			AddCustomType("YarnProgram", "Resource", programScript, yarnIcon);

			Script runnerScript = GD.Load<Script>("res://addons/YarnSpinner-Godot/DialogueRunner.cs");
			AddCustomType("YarnDialogueRunner", "Node", runnerScript, spinnerIcon);
			
			Script generalVarStoreScript = GD.Load<Script>("res://addons/YarnSpinner-Godot/GeneralVariableStorage.cs");
			AddCustomType("GeneralYarnVariableStorage", "Node", generalVarStoreScript, spinnerIcon);
			
			Script DialogueEventDispatcherScript = GD.Load<Script>("res://addons/YarnSpinner-Godot/DialogueEventDispatcher.cs");
			AddCustomType("DialogueEventDispatcher", "Node", DialogueEventDispatcherScript, spinnerIcon);
		}

		public override void _ExitTree()
		{
			RemoveImportPlugin(_importer);
			_importer = null;

			RemoveCustomType("YarnProgram");
			RemoveCustomType("YarnDialogueRunner");
			RemoveCustomType("GeneralYarnVariableStorage");
			RemoveCustomType("DialogueEventDispatcher");
		}
	}
}
#endif
