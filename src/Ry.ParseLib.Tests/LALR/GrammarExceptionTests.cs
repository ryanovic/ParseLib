namespace Ry.ParseLib.LALR
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
    using Xunit;

    public class GrammarExceptionTests
    {
        [Fact]
        public void Ctor_Creates_Instance_From_Stream()
        {
            var grammar = new Grammar();
            var terminal = new Terminal("test", 0);
            var head = new NonTerminal("head", 0, grammar);
            var productionA = new Production(head, "rule:name:1", Array.Empty<Symbol>());
            var productionB = new Production(head, "rule:name:2", Array.Empty<Symbol>());

            var ex = new GrammarException("message", terminal, productionA, productionB);
            var stream = new MemoryStream();
            var serializer = new BinaryFormatter();

            serializer.Serialize(stream, ex);
            stream.Position = 0;
            ex = (GrammarException)serializer.Deserialize(stream);

            Assert.Equal(terminal.Name, ex.Symbol);
            Assert.Equal(productionA.Name, ex.Productions[0]);
            Assert.Equal(productionB.Name, ex.Productions[1]);
        }
    }
}
