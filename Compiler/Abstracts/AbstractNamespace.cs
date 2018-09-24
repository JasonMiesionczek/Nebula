using Nebula.Compiler.Objects;

namespace Nebula.Compiler.Abstracts
{
    /// <summary>
    /// This class forms the top of the 'compiled' structure of nodes
    /// generated by the parser. The idea is to assemble the AST into 
    /// separate pieces that represent the structure of the code that is
    /// to be rendered.
    /// 
    /// The compiler will generate the object structure, using different pieces
    /// that represent different aspects of the target language.
    /// </summary>
    public class AbstractNamespace : RootObject
    {
        
    }
}