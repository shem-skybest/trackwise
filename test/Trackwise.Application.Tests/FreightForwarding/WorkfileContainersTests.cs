using Trackwise.Domain.Entities;
using Trackwise.Domain.Primitives.Enums;
using Xunit;

namespace Trackwise.Application.Tests.FreightForwarding;

/// <summary>
/// Container slots on a workfile. Forwarding books "4 x 40HC" long before any box has a number
/// (in nSFS only 7% of forwarding container rows carry one), so a slot without a box is normal.
/// </summary>
public class WorkfileContainersTests
{
    private static readonly WorkfileHeaderData Header = new() { ReportCategory = ReportCategory.Psb };
    private static readonly Guid Hc40 = Guid.NewGuid();

    private static RegularWorkfile Import() =>
        RegularWorkfile.Create("SLC-IMP0001-26-M", FreightMode.Import, Guid.NewGuid(), null, Header);

    [Fact]
    public void AddContainers_QuantityWithoutNumbers_AddsThatManySlotsInOrder()
    {
        var workfile = Import();

        var added = workfile.AddContainers(new WorkfileContainerSpec(Hc40), 4);

        Assert.Equal(4, added.Count);
        Assert.Equal([0, 1, 2, 3], workfile.Containers.Select(c => c.SortOrder));
        Assert.All(workfile.Containers, c => Assert.Null(c.ContainerId));
    }

    [Fact]
    public void AddContainers_SecondBatch_ContinuesTheOrder()
    {
        var workfile = Import();
        workfile.AddContainers(new WorkfileContainerSpec(Hc40), 2);

        var added = workfile.AddContainers(new WorkfileContainerSpec(Guid.NewGuid()));

        Assert.Equal(2, added.Single().SortOrder);
    }

    [Fact]
    public void AddContainers_NumberedBoxWithQuantityAboveOne_Throws()
    {
        var workfile = Import();

        Assert.Throws<InvalidOperationException>(
            () => workfile.AddContainers(new WorkfileContainerSpec(Hc40, Guid.NewGuid()), 2));
    }

    [Fact]
    public void AddContainers_SameBoxTwice_Throws()
    {
        var workfile = Import();
        var box = Guid.NewGuid();
        workfile.AddContainers(new WorkfileContainerSpec(Hc40, box));

        Assert.Throws<InvalidOperationException>(
            () => workfile.AddContainers(new WorkfileContainerSpec(Hc40, box)));
    }

    [Fact]
    public void AddContainers_NegativeWeight_Throws()
    {
        var workfile = Import();

        Assert.Throws<InvalidOperationException>(
            () => workfile.AddContainers(new WorkfileContainerSpec(Hc40, GrossWeightKg: -1)));
    }

    [Fact]
    public void AddContainers_CancelledFile_Throws()
    {
        var workfile = Import();
        workfile.Cancel("ops", "client withdrew");

        Assert.Throws<InvalidOperationException>(
            () => workfile.AddContainers(new WorkfileContainerSpec(Hc40)));
    }

    [Fact]
    public void UpdateContainer_FillsInTheBoxAndSealLater()
    {
        var workfile = Import();
        var slot = workfile.AddContainers(new WorkfileContainerSpec(Hc40)).Single();
        var box = Guid.NewGuid();

        workfile.UpdateContainer(slot.Id, new WorkfileContainerSpec(Hc40, box, SealNumber: " SL-0099 "));

        Assert.Equal(box, slot.ContainerId);
        Assert.Equal("SL-0099", slot.SealNumber);
    }

    [Fact]
    public void UpdateContainer_ToABoxAnotherSlotHolds_Throws()
    {
        var workfile = Import();
        var box = Guid.NewGuid();
        workfile.AddContainers(new WorkfileContainerSpec(Hc40, box));
        var other = workfile.AddContainers(new WorkfileContainerSpec(Hc40)).Single();

        Assert.Throws<InvalidOperationException>(
            () => workfile.UpdateContainer(other.Id, new WorkfileContainerSpec(Hc40, box)));
    }

    [Fact]
    public void UpdateContainer_KeepingItsOwnBox_Succeeds()
    {
        var workfile = Import();
        var box = Guid.NewGuid();
        var slot = workfile.AddContainers(new WorkfileContainerSpec(Hc40, box)).Single();

        workfile.UpdateContainer(slot.Id, new WorkfileContainerSpec(Hc40, box, SealNumber: "SL-1"));

        Assert.Equal("SL-1", slot.SealNumber);
    }

    [Fact]
    public void RemoveContainer_ReturnsTheSlotAndDropsItFromTheFile()
    {
        var workfile = Import();
        var slot = workfile.AddContainers(new WorkfileContainerSpec(Hc40)).Single();

        var removed = workfile.RemoveContainer(slot.Id);

        Assert.Same(slot, removed);
        Assert.Empty(workfile.Containers);
    }

    [Fact]
    public void RemoveContainer_UnknownSlot_Throws()
    {
        Assert.Throws<InvalidOperationException>(() => Import().RemoveContainer(Guid.NewGuid()));
    }
}
