using ExchangeMapper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExchangeMapper.Application.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Institution> Institutions { get; }
    DbSet<StudyProgram> StudyPrograms { get; }
    DbSet<StudyProfile> StudyProfiles { get; }
    DbSet<Exchange> Exchanges { get; }
    DbSet<ForeignProgram> ForeignPrograms { get; }
    DbSet<ForeignCourse> ForeignCourses { get; }
    DbSet<CourseSlot> CourseSlots { get; }
    DbSet<SlotState> SlotStates { get; }
    DbSet<SlotMapping> SlotMappings { get; }
    DbSet<Recognition> Recognitions { get; }
    DbSet<RecognitionEntry> RecognitionEntries { get; }
    DbSet<ExchangeSnapshot> ExchangeSnapshots { get; }
    DbSet<CoordinatorWhitelist> CoordinatorWhitelist { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
