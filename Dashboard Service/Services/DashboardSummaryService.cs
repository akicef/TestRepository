using Dashboard_Service.Models;
using Dashboard_Service.Repositories;
using Dashboard_Service.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dashboard_Service.Services
{
    public class DashboardSummaryService : IDashboardSummaryService
    {
        private readonly IDashboardSummaryRepository _repo;
        private readonly ICurrentUserProvider _currentUserProvider;

        public DashboardSummaryService(IDashboardSummaryRepository repo, ICurrentUserProvider currentUserProvider)
        {
            _repo = repo;
            _currentUserProvider = currentUserProvider;
        }

        public async Task<DashboardSummaryResponse> GetSummaryAsync()
        {
            var user = _currentUserProvider.GetCurrentUser();

            if (user == null)
            {
                throw new InvalidOperationException("User must be authenticated to access the dashboard.");
            }

            var role = (user.Role ?? "EMPLOYEE").ToUpper();

            var kpis = role switch
            {
                "ADMIN" => await BuildAdminKpisAsync(),
                "CRM" => await BuildCrmKpisAsync(),
                "HR" => await BuildHrKpisAsync(),
                _ => await BuildEmployeeKpisAsync(user.Id)
            };

            var tables = role switch
            {
                "ADMIN" => await BuildAdminTablesAsync(),
                "CRM" => await BuildCrmTablesAsync(),
                "HR" => await BuildHrTablesAsync(),
                _ => await BuildEmployeeTablesAsync(user.Id)
            };

            return new DashboardSummaryResponse
            {
                Role = role,
                GeneratedAt = DateTimeOffset.UtcNow,
                Kpis = kpis,
                Tables = tables
            };
        }

        private async Task<List<KpiCardDto>> BuildAdminKpisAsync()
        {
            var list = new List<KpiCardDto>();
            list.AddRange(await BuildCrmKpisAsync());
            list.AddRange(await BuildHrKpisAsync());
            return list;
        }

        private async Task<List<KpiCardDto>> BuildCrmKpisAsync()
        {
            var today = DateTime.UtcNow.Date;
            var contractsEnd = today.AddDays(30);
            var kpis = new List<KpiCardDto>();
            try { kpis.Add(new KpiCardDto { Key = "crm_total_invoices", Title = "Total invoices", Value = await _repo.CountTotalInvoicesAsync(), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "crm_pending_invoices", Title = "Pending invoices", Value = await _repo.CountPendingInvoicesAsync(), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "crm_overdue_invoices", Title = "Overdue invoices", Value = await _repo.CountOverdueInvoicesAsync(today), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "crm_open_complaints", Title = "Open complaints", Value = await _repo.CountOpenComplaintsAsync(), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "crm_expiring_contracts", Title = "Expiring contracts", Value = await _repo.CountExpiringContractsAsync(today, contractsEnd), Unit = "count" }); } catch { }
            return kpis;
        }

        private async Task<List<KpiCardDto>> BuildHrKpisAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)System.DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);

            var kpis = new List<KpiCardDto>();
            try { kpis.Add(new KpiCardDto { Key = "hr_employees_count", Title = "Employees count", Value = await _repo.CountEmployeesAsync(), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "hr_pending_absence_approval_count", Title = "Pending absences", Value = await _repo.CountAbsenceRequestsByStatusAsync("SUBMITTED"), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "hr_absent_today", Title = "Absent today", Value = await _repo.CountAbsentTodayAsync(today, "APPROVED"), Unit = "count" }); } catch { }
            try { kpis.Add(new KpiCardDto { Key = "hr_absent_this_week", Title = "Absent this week", Value = await _repo.CountAbsentThisWeekAsync(startOfWeek, endOfWeek, "APPROVED"), Unit = "count" }); } catch { }
            return kpis;
        }

