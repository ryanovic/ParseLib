using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xunit;

namespace Ry.ParseLib.Runtime
{
    public class ParserExceptionTests
    {
        [Fact]
        public void Ctor_Creates_Instance_From_Stream()
        {
            const int position = 10;
            const string lexeme = "lexeme";
            const string state = "state";

            var ex = new ParserException()
            {
                Position = position,
                Lexeme = lexeme,
                ParserState = state
            };

            var stream = new MemoryStream();
            var serializer = new BinaryFormatter();

            serializer.Serialize(stream, ex);
            stream.Position = 0;
            ex = (ParserException)serializer.Deserialize(stream);

            Assert.Equal(position, ex.Position);
            Assert.Equal(lexeme, ex.Lexeme);
            Assert.Equal(state, ex.ParserState);
        }
    }
}
