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

			Script script = GD.Load<Script>("res://addons/YarnSpinner-Godot/YarnProgram.cs");
			Texture icon = GD.Load<Texture>("res://addons/YarnSpinner-Godot/icon.png");
			AddCustomType("YarnProgram", "Resource", script, icon);
		}

		public override void _ExitTree()
		{
			RemoveImportPlugin(_importer);
			_importer = null;

			RemoveCustomType("YarnProgram");
		}
	}
}
#endif
