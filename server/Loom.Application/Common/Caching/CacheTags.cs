namespace Loom.Application.Common.Caching;

/// <summary>
/// Tags for cached reference data. Infrastructure clears a tag automatically whenever SaveChanges touches
/// one of its entity types, so services never invalidate by hand.
/// </summary>
public static class CacheTags
{
    public const string HomeCatalog = "home-catalog";   // home institutions, programmes, profiles
    public const string Coordinators = "coordinators";  // users that can act as coordinators

    public static readonly TimeSpan ReferenceDataTtl = TimeSpan.FromMinutes(10);
}
