using BrowserAutomationMaster.Core.Common;
using Xunit;
using CommandRegistry = BrowserAutomationMaster.Core.Common.Commands;

namespace BrowserAutomationMaster.Tests.Commands
{
    /// <summary>
    /// Pins the registration of CLI arguments inside <see cref="CommandRegistry.CommandList"/>. <br/>
    /// Source: Core/Common/Commands.cs
    /// </summary>
    /// <remarks>
    /// These are registry tests rather than syntax tests; they exist so a refactor of
    /// CommandList cannot silently drop an argument, which would leave
    /// <c>bamm help &lt;argument&gt;</c> and the documentation out of sync with the binary.
    /// </remarks>
    public class CommandRegistryTests
    {
        private const string ALLOW_MULTIPLE_INSTANCES = "--allow-multiple-instances";

        [Fact]
        public void AllowMultipleInstances_IsRegistered()
        {
            Assert.True(CommandRegistry.CommandExists(ALLOW_MULTIPLE_INSTANCES));
        }

        [Fact]
        public void AllowMultipleInstances_IsAnArgument()
        {
            Command? command = CommandRegistry.GetCommand(ALLOW_MULTIPLE_INSTANCES);

            Assert.NotNull(command);
            Assert.Equal(CommandType.Argument, command!.Type);
        }

        [Fact]
        public void AllowMultipleInstances_HasExamples()
        {
            string[] examples = CommandRegistry.GetExamples(ALLOW_MULTIPLE_INSTANCES);

            Assert.NotEmpty(examples);
            Assert.All(examples, example => Assert.StartsWith("bamm ", example));
        }

        [Fact]
        public void AllowMultipleInstances_HasADescription()
        {
            string? description = CommandRegistry.GetDescription(ALLOW_MULTIPLE_INSTANCES);

            Assert.False(string.IsNullOrWhiteSpace(description));
        }
    }
}
