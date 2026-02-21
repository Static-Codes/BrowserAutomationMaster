namespace BrowserAutomationMaster.Core.Types
{
    public class LibraryInfo 
    {
        public required string libName { get; init; }
        public required string basePattern { get; init; }
        public required string resourcePattern { get; init; }
    }
}