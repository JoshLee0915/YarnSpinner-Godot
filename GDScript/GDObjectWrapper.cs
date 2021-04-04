using System;
using Object = Godot.Object;

namespace YarnSpinnerGodot.GDScript
{
    public abstract class GDObjectWrapper
    {
        protected Object gdObject;
        
        protected abstract string[] Interface
        {
            get;
        }
        
        public GDObjectWrapper(Object gdObject)
        {
            if(ValidateInterface(gdObject))
                this.gdObject = gdObject;
        }

        public bool ImplementsInterface(Object gdObject)
        {
            foreach (string method in Interface)
            {
                if (!gdObject.HasMethod(method))
                    return false;
            }

            return true;
        }

        protected bool ValidateInterface(Object gdObject)
        {
            foreach (string method in Interface)
            {
                if (!gdObject.HasMethod(method))
                    throw new NotImplementedException($"Missing expected method {method}");
            }
            
            return true;
        }
    }
}