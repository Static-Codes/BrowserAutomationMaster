using static BrowserAutomationMaster.Managers.ConstantManager;
using static BrowserAutomationMaster.Messaging.Errors;

namespace BrowserAutomationMaster.Compilation
{
    public class Script
    {
        public readonly ScriptImports Imports;
        public readonly ScriptBody Body;
        public readonly ScriptRequirements Requirements;

        public Script()
        {
            Body = new ScriptBody();
            Requirements = new ScriptRequirements();
            Imports = new ScriptImports();
        }

        public void AddBodyLine(string line) { Body.AddLine(line); }
        public void AddBodyLines(string[] lines) { Body.AddLines(lines); }
        public void AddBodyLines(Dictionary<string, int> lines) { Body.AddLines(lines); }


        public void AddImportStatement(string statement) { Imports.AddStatement(statement); }
        public void AddImportStatements(string[] statement) { Imports.AddStatements(statement); }


        public void AddRequirementPackage(string package) { Requirements.AddPackage(package); }
        public void AddRequirementPackages(string[] packages) 
        {
            foreach (var package in packages) {
                Requirements.AddPackage(package);
            }
        }

        public void ResetInstanceState()
        {
            Body.ResetLines();
            Requirements.ResetPackages();
            Imports.ResetStatements();

            var isNotEmpty =
                Body.scriptLines.Count != 0 ||
                Requirements.packageList.Count != 0 ||
                Imports.statementList.Count != ScriptImports.DefaultImportCount;

            var message =
                "BAM Manager (BAMM) was unable to clean up data from the previous session.\n" +
                $"If this issue persists, please make a bug report at {ISSUES_LINK}\n" +
                "Error Log:\nUnable to reset the state of the current Script() object.";

            if (isNotEmpty)
                WriteAndExit(message, 1);

        }
    }

    public class ScriptBody
    {
        public readonly List<string> scriptLines;
        public ScriptBody()
        {
            scriptLines = [];
        }

        public void AddLine(string line)
        {
            if (scriptLines is null) return;
            scriptLines.Add(line);
        }

        public void AddLine(string line, int index)
        {
            if (scriptLines is null) return;
            scriptLines.Insert(index, line);
        }
        public void AddLines(string[] lines)
        {
            foreach (string line in lines)
                AddLine(line);
        }

        public void AddLines(Dictionary<string, int> lineAndIndex)
        {
            foreach (var (line, index) in lineAndIndex)
                AddLine(line, index);
        }

        public int GetLineCount() { return scriptLines.Count; }

        public string GetMakeRequestLine()
        {
            return scriptLines
                .Where(line => line.Equals("make_request(url)"))
                .First() ?? string.Empty;
        }

        public void ResetLines() { scriptLines.Clear(); }

    }

    public class ScriptRequirements
    {
        public readonly List<string> packageList;
        public ScriptRequirements()
        {
            packageList = [];
        }

        public ScriptRequirements(string[] packages)
        {
            packageList = [.. packages];
        }

        public void AddPackage(string package)
        {
            if (string.IsNullOrEmpty(package))
                return;

            if (packageList.Contains(package))
                return;

            packageList.Add(package);
        }

        public void AddPackages(string[] packages)
        {
            foreach (string package in packages)
                AddPackage(package);
        }

        public void ResetPackages() { packageList.Clear(); }
    }

    public class ScriptImports
    {
        private static readonly string[] DefaultImports = [
            "from sys import stderr, stdout\n"
        ];

        public static readonly int DefaultImportCount = DefaultImports.Length;

        public readonly List<string> statementList;

        public ScriptImports()
        {
            statementList = [.. DefaultImports];
        }

        public ScriptImports(IEnumerable<string> imports)
        {
            statementList = [.. imports];
        }

        public void AddStatement(string statement)
        {
            if (statement != null && !statementList.Contains(statement))
                statementList.Add(statement);
        }

        public void AddStatement(string statement, int index)
        {
            if (statement != null && !statementList.Contains(statement))
                statementList.Insert(index, statement);
        }

        public void AddStatements(string[] statements)
        {
            foreach (string statement in statements)
                AddStatement(statement);
        }

        public void AddStatements(Dictionary<string, int> statements)
        {
            foreach (var (line, index) in statements)
                AddStatement(line, index);
        }

        public void ResetStatements()
        {
            statementList.Clear();
            statementList.AddRange(DefaultImports);
        }
    }
}