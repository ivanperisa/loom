namespace Loom.Application.DTOs.Admin;

public record CoordinatorWhitelistEntryResponse(
    int Id,
    string Email,
    DateTime CreatedAt
);
