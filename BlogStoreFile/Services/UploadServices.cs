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

        public async Task<(string summaryMessage, int processedCount, int failedCount)> UploadZipContentsAsync(Stream zipStream)
        {
            if (zipStream == null || zipStream.Length == 0)
            {
                throw new ArgumentException("El flujo de datos proporcionado está vacío o no es válido.");
            }

            int processedCount = 0;
            int failedCount = 0;

            try
            {
                using var memStream = new MemoryStream();
                await zipStream.CopyToAsync(memStream).ConfigureAwait(false);
                memStream.Position = 0;

                using var archive = new ZipArchive(memStream, ZipArchiveMode.Read);

                var entries = archive.Entries
                    .Where(entry => !string.IsNullOrEmpty(entry.Name))
                    .ToList();

                int totalEntries = entries.Count;

                var options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 30
                };

                await Parallel.ForEachAsync(entries, options, async (entry, cancellationToken) =>
                {
                    try
                    {
                        await using var entryStream = entry.Open();
                        await using var ms = new MemoryStream();
                        await entryStream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
                        ms.Position = 0;

                        string blobPath = entry.FullName.Replace("\\", "/");
                        var blobClient = _containerClient.GetBlobClient(blobPath);

                        await blobClient.UploadAsync(ms, overwrite: true, cancellationToken: cancellationToken).ConfigureAwait(false);

                        Interlocked.Increment(ref processedCount);
                    }
                    catch (Exception)
                    {
                        Interlocked.Increment(ref failedCount);
                    }
                });

                // Generar el mensaje de resumen
                string summaryMessage = $"[INFO] Archivos procesados: {processedCount} de {totalEntries}\n" +
                                        $"[INFO] Archivos fallidos: {failedCount} de {totalEntries}";

                return (summaryMessage, processedCount, failedCount);
            }
            catch (InvalidDataException ex)
            {
                return ("ZIP upload failed due to invalid data.", 0, 1);
            }
            catch (Exception)
            {
                return ("ZIP upload failed due to an unexpected error.", 0, 1);
            }
        }

    }
}
