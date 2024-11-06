using System;
using System.Linq;

namespace Autabee.OpcToClass
{
    public abstract class OpcTemplate : IOpcSharperTemplate
    {
        public string Name { get; set; } = string.Empty;
        public string ClassName { get { return Name.Split('.').Last(); } }
        public string NameSpace
        {
            get
            {
                var split = Name.Split('.');
                return string.Join(".", split.Take(split.Length - 1));
            }
        }

        public abstract string GetScript(GeneratorDataSet settings);
        public abstract string GetScriptAsFile(GeneratorDataSet settings);
        public abstract string[] GetScriptNameSpaces();
    }
}
