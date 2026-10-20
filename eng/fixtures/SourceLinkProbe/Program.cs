using System.IO.Compression;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;

namespace Programaris.Tenancy.SourceLinkProbe
{
    internal static class Program
    {
        private static readonly Guid _SourceLinkKind = new("cc110556-a091-4d38-9fec-25ab9a351a6a");
        private static readonly Guid _Sha256Kind = new("8829d00f-11b8-4213-878b-770e8597ac16");

        private static readonly Dictionary<string, string[]> _Frameworks = new(StringComparer.Ordinal)
        {
            ["Programaris.Tenancy.Abstractions"] = ["netstandard2.0"],
            ["Programaris.Tenancy"] = ["netstandard2.0", "net10.0"],
            ["Programaris.Tenancy.AspNetCore"] = ["net10.0"],
            ["Programaris.Tenancy.Http"] = ["net8.0", "net10.0"]
        };

        private static int Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.Error.WriteLine("Usage: SourceLinkProbe <package directory> <version> <source SHA> <repository root>");

                return 2;
            }

            string packageDirectory = Path.GetFullPath(args[0]);
            string version = args[1];
            string sourceSha = args[2].ToLowerInvariant();
            string repositoryRoot = Path.GetFullPath(args[3]);

            if (sourceSha.Length != 40 || sourceSha.Any(character => !Uri.IsHexDigit(character)))
            {
                Console.Error.WriteLine("A complete 40-character source commit SHA is required.");

                return 2;
            }

            try
            {
                foreach (KeyValuePair<string, string[]> package in _Frameworks)
                {
                    VerifyPackage(packageDirectory, version, sourceSha, repositoryRoot, package.Key, package.Value);
                }

                Console.WriteLine("SourceLink and source document checksums validated for all public package targets.");

                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception);

