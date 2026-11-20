namespace InsuranceAnalyzer.Application.Documents
{
    public sealed class DocumentStorageOptions
    {
        public const string SectionName = "DocumentStorage";

        public string RootPath { get; set; } = "Data/PolicyDocuments";

        public long MaxFileSizeBytes { get; set; } = 25 * 1024 * 1024;
    }
}
