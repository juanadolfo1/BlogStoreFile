using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;

namespace BlogStoreFile.Services
{
    public class UploadServices
    {
        private readonly BlobContainerClient _containerClient;

        public UploadServices(IConfiguration configuration)
        {
            string connectionString = configuration.GetConnectionString("AzureBlobStorage");
            string containerName = configuration.GetValue<string>("BlobContainerName");

            var blobServiceClient = new BlobServiceClient(connectionString);
            _containerClient = blobServiceClient.GetBlobContainerClient(containerName);
        }

        /*    public async Task UploadZipContentsAsync(Stream zipStream)
            {
                using var memStream = new MemoryStream();
                await zipStream.CopyToAsync(memStream);
                memStream.Position = 0;

                using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

                var uploadTasks = new List<Task>();
                int maxParallelism = 20;

                using var semaphore = new SemaphoreSlim(maxParallelism);

                foreach (var entry in archive.Entries)
                {
                    if (string.IsNullOrEmpty(entry.Name))
                        continue;

                    await semaphore.WaitAsync();

                    uploadTasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            using var entryStream = entry.Open();
                            using var ms = new MemoryStream();
                            await entryStream.CopyToAsync(ms);
                            ms.Position = 0;

                            string blobPath = entry.FullName.Replace("\\", "/");
                            var blobClient = _containerClient.GetBlobClient(blobPath);

                            await blobClient.UploadAsync(ms, overwrite: true);
                            Console.WriteLine($"Uploaded: {blobPath}");
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }));
                }

                await Task.WhenAll(uploadTasks);
            }
    /*/
        public async Task UploadZipContentsAsync(Stream zipStream)
        {
            using var memStream = new MemoryStream();
            await zipStream.CopyToAsync(memStream).ConfigureAwait(false);
            memStream.Position = 0;

            long zipSizeInBytes = memStream.Length;
            Console.WriteLine($"[INFO] Tamaño total del ZIP: {zipSizeInBytes / (1024.0 * 1024):F2} MB");

            using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

            var entries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .ToList();

            int totalEntries = entries.Count;
            Console.WriteLine($"[INFO] Total de archivos a procesar: {totalEntries}");

            var options = new ParallelOptions
            {
                MaxDegreeOfParallelism = 20
            };

            int processedCount = 0;

            await Parallel.ForEachAsync(entries, options, async (entry, cancellationToken) =>
            {
                int current = Interlocked.Increment(ref processedCount);

                try
                {
                    await using var entryStream = entry.Open();
                    await using var ms = new MemoryStream();
                    await entryStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                    ms.Position = 0;

                    string blobPath = entry.FullName.Replace("\\", "/");
                    var blobClient = _containerClient.GetBlobClient(blobPath);

                    await blobClient.UploadAsync(ms, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                    Console.WriteLine($"[OK] ({current}/{totalEntries}) Subido: {blobPath}");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[ERROR] ({current}/{totalEntries}) Error al subir '{entry.FullName}': {ex.Message}");
                }
            });

            Console.WriteLine("[INFO] Proceso de subida finalizado.");
        }
    }
}
