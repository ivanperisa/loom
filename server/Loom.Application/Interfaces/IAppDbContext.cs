using Loom.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Loom.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Institution> Institutions { get; }
    DbSet<HomeProgram> HomePrograms { get; }
    DbSet<HomeProfile> HomeProfiles { get; }
    DbSet<HomeCourse> HomeCourses { get; }
    DbSet<HomeCourseGroup> HomeCourseGroups { get; }
    DbSet<HomeSlot> HomeSlots { get; }
    DbSet<HomeSlotType> HomeSlotTypes { get; }
    DbSet<PartnerProgram> PartnerPrograms { get; }
    DbSet<PartnerCourse> PartnerCourses { get; }
    DbSet<Exchange> Exchanges { get; }
    DbSet<LearningAgreement> LearningAgreements { get; }
    DbSet<LearningAgreementEntry> LearningAgreementEntries { get; }
    DbSet<Recognition> Recognitions { get; }
    DbSet<RecognitionEntry> RecognitionEntries { get; }
    DbSet<ExchangeSnapshot> ExchangeSnapshots { get; }
    DbSet<CoordinatorWhitelist> CoordinatorWhitelist { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
