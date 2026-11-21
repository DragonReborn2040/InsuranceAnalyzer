using InsuranceAnalyzer.Application.Documents;
using InsuranceAnalyzer.Core.Entities;
using InsuranceAnalyzer.Infrastructure.Documents.Extraction;
using InsuranceAnalyzer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace InsuranceAnalyzer.Infrastructure.Documents;

public sealed class PolicyDocumentService(
    IDbContextFactory<InsuranceAnalyzerDbContext> contextFactory,
    IDocumentStorageService storage,
    HybridPdfTextExtractionService pdfTextExtractor,
    IOptions<DocumentStorageOptions> options,
    ILogger<PolicyDocumentService> logger)
    : IPolicyDocumentService
{
    public async Task<IReadOnlyList<PolicyDocumentDto>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        return await db.PolicyDocuments
            .AsNoTracking()
            .OrderByDescending(x => x.UploadedUtc)
            .Select(x => new PolicyDocumentDto(
                x.Id,
                x.InsurancePolicyId,
                x.InsurancePolicy!.PolicyNumber,
                x.InsurancePolicy.PolicyholderName,
                x.OriginalFileName,
                x.FileSizeBytes,
                x.ProcessingStatus,
                x.UploadedUtc,
                x.PageCount,
                x.ExtractedText,
                x.ProcessingError))
            .ToListAsync(cancellationToken);
    }

    public async Task<PolicyDocumentDto?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        return await db.PolicyDocuments
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new PolicyDocumentDto(
                x.Id,
                x.InsurancePolicyId,
                x.InsurancePolicy!.PolicyNumber,
                x.InsurancePolicy.PolicyholderName,
                x.OriginalFileName,
                x.FileSizeBytes,
                x.ProcessingStatus,
                x.UploadedUtc,
                x.PageCount,
                x.ExtractedText,
                x.ProcessingError))
            .SingleOrDefaultAsync(cancellationToken);
    }

    
        public async Task<PolicyDocument> UploadAsync(
    int insurancePolicyId,
    string originalFileName,
    string contentType,
    long fileSizeBytes,
    Stream content,
    CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "Upload started. PolicyId={PolicyId}, File={File}, Size={Size}, ContentType={ContentType}",
            insurancePolicyId,
            originalFileName,
            fileSizeBytes,
            contentType);

        if (fileSizeBytes <= 0)
            throw new InvalidOperationException("The selected file is empty.");

        if (fileSizeBytes > options.Value.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"Maximum PDF size is {options.Value.MaxFileSizeBytes / 1024 / 1024} MB.");

        logger.LogInformation("File size validation passed.");

        await using var buffer = new MemoryStream();

        logger.LogInformation("Starting browser stream copy.");

        await content.CopyToAsync(buffer, cancellationToken);

        logger.LogInformation(
            "Browser stream copy completed. Bytes={Length}",
            buffer.Length);

        var bytes = buffer.ToArray();

        logger.LogInformation("Converted upload stream to byte array.");

        var hash = Convert.ToHexString(
            SHA256.HashData(bytes));

        logger.LogInformation(
            "SHA256 completed. Hash={Hash}",
            hash);

        // Verify actual PDF signature instead of trusting extension/MIME type.
        if (bytes.Length < 5 ||
            bytes[0] != '%' ||
            bytes[1] != 'P' ||
            bytes[2] != 'D' ||
            bytes[3] != 'F' ||
            bytes[4] != '-')
        {
            throw new InvalidOperationException(
                "The selected file is not a valid PDF.");
        }

        //var hash = Convert.ToHexString(
           // SHA256.HashData(bytes));

        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        var policyExists =
            await db.InsurancePolicies.AnyAsync(
                x => x.Id == insurancePolicyId,
                cancellationToken);

        if (!policyExists)
            throw new InvalidOperationException(
                "The selected insurance policy does not exist.");

        var duplicateExists =
            await db.PolicyDocuments.AnyAsync(
                x => x.InsurancePolicyId == insurancePolicyId &&
                     x.Sha256 == hash,
                cancellationToken);

        if (duplicateExists)
            throw new InvalidOperationException(
                "This PDF has already been uploaded for this policy.");

        var safeOriginalName =
            Path.GetFileName(originalFileName);

        var storedFileName =
            $"{Guid.NewGuid():N}.pdf";

        await using var saveStream =
            new MemoryStream(bytes);

        var storagePath =
            await storage.SaveAsync(
                insurancePolicyId,
                storedFileName,
                saveStream,
                cancellationToken);

        var document = new PolicyDocument
        {
            InsurancePolicyId = insurancePolicyId,
            OriginalFileName = safeOriginalName,
            StoredFileName = storedFileName,
            StoragePath = storagePath,
            ContentType = "application/pdf",
            FileSizeBytes = bytes.LongLength,
            Sha256 = hash,
            ProcessingStatus = "Processing",
            UploadedUtc = DateTime.UtcNow
        };

        db.PolicyDocuments.Add(document);

        await db.SaveChangesAsync(
            cancellationToken);

        try
        {
            var physicalPath =
                storage.GetPhysicalPath(storagePath);

            var extraction =
                await pdfTextExtractor.ExtractAsync(
                    physicalPath,
                    cancellationToken);

            document.ExtractedText =
                extraction.FullText;

            document.PageCount =
                extraction.PageCount;

            if (!extraction.HasText)
            {
                document.ProcessingStatus = "Failed";
            }
            else if (extraction.UsedOcr)
            {
                document.ProcessingStatus = "CompletedHybrid";
            }
            else
            {
                document.ProcessingStatus = "Completed";
            }

            document.ProcessingError = null;
        }
        catch (Exception ex)
        {
            document.ProcessingStatus = "Failed";

            document.ProcessingError =
                ex.GetBaseException().Message.Length > 2000
                    ? ex.GetBaseException().Message[..2000]
                    : ex.GetBaseException().Message;
        }

        await db.SaveChangesAsync(
            cancellationToken);

        return document;
    }

    public async Task<PolicyDocumentFile?> GetFileAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        var document =
            await db.PolicyDocuments
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

        if (document is null)
            return null;

        var stream =
            await storage.OpenReadAsync(
                document.StoragePath,
                cancellationToken);

        if (stream is null)
            return null;

        return new PolicyDocumentFile(
            stream,
            document.ContentType,
            document.OriginalFileName);
    }

    public async Task DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        var document =
            await db.PolicyDocuments
                .SingleOrDefaultAsync(
                    x => x.Id == id,
                    cancellationToken);

        if (document is null)
            return;

        await storage.DeleteAsync(
            document.StoragePath,
            cancellationToken);

        db.PolicyDocuments.Remove(document);

        await db.SaveChangesAsync(
            cancellationToken);
    }
}