namespace Test
{
    public class CommandTest() 
    {
        // so all keys should be seen as a boolean equation
        // Potential Example
        // @ means is present 
        // ! means is missing
        // # means is required
        // ? means is optional
        
        // string argumentName -> The string representing the name of argument in question
        // int[]? validArgLengths -> Length of supported commands or null if the param is a flag. 
        // int[] validLocations [] -> An array of indexes where this command can be placed
        // string[] unsupportedArgs -> An array of argument names that cannot be used with this argumentName        

        // var argDict = new Dictionary<int, string[]> {
        //     { 0, ["?--nohwc", "?clear"] },
        //     { 1, ["@clear", "#compiled if argDict[0] == clear"] },
        //     { 2, ["?--nohwc"] }
        // };
        // bamm | build 12345 --nohwc
        



    }
}