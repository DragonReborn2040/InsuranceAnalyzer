using InsuranceAnalyzer.Application.Documents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace InsuranceAnalyzer.Infrastructure.Documents;

public sealed class LocalDocumentStorageService
    : IDocumentStorageService
{
    private readonly string rootPath;

    public LocalDocumentStorageService(
        IHostEnvironment environment,
        IOptions<DocumentStorageOptions> options)
    {
        var configuredPath = options.Value.RootPath;

        rootPath = Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(
                environment.ContentRootPath,
                configuredPath);

        Directory.CreateDirectory(rootPath);
    }

    public async Task<string> SaveAsync(
        int policyId,
        string storedFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var policyFolder =
            Path.Combine(rootPath, policyId.ToString());

        Directory.CreateDirectory(policyFolder);

        var physicalPath =
            Path.Combine(policyFolder, storedFileName);

        await using var fileStream = new FileStream(
            physicalPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            useAsync: true);

        await content.CopyToAsync(
            fileStream,
            cancellationToken);

        return Path.Combine(
                policyId.ToString(),
                storedFileName)
            .Replace('\\', '/');
    }

    public Task<Stream?> OpenReadAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var physicalPath =
            GetPhysicalPath(relativePath);

        if (!File.Exists(physicalPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(
            physicalPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            81920,
            useAsync: true);

        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(
        string relativePath,
        CancellationToken cancellationToken = default)
    {
        var physicalPath =
            GetPhysicalPath(relativePath);

        if (File.Exists(physicalPath))
            File.Delete(physicalPath);

        return Task.CompletedTask;
    }

    public string GetPhysicalPath(string relativePath)
    {
        var normalizedRelativePath =
            relativePath.Replace('/', Path.DirectorySeparatorChar);

        var fullPath = Path.GetFullPath(
            Path.Combine(rootPath, normalizedRelativePath));

        var fullRoot =
            Path.GetFullPath(rootPath) +
            Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(
                fullRoot,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Invalid document storage path.");
        }

        return fullPath;
    }
}