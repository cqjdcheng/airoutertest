using CheapAI.Application.Common.Abstractions;
using CheapAI.Application.Common.Exceptions;
using CheapAI.Application.Common.Paging;

namespace CheapAI.Application.RelaySites;

public sealed class RelaySiteAdminService(
    IRelaySiteRepository relaySiteRepository,
    IRelayPricingCrawler relayPricingCrawler,
    ICurrentAdminAccessor currentAdminAccessor)
{
    public Task<PagedResult<RelaySiteListItemResponse>> GetPagedAsync(RelaySiteListQuery query, CancellationToken cancellationToken = default)
    {
        return relaySiteRepository.GetPagedAsync(query, cancellationToken);
    }

    public async Task<RelaySiteDetailResponse> GetByIdAsync(ulong id, CancellationToken cancellationToken = default)
    {
        var result = await relaySiteRepository.GetByIdAsync(id, cancellationToken);
        if (result is null)
        {
            throw new AppNotFoundException("站点不存在");
        }

        return result;
    }

    public async Task<PagedResult<RelaySiteOfferResponse>> GetOffersAsync(ulong id, RelaySiteOfferQuery query, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(id, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        return await relaySiteRepository.GetOffersAsync(id, query, cancellationToken);
    }

    public async Task<ulong> CreateAsync(CreateRelaySiteRequest request, CancellationToken cancellationToken = default)
    {
        var slug = SlugHelper.Normalize(request.Slug, request.Name);
        if (await relaySiteRepository.ExistsBySlugAsync(slug, null, cancellationToken))
        {
            throw new AppConflictException("站点 slug 已存在");
        }

        var normalizedRequest = new CreateRelaySiteRequest
        {
            Slug = slug,
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            WebsiteUrl = request.WebsiteUrl,
            Description = request.Description,
            SupportsRefund = request.SupportsRefund,
            SupportsInvoice = request.SupportsInvoice,
            HasDocs = request.HasDocs,
            DocsUrl = request.DocsUrl,
            InviteUrl = request.InviteUrl,
            RecentReview = request.RecentReview,
            AutoTestEnabled = request.AutoTestEnabled,
            TestApiKey = request.TestApiKey,
            TestIntervalMinutes = request.TestIntervalMinutes,
            Offers = request.Offers
        };

        return await relaySiteRepository.InsertAsync(normalizedRequest, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public async Task UpdateAsync(ulong id, UpdateRelaySiteRequest request, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(id, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        var slug = SlugHelper.Normalize(request.Slug, request.Name);
        if (await relaySiteRepository.ExistsBySlugAsync(slug, id, cancellationToken))
        {
            throw new AppConflictException("站点 slug 已存在");
        }

        var normalizedRequest = new UpdateRelaySiteRequest
        {
            Slug = slug,
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            WebsiteUrl = request.WebsiteUrl,
            Description = request.Description,
            SupportsRefund = request.SupportsRefund,
            SupportsInvoice = request.SupportsInvoice,
            HasDocs = request.HasDocs,
            DocsUrl = request.DocsUrl,
            InviteUrl = request.InviteUrl,
            RecentReview = request.RecentReview,
            AutoTestEnabled = request.AutoTestEnabled,
            TestApiKey = request.TestApiKey,
            TestIntervalMinutes = request.TestIntervalMinutes
        };

        await relaySiteRepository.UpdateAsync(id, normalizedRequest, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public async Task<RelaySiteOfferResponse> UpsertOfferAsync(ulong siteId, ulong? offerId, RelaySiteOfferUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(siteId, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        return await relaySiteRepository.UpsertOfferAsync(siteId, offerId, request, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public async Task DeleteOfferAsync(ulong siteId, ulong offerId, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(siteId, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        await relaySiteRepository.DeleteOfferAsync(siteId, offerId, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public async Task UpdateStatusAsync(ulong id, UpdateRelaySiteStatusRequest request, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(id, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        await relaySiteRepository.UpdateStatusAsync(id, request.Status, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public async Task DeleteAsync(ulong id, CancellationToken cancellationToken = default)
    {
        if (!await relaySiteRepository.ExistsByIdAsync(id, cancellationToken))
        {
            throw new AppNotFoundException("站点不存在");
        }

        await relaySiteRepository.DeleteAsync(id, currentAdminAccessor.AdminUserId, cancellationToken);
    }

    public Task<IReadOnlyList<RelayPricingPreviewItemResponse>> PreviewPricingAsync(
        RelayPricingPreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        return relayPricingCrawler.PreviewAsync(request, cancellationToken);
    }
}
