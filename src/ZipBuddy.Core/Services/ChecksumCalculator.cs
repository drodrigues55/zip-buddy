using System.Globalization;
using System.Security.Cryptography;

namespace ZipBuddy.Core.Services;

public record ChecksumResult(
    string FilePath,
    string FileName,
    long FileSize,
    string Crc32,
    string Crc64,
    string Sha1,
    string Sha256
);

public static class ChecksumCalculator
{
    private static readonly uint[] Crc32Table = GenerateCrc32Table();
    private static readonly ulong[] Crc64Table = GenerateCrc64Table();

    private static uint[] GenerateCrc32Table()
    {
        var table = new uint[256];
        for (uint i = 0; i < 256; i++)
        {
            uint entry = i;
            for (int j = 0; j < 8; j++)
            {
                entry = (entry & 1) != 0 ? (entry >> 1) ^ 0xEDB88320u : (entry >> 1);
            }
            table[i] = entry;
        }
        return table;
    }

    private static ulong[] GenerateCrc64Table()
    {
        var table = new ulong[256];
        for (ulong i = 0; i < 256; i++)
        {
            ulong entry = i;
            for (int j = 0; j < 8; j++)
            {
                // Reflected form of the ECMA-182 polynomial (CRC-64/XZ). Using the normal-form
                // polynomial here would produce an incompatible, non-standard check value.
                entry = (entry & 1) != 0 ? (entry >> 1) ^ 0xC96C5795D7870F42UL : (entry >> 1);
            }
            table[i] = entry;
        }
        return table;
    }

    /// <summary>
    /// CRC-32/XZ initial value. Exposed for incremental integrity checks (e.g. archive entry verification).
    /// </summary>
    public const uint Crc32Initial = 0xFFFFFFFF;

    /// <summary>Updates a running CRC-32 (reflected polynomial 0xEDB88320) with the next data chunk.</summary>
    public static uint Crc32Update(uint crc, ReadOnlySpan<byte> data)
    {
        foreach (var b in data)
            crc = (crc >> 8) ^ Crc32Table[(crc ^ b) & 0xFF];
        return crc;
    }

    /// <summary>Finalizes a running CRC-32 by applying the standard final XOR.</summary>
    public static uint Crc32Finalize(uint crc) => crc ^ 0xFFFFFFFF;

    public static async Task<ChecksumResult> ComputeAsync(
        string filePath,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("File not found", filePath);

        long totalBytes = fileInfo.Length;
        long processedBytes = 0;

        uint crc32 = 0xFFFFFFFF;
        ulong crc64 = 0xFFFFFFFFFFFFFFFF;

#pragma warning disable CA5350 // Do not use weak cryptographic algorithms (SHA1 needed for checksum comparison)
        using var sha1 = SHA1.Create();
#pragma warning restore CA5350
        using var sha256 = SHA256.Create();

        const int bufferSize = 64 * 1024;
        byte[] buffer = new byte[bufferSize];

        await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);

        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer.AsMemory(0, bufferSize), cancellationToken).ConfigureAwait(false)) > 0)
        {
            // CRC-32
            for (int i = 0; i < bytesRead; i++)
            {
                crc32 = (crc32 >> 8) ^ Crc32Table[(crc32 ^ buffer[i]) & 0xFF];
            }

            // CRC-64
            for (int i = 0; i < bytesRead; i++)
            {
                crc64 = (crc64 >> 8) ^ Crc64Table[(crc64 ^ buffer[i]) & 0xFF];
            }

            // SHA-1 & SHA-256 block update
            sha1.TransformBlock(buffer, 0, bytesRead, null, 0);
            sha256.TransformBlock(buffer, 0, bytesRead, null, 0);

            processedBytes += bytesRead;
            if (totalBytes > 0 && progress != null)
            {
                progress.Report((double)processedBytes / totalBytes);
            }
        }

        sha1.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);

        crc32 ^= 0xFFFFFFFF;
        crc64 ^= 0xFFFFFFFFFFFFFFFF;

        string crc32Hex = crc32.ToString("X8", CultureInfo.InvariantCulture);
        string crc64Hex = crc64.ToString("X16", CultureInfo.InvariantCulture);
        string sha1Hex = Convert.ToHexString(sha1.Hash ?? Array.Empty<byte>());
        string sha256Hex = Convert.ToHexString(sha256.Hash ?? Array.Empty<byte>());

        return new ChecksumResult(
            filePath,
            fileInfo.Name,
            totalBytes,
            crc32Hex,
            crc64Hex,
            sha1Hex,
            sha256Hex
        );
    }
}
