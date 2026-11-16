using System.Collections.ObjectModel;
using System.Diagnostics;
using ZipBuddy.Core.Collections;
using ZipBuddy.Core.Models;
using ZipBuddy.Formats;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("===============================================================================");
Console.WriteLine("                       ZIP BUDDY PERFORMANCE BENCHMARK SUITE                     ");
Console.WriteLine("===============================================================================");
Console.ResetColor();

var tempDir = Path.Combine(Path.GetTempPath(), "ZipBuddy_Benchmark_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempDir);

try
{
    var engine = new ArchiveEngine();

    // 1. Generate test data: 500 small files and 1 medium compressible file
    Console.WriteLine("[*] Generating benchmark dataset...");
    var dataDir = Path.Combine(tempDir, "dataset");
    Directory.CreateDirectory(dataDir);

    var random = new Random(42);
    for (int i = 0; i < 500; i++)
    {
        var filePath = Path.Combine(dataDir, $"file_{i:D4}.txt");
        var bytes = new byte[4096]; // 4 KB each
        random.NextBytes(bytes);
        File.WriteAllBytes(filePath, bytes);
    }

    var mediumFile = Path.Combine(dataDir, "large_data.bin");
    var mediumBytes = new byte[10 * 1024 * 1024]; // 10 MB compressible text
    for (int i = 0; i < mediumBytes.Length; i++)
    {
        mediumBytes[i] = (byte)('A' + (i % 26));
    }
    File.WriteAllBytes(mediumFile, mediumBytes);

    long totalDatasetBytes = Directory.GetFiles(dataDir, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
    Console.WriteLine($"[*] Dataset generated: {Directory.GetFiles(dataDir).Length} files, {totalDatasetBytes / (1024.0 * 1024.0):F2} MB total.");
    Console.WriteLine();

    // Table header
    Console.WriteLine("| Operation                 | Format | Elapsed (ms) | Throughput / Stats| Memory (MB) |");
    Console.WriteLine("|---------------------------|--------|--------------|-------------------|-------------|");

    // 1. Benchmark ZIP Compression
    var zipArchive = Path.Combine(tempDir, "bench.zip");
    var memBefore = GC.GetTotalMemory(true);
    var sw = Stopwatch.StartNew();

    var zipCompResult = await engine.CreateArchiveAsync(new CompressionRequest
    {
        OutputArchiveFilePath = zipArchive,
        Format = ArchiveFormat.Zip,
        SourcePaths = [dataDir],
        Options = new CompressionOptions { Level = CompressionLevel.Normal }
    });

    sw.Stop();
    var memAfter = GC.GetTotalMemory(false);
    var zipCompThroughput = (totalDatasetBytes / (1024.0 * 1024.0)) / (sw.Elapsed.TotalSeconds);
    Console.WriteLine($"| Compress Dataset          | ZIP    | {sw.ElapsedMilliseconds,12} | {zipCompThroughput,13:F2} MB/s | {Math.Max(0, (memAfter - memBefore) / (1024.0 * 1024.0)),11:F2} |");

    // 2. Benchmark ZIP Enumeration
    sw.Restart();
    var zipInfo = await engine.OpenArchiveAsync(zipArchive);
    sw.Stop();
    Console.WriteLine($"| Open & Enumerate Entries  | ZIP    | {sw.ElapsedMilliseconds,12} | {zipInfo.TotalEntries,13} entries |             |");

    // 3. Benchmark ZIP Extraction
    var zipExtractDir = Path.Combine(tempDir, "extracted_zip");
    sw.Restart();
    var zipExtResult = await engine.ExtractAsync(new ExtractionRequest
    {
        ArchiveFilePath = zipArchive,
        DestinationDirectory = zipExtractDir,
        DefaultConflictPolicy = ConflictPolicy.Overwrite
    });
    sw.Stop();
    var zipExtThroughput = (totalDatasetBytes / (1024.0 * 1024.0)) / (sw.Elapsed.TotalSeconds);
    Console.WriteLine($"| Extract Full Dataset      | ZIP    | {sw.ElapsedMilliseconds,12} | {zipExtThroughput,13:F2} MB/s |             |");

    // 4. Benchmark 7Z Compression
    var sevenZipArchive = Path.Combine(tempDir, "bench.7z");
    sw.Restart();
    var sevenZipCompResult = await engine.CreateArchiveAsync(new CompressionRequest
    {
        OutputArchiveFilePath = sevenZipArchive,
        Format = ArchiveFormat.SevenZip,
        SourcePaths = [dataDir],
        Options = new CompressionOptions { Level = CompressionLevel.Normal }
    });
    sw.Stop();
    var sevenZipThroughput = (totalDatasetBytes / (1024.0 * 1024.0)) / (sw.Elapsed.TotalSeconds);
    Console.WriteLine($"| Compress Dataset          | 7Z     | {sw.ElapsedMilliseconds,12} | {sevenZipThroughput,13:F2} MB/s |             |");

    // 5. Benchmark 7Z Enumeration
    sw.Restart();
    var sevenZipInfo = await engine.OpenArchiveAsync(sevenZipArchive);
    sw.Stop();
    Console.WriteLine($"| Open & Enumerate Entries  | 7Z     | {sw.ElapsedMilliseconds,12} | {sevenZipInfo.TotalEntries,13} entries |             |");

    // 6. Benchmark 7Z Extraction
    var sevenZipExtractDir = Path.Combine(tempDir, "extracted_7z");
    sw.Restart();
    var sevenZipExtResult = await engine.ExtractAsync(new ExtractionRequest
    {
        ArchiveFilePath = sevenZipArchive,
        DestinationDirectory = sevenZipExtractDir,
        DefaultConflictPolicy = ConflictPolicy.Overwrite
    });
    sw.Stop();
    var sevenZipExtThroughput = (totalDatasetBytes / (1024.0 * 1024.0)) / (sw.Elapsed.TotalSeconds);
    Console.WriteLine($"| Extract Full Dataset      | 7Z     | {sw.ElapsedMilliseconds,12} | {sevenZipExtThroughput,13:F2} MB/s |             |");

    // 7. Benchmark Large Archive In-Memory Traversal (10,000 items)
    var entries = new List<ArchiveEntry>(10_000);
    for (int i = 0; i < 10_000; i++)
    {
        int folderIdx = i % 50;
        entries.Add(new ArchiveEntry
        {
            FullPath = $"folder_{folderIdx}/file_{i:D6}.dat",
            Name = $"file_{i:D6}.dat",
            ParentDirectory = $"folder_{folderIdx}",
            Size = 2048,
            CompressedSize = 1024
        });
    }

    sw.Restart();
    var largeInfo = new ArchiveInfo
    {
        FilePath = "synthetic_large.zip",
        Format = ArchiveFormat.Zip,
        Entries = entries
    };
    sw.Stop();
    Console.WriteLine($"| Index 10,000 Entries      | Memory | {sw.ElapsedMilliseconds,12} | {largeInfo.TotalEntries,13} entries |             |");

    // 8. O(1) Directory Query on 10,000 entries
    sw.Restart();
    for (int i = 0; i < 100; i++)
    {
        _ = largeInfo.GetEntriesInDirectory($"folder_{i % 50}").ToList();
    }
    sw.Stop();
    double avgDirQueryUs = (sw.Elapsed.TotalMilliseconds / 100.0) * 1000.0;
    Console.WriteLine($"| O(1) Dir Queries (x100)   | Memory | {sw.ElapsedMilliseconds,12} | {avgDirQueryUs,12:F1} us/query|             |");

    // 9. ObservableRangeCollection Batch Replace vs Item-by-item Add (5,000 items)
    var sampleItems = Enumerable.Range(0, 5000).Select(i => $"Item_{i}").ToList();

    var batchCol = new ObservableRangeCollection<string>();
    sw.Restart();
    batchCol.ReplaceRange(sampleItems);
    sw.Stop();
    var batchMs = sw.Elapsed.TotalMilliseconds;

    var slowCol = new ObservableCollection<string>();
    sw.Restart();
    foreach (var item in sampleItems) slowCol.Add(item);
    sw.Stop();
    var slowMs = sw.Elapsed.TotalMilliseconds;

    Console.WriteLine($"| Batch UI Replace (5000)   | UI     | {batchMs,12:F2} | {slowMs / Math.Max(0.01, batchMs),12:F1}x faster   |             |");

    // 10. Integrity test (CRC verification) on a real 10,000-entry archive
    Console.WriteLine();
    Console.WriteLine("[*] Building 10,000-entry archive for integrity benchmark...");
    var manyDir = Path.Combine(tempDir, "many_entries");
    Directory.CreateDirectory(manyDir);
    var manyPaths = new List<string>(10_000);
    for (int i = 0; i < 10_000; i++)
    {
        var entryPath = Path.Combine(manyDir, $"entry_{i:D6}.bin");
        File.WriteAllBytes(entryPath, new byte[16]);
        manyPaths.Add(entryPath);
    }

    var manyArchive = Path.Combine(tempDir, "bench_10k.zip");
    var createMany = await engine.CreateArchiveAsync(new CompressionRequest
    {
        OutputArchiveFilePath = manyArchive,
        Format = ArchiveFormat.Zip,
        SourcePaths = manyPaths,
        Options = new CompressionOptions { Level = CompressionLevel.Store }
    });

    if (!createMany.Success)
    {
        throw new InvalidOperationException($"Falha ao criar arquivo de 10.000 entradas: {createMany.ErrorMessage}");
    }

    var manyBytes = new FileInfo(manyArchive).Length;
    var manyMemBefore = GC.GetTotalMemory(true);
    sw.Restart();
    var testMany = await engine.TestArchiveAsync(new TestArchiveRequest { ArchiveFilePath = manyArchive });
    sw.Stop();
    var manyMemAfter = GC.GetTotalMemory(false);
    var entriesPerSecond = sw.Elapsed.TotalSeconds > 0 ? 10_000.0 / sw.Elapsed.TotalSeconds : 0.0;
    var manyThroughput = (manyBytes / (1024.0 * 1024.0)) / Math.Max(sw.Elapsed.TotalSeconds, 0.001);

    Console.WriteLine($"| Test Archive (10,000 entries) | ZIP | {sw.ElapsedMilliseconds,12} | {entriesPerSecond,11:F0} entries/s | {Math.Max(0, (manyMemAfter - manyMemBefore) / (1024.0 * 1024.0)),11:F2} |");
    Console.WriteLine($"[*] Integrity result: success={testMany.Success}, filesProcessed={testMany.FilesProcessed}, corrupted={testMany.CorruptedEntries.Count}, archived={manyBytes / (1024.0 * 1024.0):F2} MB, throughput={manyThroughput:F2} MB/s");

    if (!testMany.Success || testMany.FilesProcessed != 10_000)
    {
        throw new InvalidOperationException("O benchmark de integridade nao processou as 10.000 entradas esperadas.");
    }

    Console.WriteLine("===============================================================================");
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[+] All benchmarks completed successfully with verified data integrity!");
    Console.ResetColor();
}
finally
{
    try
    {
        Directory.Delete(tempDir, true);
    }
    catch { }
}
