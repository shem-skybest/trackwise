using Trackwise.Application.Abstractions;
using Trackwise.Shared.Models;

namespace Trackwise.Application.Modules.Compliance.Eld.Queries;

public class GetAllDriversHosStatusQuery : IQuery<Result<List<DriverHosStatusDto>>>;
