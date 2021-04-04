using System;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Array = System.Array;
using Object = Godot.Object;

namespace YarnSpinnerGodot.GDScript
{
	public class GDVariableStorage: GDObjectWrapper, IVariableStorage
	{
		protected override string[] Interface => new[]
			{nameof(SetValue), nameof(GetValue), nameof(Clear), nameof(ResetToDefaults)};
		
		public void SetValue(string variableName, Value value)
		{
			gdObject.Call(nameof(SetValue), variableName, value);
		}

		public void SetValue(string variableName, string stringValue)
		{
			SetValue(variableName, new Value(stringValue));
		}

		public void SetValue(string variableName, float floatValue)
		{
			SetValue(variableName, new Value(floatValue));
		}

		public void SetValue(string variableName, bool boolValue)
		{
			SetValue(variableName, new Value(boolValue));
		}

		public Value GetValue(string variableName)
		{
			return (Value)gdObject.Call(nameof(GetValue), variableName);
		}
		
		public GDVariableStorage(Object gdObject) : base(gdObject)
		{}

		public void Clear()
		{
			gdObject.Call(nameof(Clear));
		}

		public void ResetToDefaults()
		{
			gdObject.Call(nameof(ResetToDefaults));
		}
	}
}
