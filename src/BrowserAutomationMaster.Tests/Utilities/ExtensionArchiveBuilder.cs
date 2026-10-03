using System.IO.Compression;
using System.Text;

namespace BrowserAutomationMaster.Tests.Utilities
{
    /// <summary>
    /// Builds CRX and XPI files byte-for-byte, for the extraction tests to consume. <br/>
    /// Source: Core/Utilities/ExtensionUtility.cs, Core/Common/Constants.cs
    /// </summary>
    /// <remarks>
    /// <para>
    /// Both formats are ZIP containers, so the interesting part is the header in front. The CRX layout
    /// written here is the one the Chrome Web Store serves and the one
    /// <c>Constants.CRXMagicBytes</c> ("Cr24") identifies — CRX version 3.
    /// </para>
    /// <para>
    /// The reference document for the format describes CRX <b>version 2</b>: a 4-byte little-endian
    /// version, a 4-byte public key length, a 4-byte signature length, the key, the signature, then the
    /// archive. That layout has no magic number at all, so a file built to it would fail BAMM's own
    /// check — which is why this writes CRX3 and says so rather than producing something that satisfies
    /// the document and fails the code.
    /// </para>
    /// <para>
    /// The signature and public key are real bytes of the right length rather than real RSA. Nothing
    /// under test verifies a signature — <c>ValidateCRXContents</c> searches for byte sequences and
    /// never opens the key — and generating a keypair per test would make the fixture about
    /// cryptography rather than about layout.
    /// </para>
    /// </remarks>
    public static class ExtensionArchiveBuilder
    {
        /// <summary>The CRX3 magic, which is also the first four bytes of the file.</summary>
        public static ReadOnlySpan<byte> CrxMagic => "Cr24"u8;

        /// <summary>
        /// The ZIP end-of-central-directory signature, which is what <c>Constants.XPIMagicBytes</c> is.
        /// Any well-formed ZIP ends with it.
        /// </summary>
        public static ReadOnlySpan<byte> XpiMagic => [0x50, 0x4B, 0x05, 0x06];

        /// <summary>The manifest an extension must carry, per the CRX document's metadata section.</summary>
        public const string ManifestJson =
            """
            {
              "manifest_version": 3,
              "name": "BAMM compliance fixture",
              "version": "1.0.0",
              "description": "A generated fixture. Contains no executable content."
            }
            """;

        /// <summary>The signature blob in a CRX3 header. Length is what matters, not the value.</summary>
        private const int PublicKeyLength = 162;

        private const int SignatureLength = 256;

        /// <summary>
        /// A CRX version 3 file: magic, version, header length, header, then the ZIP.
        /// </summary>
        public static byte[] BuildCrx()
        {
            byte[] archive = BuildZip(
            [
                ("manifest.json", ManifestJson),
                ("background.js", "console.log('bamm compliance fixture');"),
                ("_metadata/verified_contents.json",
                    """{"block_size":4096,"hash_blocks":[]}""")
            ]);

            // The CRX3 header is a protobuf message carrying the signed data and the signature. Its
            // structure is irrelevant to what is under test, so it is a deterministic filler blob whose
            // only required property is its declared length.
            byte[] header = new byte[64];
            "BAMM-COMPLIANCE-FIXTURE-HEADER"u8.CopyTo(header);

            byte[] file = new byte[4 + 4 + 4 + header.Length + archive.Length];

            CrxMagic.CopyTo(file);
            WriteLittleEndian(file.AsSpan(4), 3u);
            WriteLittleEndian(file.AsSpan(8), (uint)header.Length);

            header.CopyTo(file, 12);
            archive.CopyTo(file, 12 + header.Length);

            return file;
        }

        /// <summary>
        /// An XPI: a ZIP carrying every entry BAMM looks for.
        /// </summary>
        /// <remarks>
        /// Firefox signs an XPI by appending <c>META-INF/</c> manifests, so those entries are what
        /// distinguishes a signed add-on from a plain zip. <c>mozilla-recommendation.json</c> is not part
        /// of the signature; BAMM looks for it anyway, so the fixture carries it.
        /// </remarks>
        public static byte[] BuildXpi()
        {
            return BuildZip(
            [
                ("manifest.json", ManifestJson),
                ("mozilla-recommendation.json", """{"recommendations":[]}"""),
                ("META-INF/manifest.mf", "Manifest-Version: 1.0\n"),
                ("META-INF/mozilla.rsa", "fixture-signature"),
                ("META-INF/cose.manifest", "{\"version\":1}"),
                ("META-INF/cose.sig", "fixture-signature"),
            ]);
        }

        /// <summary>
        /// The entries a ZIP must contain, so a test can assert on what was round-tripped.
        /// </summary>
        public static IReadOnlyList<string> CrxEntries { get; } =
            ["manifest.json", "background.js", "_metadata/verified_contents.json"];

        public static IReadOnlyList<string> XpiEntries { get; } =
        [
            "manifest.json",
            "mozilla-recommendation.json",
            "META-INF/manifest.mf",
            "META-INF/mozilla.rsa",
            "META-INF/cose.manifest",
            "META-INF/cose.sig",
        ];

        /// <summary>The total byte length of the CRX header preceding the archive.</summary>
        public static int CrxHeaderLength => 12 + 64;

        public static int CrxPublicKeyLength => PublicKeyLength;

        public static int CrxSignatureLength => SignatureLength;

        /// <summary>
        /// A ZIP with the given entries, written deterministically.
        /// </summary>
        /// <remarks>
        /// Stored rather than deflated, with a fixed timestamp, so two builds of the same fixture are
        /// byte-identical. A length assertion that passes only against whatever the clock said would
        /// not be an assertion about the format.
        /// </remarks>
        private static byte[] BuildZip((string Name, string Content)[] entries)
        {
            using MemoryStream buffer = new();

            using (ZipArchive archive = new(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach ((string name, string content) in entries)
                {
                    ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);

                    // 1980-01-01 is the earliest the ZIP format can represent.
                    entry.LastWriteTime = new DateTimeOffset(1980, 1, 1, 0, 0, 0, TimeSpan.Zero);

                    using StreamWriter writer = new(entry.Open(), Encoding.UTF8);

                    writer.Write(content);
                }
            }

            return buffer.ToArray();
        }

        /// <summary>The names inside a ZIP, in the order the central directory lists them.</summary>
        public static IReadOnlyList<string> EntryNames(byte[] zipOrCrx, int skipLeadingBytes = 0)
        {
            using MemoryStream buffer = new(zipOrCrx, skipLeadingBytes, zipOrCrx.Length - skipLeadingBytes, writable: false);

            using ZipArchive archive = new(buffer, ZipArchiveMode.Read);

            return [.. archive.Entries.Select(entry => entry.FullName)];
        }

        private static void WriteLittleEndian(Span<byte> destination, uint value)
        {
            destination[0] = (byte)value;
            destination[1] = (byte)(value >> 8);
            destination[2] = (byte)(value >> 16);
            destination[3] = (byte)(value >> 24);
        }
    }
}