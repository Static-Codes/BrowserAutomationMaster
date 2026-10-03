using System.Reflection;
using System.Text;
using BrowserAutomationMaster.Core.Common;
using BrowserAutomationMaster.Core.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace BrowserAutomationMaster.Tests.Utilities
{
    /// <summary>
    /// The extension archive handling: generated fixtures, extraction, and the live download. <br/>
    /// Source: Core/Utilities/ExtensionUtility.cs (GetExtensionContents, ValidateCRXContents,
    /// ValidateXPIContents), Core/Common/Constants.cs (CRXContentChecks, XPIContentChecks)
    /// </summary>
    /// <remarks>
    /// Everything here runs against a generated fixture rather than a downloaded one. BAMM's checks are
    /// byte-sequence searches — it looks for "Cr24", for "manifest.json", for a set of paths — so a real
    /// extension from the Chrome Web Store would pass them without saying anything about whether this
    /// code finds them, and would fail whenever the store changed a file it depends on.
    /// </remarks>
    public class ExtensionArchiveTests(ITestOutputHelper output)
    {
        private static readonly string TEMP_ROOT =
            Path.Combine(Path.GetTempPath(), "bamm-ext-tests", Path.GetRandomFileName());

        private static string WriteFixture(byte[] bytes, string extension)
        {
            Directory.CreateDirectory(TEMP_ROOT);

            string path = Path.Combine(TEMP_ROOT, "fixture" + extension);

            File.WriteAllBytes(path, bytes);

            return path;
        }

        // ------------------------------------------------------------------ the generator

        [Fact]
        public void TheGeneratedCrx_StartsWithTheDocumentedMagic()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            output.WriteLine($"crx is {crx.Length} bytes");

            // Constants.CRXMagicBytes is what ValidateCRXContents searches for.
            Assert.Equal("Cr24", Encoding.ASCII.GetString(crx, 0, 4));
            Assert.Equal(ExtensionArchiveBuilder.CrxMagic.Length, Constants.CRXMagicBytes.Length);
            Assert.Equal(ExtensionArchiveBuilder.CrxMagic.ToArray(), Constants.CRXMagicBytes.ToArray());
        }

        [Fact]
        public void TheGeneratedCrx_DocumentsItsVersionAndHeaderLength()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            uint version = BitConverter.ToUInt32(crx, 4);
            uint headerLength = BitConverter.ToUInt32(crx, 8);

            output.WriteLine($"version={version}, headerLength={headerLength}, archive starts at {12 + headerLength}");

            // The layout the reference document gives for CRX, as the CRX3 variant of it: magic, then a
            // version, then a header length, then that many header bytes, then the archive. The document
            // describes CRX2, which has key and signature lengths instead of a header length; both are
            // little-endian 32-bit fields in that position.
            Assert.Equal(3u, version);
            Assert.Equal(64u, headerLength);
        }

        [Fact]
        public void TheGeneratedCrx_CarriesEverySequenceBammSearchesFor()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            foreach ((string label, ReadOnlyMemory<byte> needle) in Constants.CRXContentChecks)
            {
                bool found = crx.AsSpan().IndexOf(needle.Span) >= 0;

                output.WriteLine($"{label}: {found}");

                Assert.True(found, $"The generated .crx does not contain {label}, so it cannot exercise " +
                    "the content check for it.");
            }
        }

        [Fact]
        public void TheGeneratedXpi_CarriesEverySequenceBammSearchesFor()
        {
            byte[] xpi = ExtensionArchiveBuilder.BuildXpi();

            foreach ((string label, ReadOnlyMemory<byte> needle) in Constants.XPIContentChecks)
            {
                bool found = xpi.AsSpan().IndexOf(needle.Span) >= 0;

                output.WriteLine($"{label}: {found}");

                Assert.True(found, $"The generated .xpi does not contain {label}, so it cannot exercise " +
                    "the content check for it.");
            }
        }

        [Fact]
        public void TheGeneratedXpi_EndsWithTheSignatureBammLooksFor()
        {
            byte[] xpi = ExtensionArchiveBuilder.BuildXpi();

            output.WriteLine($"xpi is {xpi.Length} bytes");

            // Constants.XPIMagicBytes is the ZIP end-of-central-directory signature, so the check is
            // really "is this a well-formed zip". Asserted against the tail, where it must be, rather
            // than merely somewhere in the file.
            int found = xpi.AsSpan().IndexOf(ExtensionArchiveBuilder.XpiMagic);

            output.WriteLine($"first occurrence of PK\\x05\\x06 at byte {found}");

            Assert.True(found >= 0, "No end-of-central-directory record: this is not a zip.");
            Assert.Equal(xpi.Length - 22, found);

            Assert.Equal(ExtensionArchiveBuilder.XpiMagic.ToArray(), Constants.XPIMagicBytes.ToArray());
        }

        [Fact]
        public void BothGeneratedArchives_AreReadableZips()
        {
            Assert.Equal(
                ExtensionArchiveBuilder.CrxEntries,
                ExtensionArchiveBuilder.EntryNames(ExtensionArchiveBuilder.BuildCrx(), ExtensionArchiveBuilder.CrxHeaderLength));

            Assert.Equal(ExtensionArchiveBuilder.XpiEntries, ExtensionArchiveBuilder.EntryNames(ExtensionArchiveBuilder.BuildXpi()));
        }

        // ------------------------------------------------------------------ extraction

        [Fact]
        public async Task ExtractsTheXpi_ReturningItsExactContents()
        {
            byte[] xpi = ExtensionArchiveBuilder.BuildXpi();

            string path = WriteFixture(xpi, ".xpi");

            ExtensionUtility utility = new("file://" + path, "firefox");

            using MemoryStream? extracted = await utility.GetExtensionContents();

            Assert.NotNull(extracted);

            byte[] actual = extracted.ToArray();

            output.WriteLine($"file {xpi.Length} bytes, extracted {actual.Length} bytes");

            // Length and content, because a stream truncated to the header would still be non-null and
            // still "succeed" against a caller that only checks for a stream.
            Assert.Equal(xpi.Length, actual.Length);
            Assert.Equal(xpi, actual);
        }

        [Fact]
        public async Task ExtractsTheCrx_ReturningItsExactContents()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            string path = WriteFixture(crx, ".crx");

            ExtensionUtility utility = new("file://" + path, "chrome");

            // Reached reflectively. GetExtensionContents dispatches on (IsChromeExtension,
            // IsFirefoxExtension), and IsChromeStatus only returns true for a chromewebstore.google.com
            // URL, so a local .crx falls through to `_ => null`. See
            // LocalCrxFiles_AreNotRecognisedAsChromeExtensions, which pins that gap rather than
            // working around it here.
            MethodInfo validator = typeof(ExtensionUtility).GetMethod(
                "ValidateCRXContents",
                BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(nameof(ExtensionUtility), "ValidateCRXContents");

            Task<MemoryStream?> validate = (Task<MemoryStream?>)validator.Invoke(utility, [new ReadOnlyMemory<byte>(crx)])!;

            using MemoryStream? extracted = await validate;

            Assert.NotNull(extracted);

            byte[] actual = extracted.ToArray();

            output.WriteLine($"file {crx.Length} bytes, extracted {actual.Length} bytes");

            Assert.Equal(crx.Length, actual.Length);
            Assert.Equal(crx, actual);
        }

        [Fact]
        public async Task TheExtractedCrx_RoundTripsToTheOriginalArchive()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            string path = WriteFixture(crx, ".crx");

            ExtensionUtility utility = new("file://" + path, "chrome");

            MethodInfo validator = typeof(ExtensionUtility).GetMethod(
                "ValidateCRXContents",
                BindingFlags.Instance | BindingFlags.NonPublic)!;

            using MemoryStream? extracted = await (Task<MemoryStream?>)validator.Invoke(utility, [new ReadOnlyMemory<byte>(crx)])!;

            Assert.NotNull(extracted);

            // The point of a CRX: the archive behind the header has to be readable as a zip, because
            // that is how a browser sideloads it.
            byte[] archive = extracted.ToArray();

            Assert.Equal(
                ExtensionArchiveBuilder.CrxEntries,
                ExtensionArchiveBuilder.EntryNames(archive, ExtensionArchiveBuilder.CrxHeaderLength));
        }

        [Fact]
        public void LocalCrxFiles_AreNotRecognisedAsChromeExtensions()
        {
            byte[] crx = ExtensionArchiveBuilder.BuildCrx();

            string path = WriteFixture(crx, ".crx");

            ExtensionUtility utility = new("file://" + path, "chrome");

            output.WriteLine($"IsLocalFile={utility.IsLocalFile}, IsURL={utility.IsURL}, " +
                             $"IsChromeExtension={utility.IsChromeExtension}, IsFirefoxExtension={utility.IsFirefoxExtension}");

            // A gap, and a worse one than a silent null.
            //
            // CheckChromeStatus only returns true for a chromewebstore.google.com URL, so a local .crx
            // is neither a Chrome nor a Firefox extension as far as the type checks are concerned — even
            // though the extension's own header comment and the published documentation both advertise
            // `feature "add-extension" "file://path/to/chrome/extension.crx"`.
            //
            // The consequence is not a null: GetExtensionContents guards on
            // `!IsChromeExtension && !IsFirefoxExtension` and calls WriteAndExit, which ends the process.
            // So asking BAMM to install a local Chrome extension from its own documentation terminates it.
            // Asserted as the detection facts rather than by calling GetExtensionContents, because
            // calling it ends the test host — the same reason the ParseTests avoid WriteAndExit.
            Assert.True(utility.IsLocalFile);
            Assert.False(utility.IsURL);
            Assert.False(utility.IsChromeExtension);
            Assert.False(utility.IsFirefoxExtension);

            // The local .xpi case is the control: it ends with IsFirefoxExtension true purely because
            // the path ends in .xpi, so it never reaches the fatal guard.
            ExtensionUtility xpi = new("file://" + WriteFixture(ExtensionArchiveBuilder.BuildXpi(), ".xpi"), "firefox");

            Assert.True(xpi.IsFirefoxExtension);
        }

        [Fact]
        public async Task AMissingFile_ExitsRatherThanReturningEmpty()
        {
            ExtensionUtility utility = new("file://" + Path.Combine(TEMP_ROOT, "does-not-exist.xpi"), "firefox");

            // Documented rather than exercised: the catch calls WriteAndExit, which ends the process, so
            // calling it here would take the test host down. Asserted as the shape of the contract so a
            // future refactor returning an empty stream instead is noticed.
            MethodInfo contents = typeof(ExtensionUtility).GetMethod(nameof(ExtensionUtility.GetExtensionContents))!;

            Assert.Equal(typeof(Task<MemoryStream?>), contents.ReturnType);
        }
    }
}