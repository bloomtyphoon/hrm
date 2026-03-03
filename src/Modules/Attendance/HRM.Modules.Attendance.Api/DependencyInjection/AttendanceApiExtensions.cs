using HRM.Modules.Attendance.Api.Endpoints;
using Microsoft.AspNetCore.Routing;

namespace HRM.Modules.Attendance.Api.DependencyInjection;

public static class AttendanceApiExtensions
{
    public static IEndpointRouteBuilder MapAttendanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapAttendanceRecordEndpoints();
        return app;
    }
}
