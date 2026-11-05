namespace Trackwise.Domain.Entities;

public abstract partial class Workfile
{
    public virtual List<WorkfileContainer> Containers { get; private set; } = [];

    /// <summary>
    /// Adds <paramref name="quantity"/> slots of one size - "4 x 40HC" is how a forwarding job is
    /// booked, before any box has a number. A spec naming a specific box adds exactly one.
    /// The caller must register each returned row with its repository.
    /// </summary>
    public IReadOnlyList<WorkfileContainer> AddContainers(WorkfileContainerSpec spec, int quantity = 1)
    {
        EnsureEditable();

        if (quantity < 1)
        {
            throw new InvalidOperationException("Add at least one container.");
        }

        if (spec.ContainerId.HasValue && quantity != 1)
        {
            throw new InvalidOperationException("A container number names one box - add numbered boxes one at a time.");
        }

        EnsureBoxNotOnFile(spec.ContainerId, exceptRowId: null);

        var next = Containers.Count == 0 ? 0 : Containers.Max(c => c.SortOrder) + 1;
        var added = Enumerable.Range(0, quantity)
            .Select(i => WorkfileContainer.Create(Id, next + i, spec))
            .ToList();

        Containers.AddRange(added);
        return added;
    }

    public void UpdateContainer(Guid rowId, WorkfileContainerSpec spec)
    {
        EnsureEditable();

        var row = FindContainer(rowId);
        EnsureBoxNotOnFile(spec.ContainerId, exceptRowId: rowId);
        row.Apply(spec);
    }

    /// <summary>Removes a slot and returns it, so the caller can delete the row.</summary>
    public WorkfileContainer RemoveContainer(Guid rowId)
    {
        EnsureEditable();

        var row = FindContainer(rowId);
        Containers.Remove(row);
        return row;
    }

    private WorkfileContainer FindContainer(Guid rowId) =>
        Containers.FirstOrDefault(c => c.Id == rowId)
        ?? throw new InvalidOperationException($"That container is not on workfile '{ReferenceNumber}'.");

    private void EnsureBoxNotOnFile(Guid? containerId, Guid? exceptRowId)
    {
        if (containerId.HasValue && Containers.Any(c => c.ContainerId == containerId && c.Id != exceptRowId))
        {
            throw new InvalidOperationException($"That container is already on workfile '{ReferenceNumber}'.");
        }
    }
}
