using System;
using System.Linq;

namespace Autabee.OpcToClass
{
    public struct FunctionDefinitions
    {
        public FunctionDefinitions(string returnType, string name, string[] indexItems, string nodeIdString)
        {
            ReturnType = returnType;
            Name = name;
            IndexItems = indexItems;
            NodeIdString = nodeIdString;
        }

        public string Name { get; set; }
        public string[] IndexItems { get; set; }
        public string ReturnType { get; set; }
        public string NodeIdString { get; set; }

        public string Function()
        {
            if (IndexItems.Length > 0)
            {

                return $"{ReturnType} {Name}({"uint " + string.Join(",uint ", IndexItems)})";
            }
            else
            {
                return $"{ReturnType} {Name}()";
            }
        }
    }
}
