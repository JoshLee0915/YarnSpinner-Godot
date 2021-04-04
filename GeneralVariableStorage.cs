using System.Collections;
using Godot;
using Godot.Collections;
using Yarn;
using YarnSpinnerGodot.Interfaces;
using Node = Godot.Node;

namespace YarnSpinnerGodot
{
    public class GeneralVariableStorage: Node, IVariableStorage
    {
        [Export]
        private Dictionary _initialStoreState = new Dictionary();
        private readonly Dictionary<string, Value> _store = new Dictionary<string, Value>();
        
        public void SetValue(string variableName, Value value) => _store[variableName] = value;
        public void SetValue(string variableName, string stringValue) => SetValue(variableName, new Value(stringValue));
        public void SetValue(string variableName, float floatValue) => SetValue(variableName, new Value(floatValue));
        public void SetValue(string variableName, bool boolValue) => SetValue(variableName, new Value(boolValue));

        public Value GetValue(string variableName) => _store.ContainsKey(variableName) ? _store[variableName] : null;

        public override void _Ready() => ResetToDefaults();

        public void Clear() => _store.Clear();

        public void ResetToDefaults()
        {
            _store.Clear();
            foreach (DictionaryEntry dictionaryEntry in _initialStoreState)
                SetValue((string)dictionaryEntry.Key, new Value(dictionaryEntry.Value));
        }
    }
}