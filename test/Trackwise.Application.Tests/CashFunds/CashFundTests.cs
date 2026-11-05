using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.CashFunds;

/// <summary>
/// An operational fund is spent by Ops and liquidated by the office. These rules are what Monday's
/// liquidation board left to people: nothing counts until the office checks it, and the balance is
/// never typed.
/// </summary>
public class CashFundTests
{
    private static readonly DateOnly Sept16 = new(2026, 9, 16);
    private static readonly DateOnly Sept30 = new(2026, 9, 30);
    private static readonly DateTime Now = new(2026, 9, 30, 8, 0, 0, DateTimeKind.Utc);

    private static CashFund Fund(decimal amount = 15000) =>
        CashFund.Create(Guid.NewGuid(), Sept16, Sept30, amount, "CA-RFV2609111546-235-M");

    private static CashExpenseSpec Spec(decimal amount, string description = "PFI passway") =>
        new(Sept16, CashExpenseCategory.Passway, description, amount, null, null, null, null);

    #region Creating

    [Fact]
    public void Create_PeriodEndsBeforeItStarts_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CashFund.Create(Guid.NewGuid(), Sept30, Sept16, 15000, null));
    }

    [Fact]
    public void AddExpense_NoAmount_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Fund().AddExpense(Spec(0)));
    }

    [Fact]
    public void AddExpense_New_IsWithOpsAndBelongsToTheFund()
    {
        var fund = Fund();

        var expense = fund.AddExpense(Spec(250));

        Assert.Equal(CashExpenseStatus.WithOps, expense.Status);
        Assert.Equal(fund.Id, expense.CashFundId);
    }

    #endregion

    #region Balance

    [Fact]
    public void Balance_AdvanceLessWhatWasSpent()
    {
        var fund = Fund();
        fund.AddExpense(Spec(250));
        fund.AddExpense(Spec(180));

        Assert.Equal(430, fund.SpentTotal);
        Assert.Equal(14570, fund.Balance);
    }

    [Fact]
    public void Balance_DisallowedReceipt_DoesNotCountAndIsOwedBack()
    {
        var fund = Fund();
        var allowed = fund.AddExpense(Spec(250));
        var refused = fund.AddExpense(Spec(100, "Checker areglo"));
        fund.Forward([], Sept30);

        fund.Check([allowed.Id], "office", Now);
        fund.Disallow([refused.Id], "No receipt", "office", Now);

        Assert.Equal(250, fund.SpentTotal);
        Assert.Equal(100, fund.DisallowedTotal);
        Assert.Equal(14750, fund.Balance);
    }

    [Fact]
    public void Balance_SpentMoreThanTheAdvance_IsNegative()
    {
        var fund = Fund(200);
        fund.AddExpense(Spec(250));

        Assert.Equal(-50, fund.Balance);
    }

    #endregion

    #region Forwarding and review

    [Fact]
    public void Forward_NoneNamed_SendsEveryReceiptStillWithOps()
    {
        var fund = Fund();
        fund.AddExpense(Spec(250));
        fund.AddExpense(Spec(180));

        var sent = fund.Forward([], Sept30);

        Assert.Equal(2, sent);
        Assert.All(fund.Expenses, e =>
        {
            Assert.Equal(CashExpenseStatus.Forwarded, e.Status);
            Assert.Equal(Sept30, e.ForwardedOn);
        });
    }

    [Fact]
    public void Forward_NothingWithOps_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Fund().Forward([], Sept30));
    }

    [Fact]
    public void Forward_AlreadyForwarded_Throws()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);

        Assert.Throws<InvalidOperationException>(() => fund.Forward([expense.Id], Sept30));
    }

    [Fact]
    public void Check_ReceiptStillWithOps_Throws()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));

        Assert.Throws<InvalidOperationException>(() => fund.Check([expense.Id], "office", Now));
    }

    [Fact]
    public void Disallow_NoReason_Throws()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);

        Assert.Throws<InvalidOperationException>(() => fund.Disallow([expense.Id], " ", "office", Now));
    }

    [Fact]
    public void Check_AfterDisallowing_ClearsTheReason()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);
        fund.Disallow([expense.Id], "No receipt", "office", Now);

        fund.Check([expense.Id], "office", Now);

        Assert.Equal(CashExpenseStatus.Checked, expense.Status);
        Assert.Null(expense.DisallowedReason);
    }

    [Fact]
    public void UpdateExpense_Forwarded_Throws()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);

        Assert.Throws<InvalidOperationException>(() => fund.UpdateExpense(expense.Id, Spec(300)));
    }

    [Fact]
    public void RemoveExpense_Forwarded_Throws()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);

        Assert.Throws<InvalidOperationException>(() => fund.RemoveExpense(expense.Id));
    }

    [Fact]
    public void RemoveExpense_ExpenseOnAnotherFund_Throws()
    {
        var other = Fund().AddExpense(Spec(250));

        Assert.Throws<InvalidOperationException>(() => Fund().RemoveExpense(other.Id));
    }

    #endregion

    #region Liquidating

    [Fact]
    public void Liquidate_ReceiptNotYetChecked_Throws()
    {
        var fund = Fund();
        fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);

        Assert.Throws<InvalidOperationException>(() => fund.Liquidate("office", Now));
    }

    [Fact]
    public void Liquidate_EveryReceiptReviewed_LocksTheFund()
    {
        var fund = Fund();
        var expense = fund.AddExpense(Spec(250));
        fund.Forward([], Sept30);
        fund.Check([expense.Id], "office", Now);

        fund.Liquidate("office", Now);

        Assert.Equal(CashFundStatus.Liquidated, fund.Status);
        Assert.Equal("office", fund.LiquidatedBy);
        Assert.Throws<InvalidOperationException>(() => fund.AddExpense(Spec(50)));
    }

    [Fact]
    public void Reopen_Liquidated_TakesExpensesAgain()
    {
        var fund = Fund();
        fund.Liquidate("office", Now);

        fund.Reopen();
        fund.AddExpense(Spec(50));

        Assert.Equal(CashFundStatus.Open, fund.Status);
        Assert.Null(fund.LiquidatedAt);
    }

    [Fact]
    public void Reopen_Open_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Fund().Reopen());
    }

    #endregion
}
