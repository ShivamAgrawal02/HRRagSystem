namespace HRRagSystem.Services.Interfaces
{
    public interface IIngestionService
    {
        Task PerformIngestAsync(CancellationToken cancellationToken);
    }
}
