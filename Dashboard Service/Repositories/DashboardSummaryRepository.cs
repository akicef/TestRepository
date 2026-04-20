using Dapper;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Dashboard_Service.Models;

namespace Dashboard_Service.Repositories
{
    public class DashboardSummaryRepository : IDashboardSummaryRepository
    {
        private readonly string _connectionString;

        public DashboardSummaryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection CreateConnection() => new NpgsqlConnection(_connectionString);

        public async Task<long> CountTotalInvoicesAsync()
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from invoices");
        }

        public async Task<long> CountPendingInvoicesAsync()
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from invoices where upper(status) = 'PENDING'");
        }

        public async Task<long> CountOverdueInvoicesAsync(DateTime today)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from invoices where (upper(status) = 'OVERDUE') or (upper(status) = 'PENDING' and due_date < @Today)", new { Today = today.Date });
        }

        public async Task<long> CountOpenComplaintsAsync()
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from complaints where upper(status) = 'OPEN'");
        }

        public async Task<long> CountExpiringContractsAsync(DateTime today, DateTime endDate)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from contracts where end_date is not null and end_date between @Today and @EndDate", new { Today = today.Date, EndDate = endDate.Date });
        }

        public async Task<long> CountEmployeesAsync()
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from employees");
        }

        public async Task<long> CountAbsenceRequestsByStatusAsync(string status)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(*) from absence_requests where status = @Status", new { Status = status });
        }

        public async Task<long> CountAbsentTodayAsync(DateTime today, string status)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(distinct employee_id) from absence_requests where status = @Status and date_from <= @Today and date_to >= @Today", new { Status = status, Today = today.Date });
        }

        public async Task<long> CountAbsentThisWeekAsync(DateTime startOfWeek, DateTime endOfWeek, string status)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("select count(distinct employee_id) from absence_requests where status = @Status and date_from <= @EndOfWeek and date_to >= @StartOfWeek", new { Status = status, StartOfWeek = startOfWeek.Date, EndOfWeek = endOfWeek.Date });
        }

        public async Task<long> CountMyAbsencesByStatusAsync(long userId, string status)
        {
            using var db = CreateConnection();
            return await db.ExecuteScalarAsync<long>("SELECT COUNT(*) FROM absence_requests a WHERE a.employee_id = @UserId AND a.status = @Status", new { UserId = userId, Status = status });
        }

        public async Task<DateTime?> FindMyNextApprovedAbsenceStartAsync(long userId, DateTime today, string status)
        {
            using var db = CreateConnection();
            var result = await db.ExecuteScalarAsync<DateTime?>("SELECT MIN(date_from) FROM absence_requests WHERE employee_id = @UserId AND status = @Status AND date_from >= @Today", new { UserId = userId, Status = status, Today = today.Date });
            return result;
        }

        // Tables
        public async Task<List<InvoiceDto>> FindLatestInvoicesAsync()
        {
            using var db = CreateConnection();
            var sql = @"SELECT i.id AS Id, p.name AS PartyName, i.amount AS Amount, i.status AS Status, i.due_date AS DueDate FROM invoices i JOIN parties p ON p.id = i.party_id ORDER BY i.id DESC LIMIT 20";
            var rows = await db.QueryAsync<InvoiceDto>(sql);
            return new List<InvoiceDto>(rows);
        }

        public async Task<List<InvoiceDto>> FindOverdueInvoicesAsync()
        {
            using var db = CreateConnection();
            var sql = @"SELECT i.id AS Id, p.name AS PartyName, i.amount AS Amount, i.status AS Status, i.due_date AS DueDate FROM invoices i JOIN parties p ON p.id = i.party_id WHERE i.status = 'OVERDUE' ORDER BY i.due_date ASC";
            var rows = await db.QueryAsync<InvoiceDto>(sql);
            return new List<InvoiceDto>(rows);
        }

        public async Task<List<ComplaintDto>> FindOpenComplaintsAsync()
        {
            using var db = CreateConnection();
            var sql = @"SELECT c.id AS Id, c.title AS Title, c.description AS Description FROM complaints c WHERE c.status = 'OPEN' ORDER BY c.id DESC";
            var rows = await db.QueryAsync<ComplaintDto>(sql);
            return new List<ComplaintDto>(rows);
        }

        public async Task<List<ContractDto>> FindExpiringContractsAsync(DateTime today, DateTime endDate)
        {
            using var db = CreateConnection();
            var sql = @"SELECT c.id AS Id, p.name AS PartyName, c.status AS Status, c.end_date AS EndDate FROM contracts c JOIN parties p ON p.id = c.party_id WHERE c.end_date IS NOT NULL AND c.end_date BETWEEN @Today AND @EndDate ORDER BY c.end_date ASC";
            var rows = await db.QueryAsync<ContractDto>(sql, new { Today = today.Date, EndDate = endDate.Date });
            return new List<ContractDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindAbsencesByStatusAsync(string status)
        {
            using var db = CreateConnection();
            var sql = @"SELECT e.id AS Id, e.first_name || ' ' || e.last_name as EmployeeName, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a JOIN employees e ON e.user_id = a.employee_id WHERE a.status = @Status ORDER BY a.date_from ASC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { Status = status });
            return new List<AbsenceDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindTodaysAbsencesAsync(DateTime today)
        {
            using var db = CreateConnection();
            var sql = @"SELECT e.id AS Id, e.first_name || ' ' || e.last_name as EmployeeName, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a JOIN employees e ON e.user_id  = a.employee_id WHERE a.status = 'APPROVED' AND a.date_from <= @Today AND a.date_to >= @Today ORDER BY a.date_from ASC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { Today = today.Date });
            return new List<AbsenceDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindAbsencesThisWeekAsync(DateTime startOfWeek, DateTime endOfWeek)
        {
            using var db = CreateConnection();
            var sql = @"SELECT e.id AS Id, e.first_name || ' ' || e.last_name as EmployeeName, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a JOIN employees e ON e.user_id  = a.employee_id WHERE a.status = 'APPROVED' AND a.date_from <= @EndOfWeek AND a.date_to >= @StartOfWeek ORDER BY a.date_from ASC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { StartOfWeek = startOfWeek.Date, EndOfWeek = endOfWeek.Date });
            return new List<AbsenceDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindAbsencesThisMonthAsync(DateTime startOfMonth, DateTime endOfMonth)
        {
            using var db = CreateConnection();
            var sql = @"SELECT e.id AS Id, e.first_name || ' ' || e.last_name as EmployeeName, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a JOIN employees e ON a.employee_id  =e.user_id WHERE a.status = 'APPROVED' AND a.date_from <= @EndOfMonth AND a.date_to >= @StartOfMonth ORDER BY a.date_from ASC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { StartOfMonth = startOfMonth.Date, EndOfMonth = endOfMonth.Date });
            return new List<AbsenceDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindMyPendingAbsencesAsync(long userId, string status)
        {
            using var db = CreateConnection();
            var sql = @"SELECT a.id AS Id, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a WHERE a.employee_id = @UserId AND a.status = @Status ORDER BY a.date_from DESC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { UserId = userId, Status = status });
            return new List<AbsenceDto>(rows);
        }

        public async Task<List<AbsenceDto>> FindMyNonPendingAbsencesAsync(long userId, string status)
        {
            using var db = CreateConnection();
            var sql = @"SELECT a.id AS Id, a.type AS Type, a.date_from AS DateFrom, a.date_to AS DateTo, a.status AS Status FROM absence_requests a WHERE a.employee_id = @UserId AND a.status <> @Status ORDER BY a.date_from DESC";
            var rows = await db.QueryAsync<AbsenceDto>(sql, new { UserId = userId, Status = status });
            return new List<AbsenceDto>(rows);
        }
    }
}
