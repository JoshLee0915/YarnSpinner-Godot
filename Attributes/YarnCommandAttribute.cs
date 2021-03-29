using System;

namespace YarnSpinnerGodot.Attributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class YarnCommandAttribute: Attribute
    {
        public string CommandString { get; }

        public YarnCommandAttribute(string commandString) => CommandString = commandString;
    }
}