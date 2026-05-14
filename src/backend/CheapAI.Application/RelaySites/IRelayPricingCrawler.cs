namespace CheapAI.Application.RelaySites;

public interface IRelayPricingCrawler
{
    Task<IReadOnlyList<RelayPricingPreviewItemResponse>> PreviewAsync(
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken = default);
}
