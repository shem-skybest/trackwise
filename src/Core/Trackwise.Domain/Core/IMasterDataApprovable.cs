using Trackwise.Domain.Primitives.Enums;

namespace Trackwise.Domain.Core;

/// <summary>
///     Marks a master record that cannot go live on one person's say-so.
///     <para>
///     Implementing this is what gives an entity somewhere to <i>be</i> while it waits. Everything
///     else about maker-checker - who submitted, who reviewed, when, and why it came back - lives on
///     <c>MasterDataApprovalRequest</c> rather than here, because an approval is an event that
///     happens repeatedly to one record and a column can only remember the last one.
///     </para>
///     <para>
///     The interface carries no <c>SubmittedBy</c> for exactly that reason. "The approver may not be
///     the submitter" is checked against the open request, not against a column on the record, so a
///     record submitted, rejected and resubmitted by a different steward cannot strand the first
///     steward's name where the check reads it.
///     </para>
///     <para>
///     ⚠️ The pair with <c>IsActive</c> is an invariant, not a convention: a record that is not
///     <see cref="MasterDataApprovalState.Approved" /> is never active. Approval is the only thing
///     that sets the flag, which is why the update command refuses to.
///     </para>
/// </summary>
public interface IMasterDataApprovable : IMasterDataRecord
{
    /// <summary>
    ///     Where the record sits between being written and being live. The one piece of
    ///     <see cref="MasterDataState" /> that is stored rather than derived - see
    ///     <see cref="MasterDataApprovalState" /> for why it has to be.
    /// </summary>
    MasterDataApprovalState ApprovalState { get; set; }
}
