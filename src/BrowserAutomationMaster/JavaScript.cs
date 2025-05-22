using Esprima;

internal class JavaScript
{
    public static bool IsValidSyntax(string jsCode, out string? error)
    {
        string sanitizedCode = SanitizeJSForPython(jsCode);
        try
        {
            ParserOptions options = new() { Tolerant = false };
            JavaScriptParser parser = new(options);
            parser.ParseScript(sanitizedCode);
            error = null;
            return true;
        }
        catch (ParserException ex)
        {
            error = ex.Message;
            return false;
        }
    }
    public static string SanitizeJSForPython(string jsCode)
    {
        if (jsCode == null || jsCode == string.Empty) { return string.Empty; }

        // Escapes backslashes first so later replacements dont double up on escape
        string sanitized = jsCode.Replace("\\", "\\\\");

        // Escapes single quotes to prevent closing the outer python string
        sanitized = sanitized.Replace("'", "\\'");

        // Escapes double quotes ONLY if needed inside double-quoted python strings
        // Since js is embedded inside single quotes, this is technically optional
        sanitized = sanitized.Replace("\"", "\\\"");

        // Normalizes line endings
        sanitized = sanitized.Replace("\r\n", "\n").Replace("\r", "\n");

        // Flattens lines into a single line with "\n" inside the string
        sanitized = sanitized.Replace("\n", "\\n");

        // Escapes tab characters
        sanitized = sanitized.Replace("\t", "\\t");

        // Escapes backticks
        sanitized = sanitized.Replace("`", "\\`");

        return sanitized;
    }
}



// EXAMPLE TEST

//string code = @"function getRandomInt(min, max) {
//    return Math.floor(Math.random() * (max - min + 1)) + min;
//}

//const items = [""apple"", ""banana"", ""cherry""];
//const randomItem = items[getRandomInt(0, items.length - 1)];

//let user = {
//    id: getRandomInt(1, 1000),
//    name: ""User_"" + Date.now(),
//    active: true
//};

//if (user.active) {
//    console.log(`User ${user.name} is active and selected: ${randomItem}`);
//} else {
//    console.warn(""User is not active."");
//}";
//bool status = JavaScript.IsValidSyntax(code, out string? error);
//Console.WriteLine(status);
//Console.WriteLine(error ?? "test");
//Environment.Exit(0);
