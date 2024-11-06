using System;
using System.Linq;

namespace Autabee.OpcToClass
{
    public interface IOpcSharperTemplate
    {
        string[] GetScriptNameSpaces();
        string GetScript(GeneratorDataSet settings);
        string GetScriptAsFile(GeneratorDataSet settings);

        string Name { get; set; }
        string ClassName { get; }
        string NameSpace { get; }
    }
}
