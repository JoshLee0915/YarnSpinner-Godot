using Godot;
using Yarn;

namespace YarnSpinnerGodot
{
    [Tool]
    public class YarnProgram: Resource
    {
        [Export]
        public byte[] compiledProgram;

        private Program _program = null;
        public Program Program => _program ?? (_program = Program.Parser.ParseFrom(compiledProgram));
    }
}