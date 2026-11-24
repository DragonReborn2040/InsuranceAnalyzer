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
            throw new InvalidOperationException(
                "The selected file is empty.");

        if (fileSizeBytes > options.Value.MaxFileSizeBytes)
            throw new InvalidOperationException(
                $"Maximum PDF size is " +
                $"{options.Value.MaxFileSizeBytes / 1024 / 1024} MB.");

        await using var buffer = new MemoryStream();

        await content.CopyToAsync(buffer, cancellationToken);

        var bytes = buffer.ToArray();

        // Verify the PDF signature.
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

        var hash = Convert.ToHexString(
            SHA256.HashData(bytes));

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

        await using var saveStream = new MemoryStream(bytes);

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

        // Save the parent document first to obtain its ID.
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var physicalPath =
                storage.GetPhysicalPath(storagePath);

            var extraction =
                await pdfTextExtractor.ExtractAsync(
                    physicalPath,
                    cancellationToken);

            // Preserve the existing combined-text format.
            document.ExtractedText = extraction.FullText;
            document.PageCount = extraction.PageCount;

            // Phase 2D.2C:
            // The validator already ran in the extraction service.
            // Map the validated page results to database entities.
            var pageEntities = extraction.Pages
                .Select(page =>
                    PolicyDocumentPageMapper.ToEntity(
                        document.Id,
                        page))
                .ToList();

            // Check completeness before inserting records.
            if (pageEntities.Count != extraction.PageCount)
            {
                throw new InvalidOperationException(
                    "Extracted page count does not match " +
                    "the number of page diagnostics.");
            }

            // The database has a unique index on
            // (PolicyDocumentId, PageNumber).
            db.PolicyDocumentPages.AddRange(pageEntities);

            // Preserve the existing processing status rules.
            if (!extraction.HasText)
            {
                document.ProcessingStatus = "Failed";
            }
            else if (extraction.EmptyPageCount > 0)
            {
                document.ProcessingStatus = "PartiallyCompleted";
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

            // EF Core saves the updated document and the new
            // page records together in a transaction.
            await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Document {DocumentId} processed. " +
                "Pages={PageCount}, " +
                "PageRecords={RecordCount}, " +
                "Status={Status}",
                document.Id,
                extraction.PageCount,
                pageEntities.Count,
                document.ProcessingStatus);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Do not hide request cancellation as extraction success.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Document processing or diagnostic persistence " +
                "failed for DocumentId={DocumentId}.",
                document.Id);

            // If page inserts were pending when SaveChanges failed,
            // remove them from the EF Core change tracker before
            // saving the failed document status.
            foreach (var entry in db.ChangeTracker
                         .Entries<PolicyDocumentPage>()
                         .Where(x => x.State == EntityState.Added)
                         .ToList())
            {
                entry.State = EntityState.Detached;
            }

            document.Pages.Clear();

            document.ProcessingStatus = "Failed";

            var errorMessage =
                ex.GetBaseException().Message;

            document.ProcessingError =
                errorMessage.Length > 2000
                    ? errorMessage[..2000]
                    : errorMessage;

            await db.SaveChangesAsync(cancellationToken);
        }

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
    public async Task<IReadOnlyList<PolicyDocumentPageDto>> GetPagesAsync(
    int documentId,
    CancellationToken cancellationToken = default)
    {
        if (documentId <= 0)
            throw new ArgumentOutOfRangeException(nameof(documentId));

        await using var db =
            await contextFactory.CreateDbContextAsync(
                cancellationToken);

        var pages = await db.PolicyDocumentPages
            .AsNoTracking()
            .Where(x => x.PolicyDocumentId == documentId)
            .OrderBy(x => x.PageNumber)
            .ToListAsync(cancellationToken);

        return pages
            .Select(PolicyDocumentPageMapper.ToDto)
            .ToList();
    }
}