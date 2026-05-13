using Microsoft.AspNetCore.Mvc.Rendering;

namespace APP.Helpers
{
    public class EnumExtensions
    {
        public enum Gender
        {
            Male = 1,
            Female = 2,
            Other = 3
        }
        public enum MaritalStatus
        {
            Married = 1,
            Unmarried = 2,
            Divorced = 3
        }
        public enum SalaryComponentType
        {
            Earning = 1,
            Deduction = 2
        }
        public enum HalfDayType
        {
            FirstHalf = 1,
            SecondHalf = 2
        }
        public enum PunchType
        {
            In = 1,
            Out = 2,
            BreakIn = 3,
            BreakOut = 4
        }

        public enum LeaveStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3,
            Cancelled = 4
        }
        public enum AnnouncementType
        {
            General = 1,
            Policy = 2,
            Holiday = 3,
            Urgent = 4
        }
        public enum EventType
        {
            Meeting = 1,
            Holiday = 2,
            Training = 3,
            Celebration = 4
        }
        public enum EventParticipantStatus
        {
            Invited = 1,
            Accepted = 2,
            Declined = 3
        }

        public enum AllocationStatus
        {
            NotAllocated = 0,     // अभी assign नहीं हुआ

            Allocated = 1,        // successfully assign हो गया
            PartiallyAllocated = 2, // partially assign (multi-resource case)

            InProgress = 3,       // allocation use में है (active)

            Completed = 4,        // allocation पूरा हो गया
            Released = 5,         // allocation free कर दिया गया

            Transferred = 6,      // एक से दूसरे को transfer किया गया

            Cancelled = 7,        // allocation cancel
            Expired = 8           // time खत्म (contract / booking)
        }
        public enum CandidateStatus
        {
            Applied = 1,
            Shortlisted = 2,
            Interview = 3,
            Selected = 4,
            Rejected = 5
        }

        public enum InterviewScheduleStatus
        {

            None = 0,
            Scheduled = 1,
            Completed = 2,
            Cancelled = 3,
            Expired = 4,
        }

        public enum JobStatus
        {
            Open = 1,
            Closed = 2,
            OnHold = 3
        }

        public enum InterviewScheduleMode
        {
            Online = 1,
            Offline = 2
        }

        public enum ApprovalStatus
        {
            Draft = 0,        // अभी submit नहीं हुआ
            Pending = 1,      // approval के लिए pending

            Approved = 2,     // approve हो गया
            Rejected = 3,     // reject हो गया

            Cancelled = 4,    // user ने cancel किया
            OnHold = 5,       // temporarily hold पर

            PartiallyApproved = 6, // multi-level approval में partially approve
            ReSubmitted = 7        // reject के बाद फिर से submit
        }

        public enum EmploymentType
        {
            Unknown = 0,

            Permanent = 1,          // Full-time employee (regular)
            Contract = 2,           // Fixed अवधि का contract
            Temporary = 3,          // Short-term / temporary

            PartTime = 4,           // Part-time job
            Intern = 5,             // Internship

            Freelancer = 6,         // Project based / freelance
            Consultant = 7,         // Expert advisory role

            Trainee = 8,            // Training phase employee
            Apprentice = 9,         // सीखने के दौरान काम

            Probation = 10,         // Trial period employee

            Outsourced = 11,        // Third-party payroll
            DailyWage = 12          // Daily basis worker
        }

        public enum PaymentStatus
        {
            Pending = 1,        // Invoice created but no payment yet
            PartiallyPaid = 2,  // Some amount paid
            Paid = 3,           // Fully paid

            Failed = 4,         // Payment attempt failed (gateway/bank)
            Cancelled = 5,      // Payment cancelled by user/system

            Refunded = 6,       // Fully refunded
            PartiallyRefunded = 7, // Partial refund done

            Overdue = 8         // Payment date crossed without full payment
        }

        public enum AttendanceStatus
        {
            None = 0,

            Present = 1,
            Absent = 2,
            Leave = 3,
            Holiday = 4,
            WeekOff = 5,

            HalfDay = 6,
            Late = 7,
            EarlyExit = 8,

