using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Trackwise.Domain.Primitives.ValueObjects;
using Xunit;

namespace Trackwise.Application.Tests.FuelLog;

/// <summary>A fill-up is a truck expense in the fuel category, reviewed as Verified or Flagged.</summary>
public class TruckExpenseFuelTests
{
    private static FuelFillUp FillUp(decimal liters = 35.55m, decimal amount = 3732.75m) =>
        new(Guid.NewGuid(), new DateTime(2026, 9, 28, 2, 0, 0, DateTimeKind.Utc), liters,
            new Money { Amount = amount, Currency = "PHP" }, FleetCard.Shell, 150_200, null, "Shell Molino", null);

    [Fact]
    public void ForFuel_WorksOutThePricePerLiter()
    {
        var expense = TruckExpense.ForFuel(FillUp());

        Assert.Equal(TruckExpenseCategory.Fuel, expense.Category);
        Assert.Equal(VolumeUnit.Liters, expense.QuantityUnit);
        Assert.Equal(105.0000m, expense.PricePerUnit);
        Assert.Equal(FleetCard.Shell, expense.FleetCard);
        Assert.Equal(ExpenseStatus.Pending, expense.Status);
    }

    [Fact]
    public void CorrectFuel_Flagged_GoesBackForReview()
    {
        var expense = TruckExpense.ForFuel(FillUp());
        expense.Reject("office", "Liters look wrong");

        expense.CorrectFuel(FillUp(liters: 40));

        Assert.Equal(ExpenseStatus.Pending, expense.Status);
        Assert.Null(expense.RejectionReason);
        Assert.Equal(40, expense.Quantity);
    }

    [Fact]
    public void CorrectFuel_Verified_Throws()
    {
        var expense = TruckExpense.ForFuel(FillUp());
        expense.Approve("office");

        Assert.Throws<InvalidOperationException>(() => expense.CorrectFuel(FillUp(liters: 40)));
    }

    [Fact]
    public void CorrectFuel_NotFuel_Throws()
    {
        var toll = new TruckExpense
        {
            TruckId = Guid.NewGuid(),
            Amount = new Money { Amount = 120, Currency = "PHP" },
            ExpenseDate = DateTime.UtcNow,
            Category = TruckExpenseCategory.Toll
        };

        Assert.Throws<InvalidOperationException>(() => toll.CorrectFuel(FillUp()));
    }
}
