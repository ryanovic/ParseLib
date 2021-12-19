namespace ParseLib.Runtime
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Xunit;

    public class TestMathData : TheoryData<string, int>
    {
        public TestMathData()
        {
            Add("1 + 1", 2);
            Add("-1 - 1", -2);
            Add("2 + 2 * 2", 6);
            Add("(2 + 2) * 2", 8);
            Add("10 / (2 + 2)", 2);
            Add("100 / (1 + 2) * 2", 66);
        }
    }
}
