namespace Trackwise.Domain.Entities;

public abstract partial class Workfile
{
    /// <summary>
    /// Whether this file can have truck legs (<see cref="TruckingWorkfile.CreateLeg"/>). Only an
    /// ocean file with a Philippine end has boxes to truck to or from a port.
    /// </summary>
    public virtual bool SupportsTruckLegs => false;

    internal void EnsureCanAddTruckLeg()
    {
        if (!SupportsTruckLegs)
        {
            throw new InvalidOperationException($"Workfile '{ReferenceNumber}' can't have truck legs.");
        }

        EnsureEditable();
    }
}