            WorkFromHome = 9,
            OnDuty = 10,          // Official work outside office
            MissPunch = 11,       // Forgot punch-in/out

            Overtime = 12,
            CompOff = 13,         // Compensatory Off

            PaidLeave = 14,
            UnpaidLeave = 15,

            SandwichLeave = 16,   // Leave between holidays/weekoffs

            Training = 17,
            BusinessTrip = 18
        }
        public enum FinancialYearStatus
        {
            Upcoming = 0,
            Open = 1,        // Active - transactions allowed
            Closed = 2,      // Locked - no changes allowed
            Archived = 3,    // Historical - read-only
            Frozen = 4       // Temporarily locked (audit/review)
        }
        public enum BusinessOwnershipType
        {
            Unknown = 0,

            Proprietorship = 1,        // एक व्यक्ति द्वारा चलाया गया व्यवसाय
            Partnership = 2,           // 2 या अधिक partners

            LLP = 3,                   // Limited Liability Partnership
            PrivateLimited = 4,        // Pvt Ltd Company
            PublicLimited = 5,         // Public Ltd Company

            OnePersonCompany = 6,      // OPC

            HUF = 7,                   // Hindu Undivided Family
            Cooperative = 8,           // Cooperative Society

            Trust = 9,                 // Trust
            NGO = 10,                  // Non-Profit Organization

            Government = 11            // Govt Organization
        }
        public enum BusinessCategory
        {
            Unknown = 0,

            Manufacturing = 1,     // उत्पादन करने वाली कंपनी
            Trading = 2,           // खरीद-बिक्री (Reseller / Wholesaler / Retailer)
            Service = 3,           // Service based company

            Retail = 4,            // Retail दुकान
            Wholesale = 5,         // थोक व्यापार

            Distributor = 6,       // Distributor
            Dealer = 7,            // Dealer / Channel Partner

            ECommerce = 8,         // Online business
            Exporter = 9,          // Export business
            Importer = 10,         // Import business

            Construction = 11,     // Construction / Builder
            Logistics = 12,        // Transport / Logistics

            Healthcare = 13,       // Hospital / Clinic
            Education = 14,        // School / College

            Finance = 15,          // Finance / NBFC
            ITSoftware = 16,       // IT / Software company

            Agriculture = 17,      // Farming / Agro business
            Hospitality = 18       // Hotel / Restaurant
        }
    }

    public static class EnumHelper
    {
        /// <summary>
        /// Get dropdown list for any enum
        /// </summary>
        public static List<SelectListItem> GetEnumList<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .Select(x => new SelectListItem
                       {
                           Text = x.ToString(),
                           Value = Convert.ToInt32(x).ToString()
                       })
                       .ToList();
        }

        /// <summary>
        /// Get enum name by value
        /// </summary>
        public static string GetEnumName<TEnum>(int value) where TEnum : Enum
        {
            return Enum.IsDefined(typeof(TEnum), value)
                ? Enum.GetName(typeof(TEnum), value)
                : "Unknown";
        }

        /// <summary>
        /// Convert int/string to enum
        /// </summary>
        public static TEnum GetEnumValue<TEnum>(object value) where TEnum : struct, Enum
        {
            if (Enum.TryParse(value.ToString(), true, out TEnum result))
            {
                return result;
            }

            return default;
        }

        /// <summary>
        /// Check enum value exists
        /// </summary>
        public static bool IsValidEnum<TEnum>(object value) where TEnum : Enum
        {
            return Enum.TryParse(typeof(TEnum), value.ToString(), true, out _);
        }

        /// <summary>
        /// Get all enum values
        /// </summary>
        public static List<TEnum> GetEnumValues<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .ToList();
        }

        /// <summary>
        /// Get dictionary of enum
        /// </summary>
        public static Dictionary<int, string> GetEnumDictionary<TEnum>() where TEnum : Enum
        {
            return Enum.GetValues(typeof(TEnum))
                       .Cast<TEnum>()
                       .ToDictionary(
                           x => Convert.ToInt32(x),
                           x => x.ToString()
                       );
        }
    }
}
