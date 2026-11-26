using BARANG.Core.Domain.Common;
using BARANG.Core.Domain.Production;
using Xunit;

namespace BARANG.Core.UnitTests;

public sealed class WorkShiftTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 26, 8, 0, 0, TimeSpan.Zero);
    [Fact] public void MorningShiftIsCreatedActiveWithEightHours() { Result<WorkShift> shift = WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "MORNING", "شیفت صبح", new TimeOnly(8, 0), new TimeOnly(16, 0), Now); Assert.True(shift.IsSuccess); Assert.True(shift.Value.IsActive); Assert.Equal(8, shift.Value.DurationHours); }
    [Fact] public void OvernightShiftDurationWrapsTwentyFourHours() { Result<WorkShift> shift = WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "NIGHT", "شیفت شب", new TimeOnly(22, 0), new TimeOnly(6, 0), Now); Assert.True(shift.IsSuccess); Assert.Equal(8, shift.Value.DurationHours); }
    [Theory] [InlineData("M")] [InlineData("BAD CODE")] [InlineData("")] public void InvalidCodeIsRejected(string code) => Assert.True(WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), code, "عنوان", new TimeOnly(8, 0), new TimeOnly(16, 0), Now).IsFailure);
    [Fact] public void EmptyTitleIsRejected() => Assert.True(WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "SHIFT1", "  ", new TimeOnly(8, 0), new TimeOnly(16, 0), Now).IsFailure);
    [Fact] public void UpdateRequiresMatchingVersion() { WorkShift shift = WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "MORNING", "صبح", new TimeOnly(8, 0), new TimeOnly(16, 0), Now).Value; Assert.True(shift.Update("صبح کاری", new TimeOnly(7, 30), new TimeOnly(15, 0), shift.Version + 5, Now).IsFailure); Assert.True(shift.Update("صبح کاری", new TimeOnly(7, 30), new TimeOnly(15, 0), shift.Version, Now).IsSuccess); Assert.Equal(7.5, shift.DurationHours); }
    [Fact] public void DeactivationIsIdempotent() { WorkShift shift = WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "MORNING", "صبح", new TimeOnly(8, 0), new TimeOnly(16, 0), Now).Value; long version = shift.Version; shift.SetActive(false, Now); shift.SetActive(false, Now); Assert.False(shift.IsActive); Assert.Equal(version + 1, shift.Version); }
    [Fact] public void EquipmentCapacityRejectsNegativeAndHugeValues() { Result<WorkShift> unused = WorkShift.Create(Guid.NewGuid(), Guid.NewGuid(), "X", "X", new TimeOnly(8, 0), new TimeOnly(9, 0), Now); var equipment = BARANG.Core.Domain.Catalog.Equipment.Create(Guid.NewGuid(), Guid.NewGuid(), "PRINT-01", "پرینتر DTG", "PRINT-DTG", "AVAILABLE", null, null, null, null, null, null, Now).Value; Assert.True(equipment.SetCapacity(-1, Now).IsFailure); Assert.True(equipment.SetCapacity(200000, Now).IsFailure); Assert.True(equipment.SetCapacity(120, Now).IsSuccess); Assert.Equal(120, equipment.CapacityPerHour); Assert.True(equipment.SetCapacity(null, Now).IsSuccess); Assert.Null(equipment.CapacityPerHour); }
}
