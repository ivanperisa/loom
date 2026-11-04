using ErrorOr;
using Loom.Application.Common;
using Loom.Application.Common.Errors;
using Loom.Application.Common.Querying;
using Loom.Application.Interfaces;
using Loom.Domain.Common;
using Loom.Domain.Entities;
using Loom.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Features.Catalog;

public sealed class PartnerInstitutionService(IAppDbContext db)
{
    private static readonly ListSpec<Institution, PartnerInstitutionAdminResponse> List = ListSpec.For<Institution>()
        .SearchIn(i => i.Name, i => i.NameHr, i => i.City, i => i.ErasmusCode)
        .SortBy("name", i => i.Name)
        .SortBy("country", i => i.Country)
        .SortBy("erasmusCode", i => i.ErasmusCode)
        .DefaultSort((q, _) => q.OrderBy(i => i.Country).ThenBy(i => i.Name))
        .Project(CatalogProjections.PartnerInstitution);

    private IQueryable<Institution> Partners => db.Institutions.Where(i => i.Type == InstitutionType.Partner);

    public async Task<PagedResponse<PartnerInstitutionAdminResponse>> ListAsync(PartnerInstitutionListQuery query, CancellationToken ct)
    {
        var page = await Partners
            .AsNoTracking()
            .IncludeDeleted(query.IncludeDeleted)
            .WhereIf(!string.IsNullOrWhiteSpace(query.Country), i => i.Country == query.Country)
            .ToPageAsync(List, query, ct);

        var hasDeleted = await Partners.AnyAsync(i => i.IsDeleted, ct);
        return page with { HasDeleted = hasDeleted };
    }

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> CreateAsync(PartnerInstitutionRequest request, CancellationToken ct)
    {
        var institution = new Institution { Type = InstitutionType.Partner };
        var applied = Apply(institution, request);
        if (applied.IsError) return applied.Errors;

        db.Institutions.Add(institution);
        await db.SaveChangesAsync(ct);
        return await GetAsync(institution.Id, ct);
    }

    public async Task<ErrorOr<PartnerInstitutionAdminResponse>> UpdateAsync(int institutionId, PartnerInstitutionRequest request, CancellationToken ct)
    {
        var institution = await Partners.FirstOrDefaultAsync(i => i.Id == institutionId, ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;

        var applied = Apply(institution, request);
        if (applied.IsError) return applied.Errors;

        await db.SaveChangesAsync(ct);
        return await GetAsync(institution.Id, ct);
    }

    /// <summary>Soft-deletes institutions that exchanges point to, removes the rest.</summary>
    public async Task<ErrorOr<Deleted>> DeleteAsync(int institutionId, CancellationToken ct)
    {
        var institution = await Partners.FirstOrDefaultAsync(i => i.Id == institutionId, ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;

        if (await db.Exchanges.AnyAsync(e => e.PartnerInstitutionId == institutionId, ct))
            institution.MarkDeleted();
        else
            db.Institutions.Remove(institution);

        await db.SaveChangesAsync(ct);
        return Result.Deleted;
    }

    public async Task<ErrorOr<Updated>> RestoreAsync(int institutionId, CancellationToken ct)
    {
        var institution = await Partners.FirstOrDefaultAsync(i => i.Id == institutionId, ct);
        if (institution is null) return CommonErrors.InstitutionNotFound;

        institution.Restore();
        await db.SaveChangesAsync(ct);
        return Result.Updated;
    }

    private Task<PartnerInstitutionAdminResponse> GetAsync(int institutionId, CancellationToken ct) =>
        db.Institutions.AsNoTracking().Where(i => i.Id == institutionId).Select(CatalogProjections.PartnerInstitution).FirstAsync(ct);

    private static ErrorOr<Success> Apply(Institution institution, PartnerInstitutionRequest request)
    {
        var name = request.Name.NullIfBlank();
        var country = request.Country.NullIfBlank();
        if (name is null) return CatalogErrors.InstitutionNameRequired;
        if (country is null) return CatalogErrors.CountryRequired;

        institution.Name = name;
        institution.NameHr = request.NameHr.NullIfBlank() ?? name;
        institution.Country = country;
        institution.City = request.City.NullIfBlank();
        institution.ErasmusCode = request.ErasmusCode.NullIfBlank();
        return Result.Success;
    }
}
