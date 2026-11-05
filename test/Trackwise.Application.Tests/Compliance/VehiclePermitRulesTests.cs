using Trackwise.Application.Modules.Compliance.VehiclePermits;
using Trackwise.Application.Modules.Compliance.VehiclePermits.Commands;
using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.Compliance;

public class VehiclePermitRulesTests
{
    private static readonly Guid TruckId = Guid.NewGuid();
    private static readonly Guid OtherTruckId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 9, 29);

    #region Current papers

    [Fact]
    public void CurrentIds_RenewalOnFile_OnlyTheLaterPaperIsCurrent()
    {
        var lapsed = VehiclePermit.ForTruck(TruckId, VehiclePermitType.LtoRegistration, Today.AddDays(-20));
        var renewal = VehiclePermit.ForTruck(TruckId, VehiclePermitType.LtoRegistration, Today.AddDays(345));

        var current = VehiclePermitRules.CurrentIds([lapsed, renewal]);

        Assert.Equal([renewal.Id], current);
    }

    [Fact]
    public void CurrentIds_DifferentTypesAndVehicles_EachIsCurrentOnItsOwn()
    {
        var registration = VehiclePermit.ForTruck(TruckId, VehiclePermitType.LtoRegistration, Today);
        var franchise = VehiclePermit.ForTruck(TruckId, VehiclePermitType.LtfrbFranchise, Today);
        var otherTruck = VehiclePermit.ForTruck(OtherTruckId, VehiclePermitType.LtoRegistration, Today);

        var current = VehiclePermitRules.CurrentIds([registration, franchise, otherTruck]);

        Assert.Equal(3, current.Count);
    }

    #endregion

    #region Reminder thresholds

    [Theory]
    [InlineData(61, null, null)]
    [InlineData(60, null, 60)]
    [InlineData(25, null, 30)]
    [InlineData(25, 60, 30)]
    [InlineData(25, 30, null)]
    [InlineData(5, 30, 7)]
    [InlineData(0, 7, 0)]
    [InlineData(-40, null, 0)]
    [InlineData(-40, 0, null)]
    public void DueThreshold_DaysLeftAndLastSent_FiresEachThresholdOnce(int daysLeft, int? lastSent, int? expected)
    {
        Assert.Equal(expected, VehiclePermitRules.DueThreshold(daysLeft, lastSent));
    }

    [Fact]
    public void SetExpiry_NewDate_ReArmsReminders()
    {
        var permit = VehiclePermit.ForTruck(TruckId, VehiclePermitType.Insurance, Today.AddDays(5));
        permit.MarkReminded(7, DateTime.UtcNow);

        permit.SetExpiry(Today.AddDays(365));

        Assert.Null(permit.LastReminderThresholdDays);
        Assert.Equal(60, VehiclePermitRules.DueThreshold(55, permit.LastReminderThresholdDays));
    }

    #endregion

    #region Validation

    [Fact]
    public void CreateValidator_BothTruckAndTrailer_IsRejected()
    {
        var sut = new CreateVehiclePermitValidator();

        var result = sut.Validate(new CreateVehiclePermitCommand
        {
            TruckId = TruckId,
            TrailerId = Guid.NewGuid(),
            Type = VehiclePermitType.LtoRegistration,
            ExpiresOn = Today
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateValidator_IssuedAfterExpiry_IsRejected()
    {
        var sut = new CreateVehiclePermitValidator();

        var result = sut.Validate(new CreateVehiclePermitCommand
        {
            TruckId = TruckId,
            Type = VehiclePermitType.LtoRegistration,
            IssuedOn = Today.AddDays(1),
            ExpiresOn = Today
        });

        Assert.False(result.IsValid);
    }

    #endregion
}
