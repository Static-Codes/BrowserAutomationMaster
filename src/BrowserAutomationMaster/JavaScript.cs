using Esprima;

internal class JavaScript
{
    // ADD ROBUST POST PROCESSING
    public static bool IsValidSyntax(string jsCode, out string? error)
    {
        string sanitizedCode = jsCode.Replace('\'', '"');
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
