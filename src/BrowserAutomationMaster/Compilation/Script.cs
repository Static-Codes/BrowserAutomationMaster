using BrowserAutomationMaster.Messaging;
using static BrowserAutomationMaster.Managers.ConstantManager;

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
        public void AddRequirementPackages(string package) { Requirements.AddPackage(package); }




        public void ResetInstanceState()
        {
            Body.ResetLines();
            Requirements.ResetPackages();
            Imports.ResetStatements();

            var isNotEmpty =
                Body.scriptLines.Count == 0 ||
                Requirements.packageList.Count == 0 ||
                Imports.statementList.Count == 0;
            
            var message = 
                "BAM Manager (BAMM) was unable to clean up data from the previous session.\n" + 
                $"If this issue persists, please make a bug report at {ISSUES_LINK}\n" +
                "Error Log:\nUnable to reset the state of the current Script() object.";

            if (isNotEmpty)
                Errors.WriteErrorAndExit(message, 1);

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
            if (scriptLines == null) return;
            scriptLines.Add(line);
        }

        public void AddLine(string line, int index)
        {
            if (scriptLines == null) return;
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
        public readonly List<string> statementList;
        public ScriptImports()
        {
            statementList = [];
        }

        public ScriptImports(IEnumerable<string> imports) 
        {
            statementList = [.. imports];
        }

        public void AddStatement(string statement)
        {
            if (statement != null)
                statementList.Add(statement);
        }

        public void AddStatements(string[] statements)
        {
            foreach (string statement in statements)
                AddStatement(statement);
        }

        public void ResetStatements() { statementList.Clear(); }
    }

}
