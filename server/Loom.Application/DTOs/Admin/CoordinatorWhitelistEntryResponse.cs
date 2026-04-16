namespace Loom.Application.DTOs.Admin;

public record CoordinatorWhitelistEntryResponse(
    Guid Id,
    string Email,
    DateTime CreatedAt
);
