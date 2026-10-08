using Loom.Domain.Entities;

namespace Loom.Application.Features.Planning;

public static class PlanningMappers
{
    public static HomeSlotResponse ToResponse(this HomeSlot slot) => new(
        slot.Id,
        slot.Semester,
        slot.SlotPosition,
        slot.Ects,
        slot.SlotTypeId,
        slot.SlotType.Name,
        slot.SlotType.NameEn,
        slot.SlotType.Color,
        slot.Course?.IsvuCode,
        slot.Course?.Name,
        slot.Course?.NameEn,
        slot.CourseGroup?.IsvuCode,
        slot.CourseGroup?.Name,
        slot.CourseGroup?.NameEn
    );

    public static LearningAgreementEntryResponse ToResponse(this LearningAgreementEntry entry) => new(
        entry.Id,
        entry.HomeSlotId,
        entry.Mode.ToString(),
        entry.PartnerCourseId,
        entry.PartnerCourse?.Code,
        entry.PartnerCourse?.Name,
        entry.PartnerCourse?.NameHr,
        entry.PartnerCourse?.Url,
        entry.AwardedEcts,
        entry.IsDeleted,
        AmendmentNumber: null
    );
}
