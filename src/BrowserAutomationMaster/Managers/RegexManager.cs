using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace BrowserAutomationMaster.Managers
{

    public static partial class RegexManager
    {

        public static readonly Regex PyVersionRegex = PyVersionExpression();
        [GeneratedRegex("^Python\\s(3.[0-9]{1,2})", RegexOptions.Compiled)]

        private static partial Regex PyVersionExpression();

    }
}
