using System.Buffers;

namespace BrowserAutomationMaster.Core.Helpers 
{
    // Since System.Globalization.TextInfo.ToTitleCase is more complicated than required
    public static class StringExtensions 
    {
        public static string ToTitle(this string value) => string.Concat(char.ToUpper(value[0]), value[1..]);
        public static string ToTitle(this string[] values) => string.Concat(
            values.Select(val => val.ToTitle())
        );

        // <summary>
        // Converts a string to PascalCase using Invariant culture.
        // This is a roughly implementation forgive me, its quite late.
        // This handles PascalCase conversion for the common use cases I could think of, including but not limited to:
        // - .ini keys, 
        // - Environment variables
        // - snake_case
        // - etc
        // </summary>
        
        public static string ToPascalCase(this string? s)
        {
            if (string.IsNullOrEmpty(s)) {
                return "";
            }
            
            var willOverscan = s.Length > 256;
            
            // For short strings, heap allocation feels unnecessary as renting a buffer will allocate more memory than required.
            char[]? rentedArray = null;
            
            Span<char> rentedBuffer = willOverscan
                ? (rentedArray = ArrayPool<char>.Shared.Rent(s.Length))
                : stackalloc char[s.Length];
            
            var delimiters = new char[] { '_', '-', '.' };

            try
            {
                int destinationIndex = 0;
                bool nextCharIsUpperCase = true;

                for (int i = 0; i < s.Length; i++)
                {
                    char c = s[i];

                    if (delimiters.Contains(c)) //
                    {
                        nextCharIsUpperCase = true;
                        continue;
                    }

                    if (nextCharIsUpperCase)
                    {
                        rentedBuffer[destinationIndex++] = char.ToUpperInvariant(c);
                        nextCharIsUpperCase = false;
                    }
                    else
                    {
                        rentedBuffer[destinationIndex++] = char.ToLowerInvariant(c);
                    }
                    
                    // Handling cases where the next char should be capitalized as the current char is a digit.
                    // "v2setting" -> "V2Setting"
                    if (char.IsDigit(c))
                    {
                        nextCharIsUpperCase = true;
                    }
                }

                return new string(rentedBuffer[..destinationIndex]);
            }
            
            // This can be removed realistically since the finally block below will expose the exception object in its entirety.
            catch (Exception ex) 
            {
                Console.WriteLine(
                    string.Join('\n', [
                        $"An exception occured while attempting to convert {s} to PascalCase.",
                        ex.Message
                    ])
                );
                return "";
            }
            
            finally
            {
                if (rentedArray != null) {
                    ArrayPool<char>.Shared.Return(rentedArray);
            
                }
            }
        }    
    }
}