using System.Collections.Generic;
using Godot;
using YarnSpinnerGodot.Interfaces;

namespace YarnSpinnerGodot.GDScript
{
    public class GDYarnCommandProvider: GDObjectWrapper, IYarnCommandsProvider
    {
        protected override string[] Interface =>
            new[] {nameof(YarnCommandHandlers)};
        
        public Dictionary<string, DialogueRunner.CommandHandler> YarnCommandHandlers
        {
            get
            {
                Godot.Collections.Dictionary<string, string> commands =
                    gdObject.Call(nameof(YarnCommandHandlers)) as Godot.Collections.Dictionary<string, string> ??
                    new Godot.Collections.Dictionary<string, string>();

                Dictionary<string, DialogueRunner.CommandHandler> handlers =
                    new Dictionary<string, DialogueRunner.CommandHandler>();

                foreach (KeyValuePair<string,string> command in commands)
                    handlers.Add(command.Key,
                        (parameters => gdObject.Call(command.Value, parameters)));

                return handlers;
            }
        }

        
        // The callback can not be passed to the GDObject so can not do blocking commands
        public Dictionary<string, DialogueRunner.BlockingCommandHandler> YarnBlockingCommandHandlers =>
            new Dictionary<string, DialogueRunner.BlockingCommandHandler>();
        
        public GDYarnCommandProvider(Object gdObject) : base(gdObject)
        {}
    }
}