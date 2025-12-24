using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FleetManager.Business.DataObjects.DashboardDriverDto
{
    public class DriverDashboardDto
    {
        public string DriverName { get; set; } = string.Empty;
        public DriverStatsDto Stats { get; set; } = new DriverStatsDto();
        public AssignedVehicleDto? AssignedVehicle { get; set; }
        public WeeklyPerformanceDto WeeklyPerformance { get; set; } = new WeeklyPerformanceDto();
        public List<ScheduleItemDto> TodaysSchedule { get; set; } = new List<ScheduleItemDto>();
        public List<ActivityItemDto> RecentActivities { get; set; } = new List<ActivityItemDto>();
        public SafetyMetricsDto SafetyMetrics { get; set; } = new SafetyMetricsDto();
        public MonthlyPerformanceDto MonthlyPerformance { get; set; } = new MonthlyPerformanceDto();
    }
}