                return 1;
            }
        }

        private static void VerifyPackage(
            string directory,
            string version,
            string sourceSha,
            string repositoryRoot,
            string packageId,
            IReadOnlyCollection<string> frameworks)
        {
            using ZipArchive package = ZipFile.OpenRead(Path.Combine(directory, $"{packageId}.{version}.nupkg"));
            ZipArchiveEntry nuspecEntry = package.GetEntry($"{packageId}.nuspec")
                ?? throw new InvalidDataException($"Missing NuSpec: {packageId}");

            using (Stream nuspecStream = nuspecEntry.Open())
            {
                XDocument nuspec = XDocument.Load(nuspecStream);
                XElement? repository = nuspec.Descendants().FirstOrDefault(element => element.Name.LocalName == "repository");
                string? repositoryUrl = repository?.Attribute("url")?.Value;
                string? repositoryCommit = repository?.Attribute("commit")?.Value;

                if (!string.Equals(repositoryUrl, "https://github.com/programaris/tenancy", StringComparison.Ordinal))
                {
                    throw new InvalidDataException($"Unexpected repository URL in NuSpec: {packageId}");
                }

                if (!string.Equals(repositoryCommit, sourceSha, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException($"Missing or incorrect repository commit in NuSpec: {packageId}");
                }
            }

            using ZipArchive symbols = ZipFile.OpenRead(Path.Combine(directory, $"{packageId}.{version}.snupkg"));

            foreach (string framework in frameworks)
            {
                VerifySymbols(symbols, packageId, framework, sourceSha, repositoryRoot);
            }
        }

        private static void VerifySymbols(
            ZipArchive archive,
            string packageId,
            string framework,
            string sourceSha,
            string repositoryRoot)
        {
            string symbolPath = $"lib/{framework}/{packageId}.pdb";
            ZipArchiveEntry entry = archive.GetEntry(symbolPath)
                ?? throw new InvalidDataException($"Missing PDB: {symbolPath}");

            using Stream input = entry.Open();
            using var buffer = new MemoryStream();

            input.CopyTo(buffer);
            buffer.Position = 0;

            using MetadataReaderProvider provider = MetadataReaderProvider.FromPortablePdbStream(buffer);
            MetadataReader metadata = provider.GetMetadataReader();

            string? sourceLinkJson = null;

            foreach (CustomDebugInformationHandle handle in metadata.CustomDebugInformation)
            {
                CustomDebugInformation information = metadata.GetCustomDebugInformation(handle);

                if (information.Parent.Kind != HandleKind.ModuleDefinition ||
                    metadata.GetGuid(information.Kind) != _SourceLinkKind)
                {
                    continue;
                }

                if (sourceLinkJson is not null)
                {
                    throw new InvalidDataException($"Multiple SourceLink records: {symbolPath}");
                }

                sourceLinkJson = Encoding.UTF8.GetString(metadata.GetBlobBytes(information.Value));
            }

            if (string.IsNullOrWhiteSpace(sourceLinkJson))
            {
                throw new InvalidDataException($"Missing SourceLink mapping: {symbolPath}");
            }

            using JsonDocument sourceLink = JsonDocument.Parse(sourceLinkJson);
            JsonElement mappings = sourceLink.RootElement.GetProperty("documents");

            if (mappings.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidDataException($"Invalid SourceLink mapping: {symbolPath}");
            }

            int verifiedDocuments = 0;

            foreach (DocumentHandle handle in metadata.Documents)
            {
                Document document = metadata.GetDocument(handle);
                string pdbPath = metadata.GetString(document.Name).Replace('\\', '/');
                int sourceIndex = pdbPath.IndexOf("/src/", StringComparison.Ordinal);

                if (sourceIndex < 0)
                {
                    continue;
                }

                string relativePath = pdbPath.Substring(sourceIndex + 1);

                if (!relativePath.StartsWith($"src/{packageId}/", StringComparison.Ordinal) ||
                    !relativePath.EndsWith(".cs", StringComparison.Ordinal) ||
                    relativePath.Contains("/obj/", StringComparison.Ordinal))
                {
                    continue;
                }

                string expectedUrl = $"https://raw.githubusercontent.com/programaris/tenancy/{sourceSha}/{relativePath}";
                bool sourceMapped = false;

                foreach (JsonProperty mapping in mappings.EnumerateObject())
                {
                    string pattern = mapping.Name.Replace('\\', '/');

                    if (!pattern.EndsWith('*') || !pdbPath.StartsWith(pattern[..^1], StringComparison.Ordinal))
                    {
                        continue;
                    }

                    string? destination = mapping.Value.GetString();

                    if (destination is null || !destination.EndsWith('*'))
                    {
                        continue;
                    }

                    string suffix = pdbPath.Substring(pattern.Length - 1);
                    string resolvedUrl = destination[..^1] + suffix;

                    if (string.Equals(resolvedUrl, expectedUrl, StringComparison.Ordinal))
                    {
                        sourceMapped = true;

                        break;
                    }
                }

                if (!sourceMapped)
                {
                    throw new InvalidDataException($"SourceLink does not resolve to the exact commit: {pdbPath}");
                }

                string physicalPath = Path.GetFullPath(
                    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));

                if (!physicalPath.StartsWith(repositoryRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                    !File.Exists(physicalPath))
                {
                    throw new InvalidDataException($"Source file is missing or outside repository root: {relativePath}");
                }

                if (metadata.GetGuid(document.HashAlgorithm) != _Sha256Kind)
                {
                    throw new InvalidDataException($"Source document does not use SHA-256: {relativePath}");
                }

                byte[] recordedHash = metadata.GetBlobBytes(document.Hash);
                byte[] actualHash = SHA256.HashData(File.ReadAllBytes(physicalPath));

                if (!recordedHash.AsSpan().SequenceEqual(actualHash))
                {
                    throw new InvalidDataException($"Source checksum does not match repository file: {relativePath}");
                }

                verifiedDocuments++;
            }

            if (verifiedDocuments == 0)
            {
                throw new InvalidDataException($"No checked-in source documents validated for {symbolPath}");
            }

            Console.WriteLine($"Verified {symbolPath}: SourceLink SHA={sourceSha}, source checksums={verifiedDocuments}");
        }
    }
}
