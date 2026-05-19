using CommercialMagDb.Api.Contracts.Sync;

namespace CommercialMagDb.Api.Infrastructure;

public sealed class SyncService(SyncRepository repository)
{
    public Task<SyncResult> SyncStockSortieAsync(StockSortieSyncRequest request, CancellationToken ct = default)
        => repository.SaveStockSortieAsync(request, ct);

    public Task<SyncResult> SyncDepenseAsync(DepenseSyncRequest request, CancellationToken ct = default)
        => repository.SaveDepenseAsync(request, ct);
}
