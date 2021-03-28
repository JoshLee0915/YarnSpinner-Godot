using System;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Array = System.Array;
using Object = Godot.Object;

namespace YarnSpinnerGodot
{
    public class GDVariableStorage: IVariableStorage
    {
        private Object _gdObject;
        
        public void SetValue(string variableName, Value value)
        {
            Array args;
            _gdObject.Call(nameof(SetValue), variableName, value);
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
            return _gdObject.Call(nameof(GetValue), variableName) as Value;
        }

        public GDVariableStorage (Object gdObject)
        {
            if (!gdObject.HasMethod(nameof(SetValue)) || !gdObject.HasMethod(nameof(GetValue)) ||
                !gdObject.HasMethod(nameof(Clear)) || !gdObject.HasMethod(nameof(ResetToDefaults)))
                throw new NotImplementedException("The passed gdObject does not implement the expected interface");
            
            _gdObject = gdObject;
        }

        public void Clear()
        {
            _gdObject.Call(nameof(Clear));
        }

        public void ResetToDefaults()
        {
            _gdObject.Call(nameof(ResetToDefaults));
        }
    }
}