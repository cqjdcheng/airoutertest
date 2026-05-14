namespace CheapAI.Application.Models;

public interface IModelCatalogCrawler
{
    Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewAsync(ModelImportPreviewRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModelImportPreviewItemResponse>> PreviewOpenRouterAsync(CancellationToken cancellationToken = default);
}
