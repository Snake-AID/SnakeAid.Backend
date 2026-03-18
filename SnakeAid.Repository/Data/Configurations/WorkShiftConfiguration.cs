using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnakeAid.Core.Domains;

namespace SnakeAid.Repository.Data.Configurations
{
    public class WorkShiftConfiguration : IEntityTypeConfiguration<WorkShift>
    {
        public void Configure(EntityTypeBuilder<WorkShift> builder)
        {
            builder.ToTable("WorkShifts");

            builder.HasIndex(s => s.Name)
                .HasDatabaseName("IX_WorkShifts_Name");

            builder.HasIndex(s => new { s.StartTime, s.EndTime })
                .HasDatabaseName("IX_WorkShifts_StartTime_EndTime");
        }
    }
}
