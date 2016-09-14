using System;


namespace Parrot.DroneControl.Protocol
{
    internal class ConfigCommand
    {
        public bool IsMultiConfig { get; private set; }
        public string ParamName { get; private set; }
        public string ParamValue { get; private set; }

        public ConfigCommand(bool isMultiConfig, string paramName, string paramValue)
        {
            this.IsMultiConfig = isMultiConfig;
            this.ParamName = paramName;
            this.ParamValue = paramValue;
        }

        public ConfigCommand(string paramName, string paramValue) 
            : this(true, paramName, paramValue)
        {
        }       

        public ConfigCommand(string paramName, int paramValue) 
            : this(paramName, paramValue.ToString())
        {
        }

        public ConfigCommand(string paramName, float paramValue)
            : this(paramName, paramValue.ToString())
        {
        }

        public ConfigCommand(string paramName, bool paramValue)
            : this(paramName, paramValue.ToString().ToUpper())
        {
        }
    }
}
