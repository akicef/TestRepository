using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dashboard_Service.Models;

namespace Dashboard_Service.Repositories
{
    public interface IDashboardSummaryRepository
    {
        // CRM KPIs
        Task<long> CountTotalInvoicesAsync();
        Task<long> CountPendingInvoicesAsync();
        Task<long> CountOverdueInvoicesAsync(DateTime today);
        Task<long> CountOpenComplaintsAsync();
        Task<long> CountExpiringContractsAsync(DateTime today, DateTime endDate);

        // HR KPIs
        Task<long> CountEmployeesAsync();
        Task<long> CountAbsenceRequestsByStatusAsync(string status);
        Task<long> CountAbsentTodayAsync(DateTime today, string status);
        Task<long> CountAbsentThisWeekAsync(DateTime startOfWeek, DateTime endOfWeek, string status);

        // Employee KPIs
        Task<long> CountMyAbsencesByStatusAsync(long userId, string status);
        Task<DateTime?> FindMyNextApprovedAbsenceStartAsync(long userId, DateTime today, string status);

        // Tables (strongly-typed)
        Task<List<InvoiceDto>> FindLatestInvoicesAsync();
        Task<List<InvoiceDto>> FindOverdueInvoicesAsync();
        Task<List<ComplaintDto>> FindOpenComplaintsAsync();
        Task<List<ContractDto>> FindExpiringContractsAsync(DateTime today, DateTime endDate);

        Task<List<AbsenceDto>> FindAbsencesByStatusAsync(string status);
        Task<List<AbsenceDto>> FindTodaysAbsencesAsync(DateTime today);
        Task<List<AbsenceDto>> FindAbsencesThisWeekAsync(DateTime startOfWeek, DateTime endOfWeek);
        Task<List<AbsenceDto>> FindAbsencesThisMonthAsync(DateTime startOfMonth, DateTime endOfMonth);

        Task<List<AbsenceDto>> FindMyPendingAbsencesAsync(long userId, string status);
        Task<List<AbsenceDto>> FindMyNonPendingAbsencesAsync(long userId, string status);
    }
}