private async Task<List<KpiCardDto>> BuildEmployeeKpisAsync(long userId)
{
    var today = DateTime.UtcNow.Date;

    long pending = 0;
    long approved = 0;
    DateTime? next = null;

    try
    {
        pending = await _repo.CountMyAbsencesByStatusAsync(userId, "SUBMITTED");
    }
    catch { }

    try
    {
        approved = await _repo.CountMyAbsencesByStatusAsync(userId, "APPROVED");
    }
    catch { }

    try
    {
        next = await _repo.FindMyNextApprovedAbsenceStartAsync(userId, today, "APPROVED");
    }
    catch { }

    long days = 0;
    if (next.HasValue)
        days = (next.Value.Date - today).Days;

    var kpis = new List<KpiCardDto>();

    try
    {
        kpis.Add(new KpiCardDto
        {
            Key = "employee_pending_absences",
            Title = "Pending absences",
            Value = pending,
            Unit = "count"
        });
    }
    catch { }

    try
    {
        kpis.Add(new KpiCardDto
        {
            Key = "employee_approved_absences",
            Title = "Approved absences",
            Value = approved,
            Unit = "count"
        });
    }
    catch { }

    try
    {
        kpis.Add(new KpiCardDto
        {
            Key = "employee_days_until_next_approved_absence",
            Title = "Next absence in",
            Value = days,
            Unit = "days"
        });
    }
    catch { }

    return kpis;
}

        private async Task<List<TableSectionDto>> BuildAdminTablesAsync()
        {
            var list = new List<TableSectionDto>();
            list.AddRange(await BuildCrmTablesAsync());
            list.AddRange(await BuildHrTablesAsync());
            return list;
        }

        private async Task<List<TableSectionDto>> BuildCrmTablesAsync()
        {
            var today = DateTime.UtcNow.Date;
            var end = today.AddDays(30);
            var tables = new List<TableSectionDto>();
            try
            {
                var latest = await _repo.FindLatestInvoicesAsync();
                tables.Add(new TableSectionDto
                {
                    Key = "crm_latest_invoices",
                    Title = "Latest invoices",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Invoice" },
                        new ColumnDto { Key = "party", Title = "Party" },
                        new ColumnDto { Key = "amount", Title = "Amount" },
                        new ColumnDto { Key = "status", Title = "Status" },
                        new ColumnDto { Key = "due_date", Title = "Due date" }
                    },
                    Rows = latest.Select(i => new Dictionary<string, object>
                    {
                        ["id"] = i.Id,
                        ["party"] = i.PartyName,
                        ["amount"] = i.Amount,
                        ["status"] = i.Status,
                        ["due_date"] = i.DueDate as object
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var overdue = await _repo.FindOverdueInvoicesAsync();
                tables.Add(new TableSectionDto
                {
                    Key = "crm_overdue_invoices",
                    Title = "Overdue invoices",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Invoice" },
                        new ColumnDto { Key = "party", Title = "Party" },
                        new ColumnDto { Key = "amount", Title = "Amount" },
                        new ColumnDto { Key = "status", Title = "Status" },
                        new ColumnDto { Key = "due_date", Title = "Due date" }
                    },
                    Rows = overdue.Select(i => new Dictionary<string, object>
                    {
                        ["id"] = i.Id,
                        ["party"] = i.PartyName,
                        ["amount"] = i.Amount,
                        ["status"] = i.Status,
                        ["due_date"] = i.DueDate as object
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var complaints = await _repo.FindOpenComplaintsAsync();
                tables.Add(new TableSectionDto
                {
                    Key = "crm_open_complaints",
                    Title = "Open complaints",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Complaint ID" },
                        new ColumnDto { Key = "title", Title = "Title" },
                        new ColumnDto { Key = "description", Title = "Description" }
                    },
                    Rows = complaints.Select(c => new Dictionary<string, object>
                    {
                        ["id"] = c.Id,
                        ["title"] = c.Title,
                        ["description"] = c.Description
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var exp = await _repo.FindExpiringContractsAsync(today, end);
                tables.Add(new TableSectionDto
                {
                    Key = "crm_expiring_contracts",
                    Title = "Expiring contracts",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Contract id" },
                        new ColumnDto { Key = "party", Title = "Party name" },
                        new ColumnDto { Key = "status", Title = "Status" },
                        new ColumnDto { Key = "days_left", Title = "Days left" }
                    },
                    Rows = exp.Select(c => new Dictionary<string, object>
                    {
                        ["id"] = c.Id,
                        ["party"] = c.PartyName,
                        ["status"] = c.Status,
                        ["days_left"] = c.EndDate.HasValue ? (object)((c.EndDate.Value - today).Days) : null
                    }).ToList()
                });
            }
            catch { }
            return tables;
        }

        private async Task<List<TableSectionDto>> BuildHrTablesAsync()
        {
            var today = DateTime.UtcNow.Date;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek + (int)System.DayOfWeek.Monday);
            var endOfWeek = startOfWeek.AddDays(6);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var endOfMonth = startOfMonth.AddMonths(1).AddDays(-1);

            var tables = new List<TableSectionDto>();
            try
            {
                var pending = await _repo.FindAbsencesByStatusAsync("SUBMITTED");
                tables.Add(new TableSectionDto
                {
                    Key = "hr_pending_absence_approvals",
                    Title = "Pending absence approvals",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Employee ID" },
                        new ColumnDto { Key = "employee", Title = "Employee" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" }
                    },
                    Rows = pending.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["employee"] = a.EmployeeName,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var week = await _repo.FindAbsencesThisWeekAsync(startOfWeek, endOfWeek);
                tables.Add(new TableSectionDto
                {
                    Key = "hr_absences_this_week",
                    Title = "Absences this week",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Employee ID" },
                        new ColumnDto { Key = "employee", Title = "Employee" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" }
                    },
                    Rows = week.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["employee"] = a.EmployeeName,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var month = await _repo.FindAbsencesThisMonthAsync(startOfMonth, endOfMonth);
                tables.Add(new TableSectionDto
                {
                    Key = "hr_absences_this_month",
                    Title = "Absences this month",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Employee ID" },
                        new ColumnDto { Key = "employee", Title = "Employee" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" }
                    },
                    Rows = month.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["employee"] = a.EmployeeName,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var todayList = await _repo.FindTodaysAbsencesAsync(today);
                tables.Add(new TableSectionDto
                {
                    Key = "hr_todays_absences",
                    Title = "Todays absences",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Employee ID" },
                        new ColumnDto { Key = "employee", Title = "Employee" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" }
                    },
                    Rows = todayList.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["employee"] = a.EmployeeName,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo
                    }).ToList()
                });
            }
            catch { }
            return tables;
        }

        private async Task<List<TableSectionDto>> BuildEmployeeTablesAsync(long userId)
        {
            var tables = new List<TableSectionDto>();
            try
            {
                var pending = await _repo.FindMyPendingAbsencesAsync(userId, "SUBMITTED");
                tables.Add(new TableSectionDto
                {
                    Key = "employee_pending_absences",
                    Title = "Pending absences",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Absence ID" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" }
                    },
                    Rows = pending.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo
                    }).ToList()
                });
            }
            catch { }
            try
            {
                var processed = await _repo.FindMyNonPendingAbsencesAsync(userId, "SUBMITTED");
                tables.Add(new TableSectionDto
                {
                    Key = "employee_non_pending_absences",
                    Title = "Processed absences",
                    Columns = new List<ColumnDto>
                    {
                        new ColumnDto { Key = "id", Title = "Absence ID" },
                        new ColumnDto { Key = "type", Title = "Type" },
                        new ColumnDto { Key = "from", Title = "From" },
                        new ColumnDto { Key = "to", Title = "To" },
                        new ColumnDto { Key = "status", Title = "Status" }
                    },
                    Rows = processed.Select(a => new Dictionary<string, object>
                    {
                        ["id"] = a.Id,
                        ["type"] = a.Type,
                        ["from"] = a.DateFrom,
                        ["to"] = a.DateTo,
                        ["status"] = a.Status
                    }).ToList()
                });
            }
            catch { }
            return tables;
        }
    }
}
