using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using static Domain.Enums.EnumExtensions;

namespace Domain.Enums
{
    public class EnumExtensions
    {
        public enum AppFeatureType
        {
            Dashboard = 0,
            Master = 1,
            Transaction = 2,
            Report = 3,
            Setting=4,
            Security=5
        }

        public enum Gender
        {
            Male=1,
            Female=2,
            Other=3
        }
        public enum BankAccountType
        {
            Savings = 1,
            Current = 2,
            Salary = 3
        }
        public enum MaritalStatus
        {
            Married=1,
            Unmarried = 2,
            Divorced=3
        }
        public enum LoginStatus
        {
            Success = 1,
            Failed = 2,
            Locked = 3
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
            Urgent = 4,
            Achievement= 5,
            Important=6,
        } 

        public enum AnnouncementPriority
        {
            High=1, 
            Medium = 2,
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
            Scheduled= 1,
            Completed=2,
            Cancelled=3, 
            Expired=4,
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
            ReSubmitted = 7,       // reject के बाद फिर से submit
            ReturnedToEmployee = 8 // an approver sent it back for correction - waiting on the employee, not an approver
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

        // Work From Home request approval workflow - single-level approval
        // (the requesting employee's direct ReportingManagerId, or HR/Admin
        // as an override) unlike Leave/Attendance Regularization's
        // multi-level chain. See Domain/Entities/WfhRequest.cs.
        public enum WfhRequestStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3,
            Cancelled = 4
        }

        // Payslip Request approval workflow - unlike WfhRequestStatus/
        // OnDutyRequestStatus's single-level approval, this is TWO stages:
        // the requesting employee's direct Reporting Manager (or HR/Admin
        // override) first, then Finance (a permission-based role, see
        // AppFeatureConstants.PAYROLL_PAYSLIP_REQUEST) second. Manager
        // approval auto-forwards straight to PendingFinanceAction in the
        // same call (there is no separate manual "forward" step) - see
        // PayslipRequestService.ManagerApproveAsync. Either stage can
        // reject, which is terminal (RejectedByManager / RejectedByFinance)
        // - the employee only ever gets file access after Finance marks the
        // request Completed. See Domain/Entities/PayslipRequest.cs.
        public enum PayslipRequestStatus
        {
            PendingManagerApproval = 1,
            RejectedByManager = 2,
            ApprovedByManager = 3,
            PendingFinanceAction = 4,
            PayslipGenerated = 5,
            Completed = 6,
            RejectedByFinance = 7
        }

        // On Duty request approval workflow - single-level approval (the
        // requesting employee's direct ReportingManagerId, or HR/Admin as
        // an override), identical shape to WfhRequestStatus above but kept
        // as its own enum (a separate type per transactional module is the
        // existing convention in this codebase - see WfhRequestStatus,
        // LeaveApplication's status, etc. - so OD can evolve independently
        // of WFH, e.g. a future OD-only status, without touching WFH). See
        // Domain/Entities/OnDutyRequest.cs.
        public enum OnDutyRequestStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3,
            Cancelled = 4
        }

        // Short Leave request approval workflow - single-level approval (the
        // requesting employee's direct ReportingManagerId, or HR/Admin as an
        // override), same shape as WfhRequestStatus/OnDutyRequestStatus above
        // but its own enum per the existing per-module convention. Unlike
        // WFH/On Duty, approving a Short Leave request does NOT touch
        // Attendance - it deducts a fractional day from the chosen
        // LeaveType's balance instead. See Domain/Entities/ShortLeaveRequest.cs.
        public enum ShortLeaveRequestStatus
        {
            Pending = 1,
            Approved = 2,
            Rejected = 3,
            Cancelled = 4
        }

        // Comp Off candidate review workflow - "System-detected, HR-approved":
        // API/BackgroundServices/CompOffDetectionService.cs auto-detects a day
        // an employee worked extra hours on a Holiday/WeekOff and creates a
        // CompOffCandidate row at PendingReview - it never credits the
        // balance itself. HR must individually Approve (credits
        // CreditedDays into the "Comp Off" LeaveType balance via
        // ILeaveBalanceService.CreditLeaveAsync) or Reject each candidate -
        // see Domain/Entities/CompOffCandidate.cs / CompOffService.
        public enum CompOffCandidateStatus
        {
            PendingReview = 1,
            Approved = 2,
            Rejected = 3
        }

        // Penalty applied once an employee crosses their AttendancePolicy's
        // LateMarkGraceCount (allowed late arrivals per month) - see
        // Domain/Entities/AttendancePolicy.cs.
        public enum LateMarkPenaltyType
        {
            None = 1,
            WarningOnly = 2,
            HalfDayDeduction = 3,
            FullDayDeduction = 4
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

        public enum DocumentType
        {
            // Government ID Proofs

            [Display(Name = "Aadhaar Card")]
            AadhaarCard = 1,

            [Display(Name = "PAN Card")]
            PANCard = 2,

            [Display(Name = "Passport")]
            Passport = 3,

            [Display(Name = "Driving License")]
            DrivingLicense = 4,

            [Display(Name = "Voter ID")]
            VoterID = 5,


            // Employee Documents

            [Display(Name = "Resume")]
            Resume = 6,

            [Display(Name = "Offer Letter")]
            OfferLetter = 7,

            [Display(Name = "Appointment Letter")]
            AppointmentLetter = 8,

            [Display(Name = "Experience Letter")]
            ExperienceLetter = 9,

            [Display(Name = "Relieving Letter")]
            RelievingLetter = 10,

            [Display(Name = "Salary Slip")]
            SalarySlip = 11,


            // Education Documents

            [Display(Name = "Education Certificate")]
            EducationCertificate = 12,

            [Display(Name = "Degree Certificate")]
            DegreeCertificate = 13,

            [Display(Name = "10th Marksheet")]
            Marksheet10th = 14,

            [Display(Name = "12th Marksheet")]
            Marksheet12th = 15,

            [Display(Name = "Graduation Certificate")]
            GraduationCertificate = 16,

            [Display(Name = "Post Graduation Certificate")]
            PostGraduationCertificate = 17,


            // Bank Documents

            [Display(Name = "Bank Passbook")]
            BankPassbook = 18,

            [Display(Name = "Cancel Cheque")]
            CancelCheque = 19,

            [Display(Name = "Bank Statement")]
            BankStatement = 20,


            // HR & Compliance Documents

            [Display(Name = "PF Document")]
            PFDocument = 21,

            [Display(Name = "ESIC Document")]
            ESICDocument = 22,

            [Display(Name = "Medical Certificate")]
            MedicalCertificate = 23,

            [Display(Name = "Address Proof")]
            AddressProof = 24,

            [Display(Name = "Photo")]
            Photo = 25,

            [Display(Name = "Signature")]
            Signature = 26,


            // Other Documents

            [Display(Name = "Birth Certificate")]
            BirthCertificate = 27,

            [Display(Name = "Marriage Certificate")]
            MarriageCertificate = 28,

            [Display(Name = "Police Verification")]
            PoliceVerification = 29,

            [Display(Name = "Other")]
            Other = 30
        }

        public enum LeaveTransactionType
        {
            Allocate = 1,
            Credit = 2,
            Deduct = 3,
            CarryForward = 4
        }

        // Leave Policy Engine (foundational phase) - drives
        // LeaveAccrualService's cycle-boundary detection for
        // LeaveType.AccrualFrequency. None = fully manual (default,
        // backward-compatible for every existing LeaveType).
        public enum LeaveAccrualFrequency
        {
            None = 1,
            Monthly = 2,
            Quarterly = 3,
            Yearly = 4
        }

        // Leave Policy Engine (foundational phase) - restricts a LeaveType
        // to a specific gender (e.g. Maternity/Paternity-style leaves).
        // All = default, no restriction (backward-compatible).
        public enum LeaveApplicableGender
        {
            All = 1,
            Male = 2,
            Female = 3
        }

        public enum Nationality
        {
            [Display(Name = "Indian")]
            Indian = 1,

            [Display(Name = "American")]
            American = 2,

            [Display(Name = "British")]
            British = 3,

            [Display(Name = "Canadian")]
            Canadian = 4,

            [Display(Name = "Australian")]
            Australian = 5,

            [Display(Name = "German")]
            German = 6,

            [Display(Name = "French")]
            French = 7,

            [Display(Name = "Japanese")]
            Japanese = 8,

            [Display(Name = "Chinese")]
            Chinese = 9,

            [Display(Name = "Singaporean")]
            Singaporean = 10,

            [Display(Name = "UAE")]
            UAE = 11,

            [Display(Name = "Saudi Arabian")]
            SaudiArabian = 12,

            [Display(Name = "Nepalese")]
            Nepalese = 13,

            [Display(Name = "Bangladeshi")]
            Bangladeshi = 14,

            [Display(Name = "Sri Lankan")]
            SriLankan = 15,

            [Display(Name = "Pakistani")]
            Pakistani = 16,

            [Display(Name = "Other")]
            Other = 99
        }

        public enum PassportStatus
        {
            [Display(Name = "Not Available")]
            NotAvailable = 0,

            [Display(Name = "Applied")]
            Applied = 1,

            [Display(Name = "Active")]
            Active = 2,

            [Display(Name = "Expired")]
            Expired = 3,

            [Display(Name = "Renewal in Progress")]
            RenewalInProgress = 4,

            [Display(Name = "Cancelled")]
            Cancelled = 5,

            [Display(Name = "Lost")]
            Lost = 6,

            [Display(Name = "Damaged")]
            Damaged = 7,

            [Display(Name = "Surrendered")]
            Surrendered = 8
        }

        public enum EmployeeDocumentType
        {
            Aadhaar = 1,
            PAN = 2,
            Passport = 3,
            DrivingLicense = 4,
            VoterId = 5,

            Resume = 6,
            ProfilePhoto = 7,
            Signature = 8,

            OfferLetter = 9,
            AppointmentLetter = 10,
            JoiningLetter = 11,
            RelievingLetter = 12,
            ExperienceLetter = 13,

            DegreeCertificate = 14,
            DiplomaCertificate = 15,
            Marksheet = 16,

            SalarySlip = 17,
            BankPassbook = 18,
            CancelledCheque = 19,

            BirthCertificate = 20,
            MarriageCertificate = 21,

            MedicalCertificate = 22,
            FitnessCertificate = 23,

            PoliceVerification = 24,
            BackgroundVerification = 25,

            PFDocument = 26,
            ESICDocument = 27,

            Other = 99
        }

        // Help & Support - ticket workflow.
        public enum TicketCategory
        {
            Technical = 1,
            HR = 2,
            Payroll = 3,
            Leave = 4,
            IT = 5,
            General = 6,
            Other = 7
        }

        public enum TicketPriority
        {
            Low = 1,
            Medium = 2,
            High = 3,
            Urgent = 4
        }

        public enum TicketStatus
        {
            Open = 1,
            InProgress = 2,
            Resolved = 3,
            Closed = 4
        }

        // The specific leave-workflow event a notification was raised for.
        // Kept narrowly scoped to what the Leave Application chain actually
        // needs (Apply/Approve/Reject/SendBack/Resubmit/Cancel) rather than a
        // generic catch-all. This drives the Title/message text composed in
        // LeaveApplicationService; it is NOT persisted as its own column -
        // the existing Notification.NotificationType string field still
        // carries the UI severity (Info/Success/Warning/Error) that the
        // Notification views already switch on, while this event maps to
        // that severity and to the human-readable title.
        public enum LeaveNotificationEvent
        {
            LeaveApplied = 1,
            LeaveApproved = 2,
            LeaveRejected = 3,
            LeaveSentBack = 4,
            LeaveResubmitted = 5,
            LeaveCancelled = 6,
            ApprovalPending = 7,
            General = 8
        }

        // =====================================================
        // EMPLOYEE ONBOARDING
        // An OnboardingCase always belongs to an Employee that already
        // exists (started manually by HR, or auto-started when a
        // Recruitment Candidate is converted into an Employee - see
        // Employee.CandidateId / EmployeeDto.CandidateId). It spans 5
        // conceptual stages (OnboardingStageType), each tracked via a
        // checklist of OnboardingChecklistItem rows.
        // =====================================================

        public enum OnboardingCaseStatus
        {
            NotStarted = 1,
            InProgress = 2,
            Completed = 3,
            OnHold = 4,
            Cancelled = 5
        }

        public enum OnboardingStageType
        {
            DocumentVerification = 1,
            WelcomeKit = 2,
            EmployeeCreation = 3,
            AssetAllocation = 4,
            JoiningChecklist = 5
        }

        public enum OnboardingChecklistItemStatus
        {
            Pending = 1,
            InProgress = 2,
            Completed = 3,
            NotApplicable = 4
        }

        // =====================================================
        // PROBATION & CONFIRMATION (Maker-Checker)
        // =====================================================
        // Foundational Maker-Checker (segregation-of-duties) workflow
        // status for Domain/Entities/ProbationConfirmation.cs - a MAKER
        // (HR staff holding Create permission on PROBATION_CONFIRMATION)
        // proposes an outcome for an employee's probation, and a DIFFERENT
        // person acting as CHECKER (holding Approve permission) must
        // Approve or Reject it - see
        // ProbationConfirmationService.ApproveAsync/RejectAsync for the
        // actingUserId != MakerId invariant (no override, not even for
        // HR/Admin). This exact field shape (MakerId/MakerActionOn/
        // MakerRemarks/Status/CheckerId/CheckerActionOn/CheckerRemarks) is
        // meant to be replicated by the later PIP Outcome and Employee
        // Transfer features - per this codebase's existing convention of a
        // separate, identically-shaped enum per module (see
        // WfhRequestStatus/OnDutyRequestStatus/ShortLeaveRequestStatus
        // above), those features should define their OWN
        // PipOutcomeStatus/TransferStatus enums with this same shape
        // rather than reusing this one.
        public enum ProbationConfirmationStatus
        {
            PendingChecker = 1,
            Approved = 2,
            Rejected = 3
        }

        // The Maker's proposed outcome for an employee's probation review -
        // see Domain/Entities/ProbationConfirmation.cs.
        public enum ProbationRecommendation
        {
            Confirm = 1,       // Confirm employment - Employee.ConfirmationDate set, EmploymentType flips Probation -> Permanent
            Extend = 2,        // Extend probation - Employee.ProbationEndDate set to ExtendedProbationEndDate
            PlaceOnPIP = 3,    // Hand-off to the (later) PIP module - no Employee change made here
            Terminate = 4      // Terminate during probation - Employee.RelievingDate set
        }

        // =====================================================
        // TAXATION (Income Tax / TDS - India)
        // =====================================================
        // Enterprise Taxation Module - Domain/Entities/TaxSlab.cs,
        // TaxDeclaration.cs, EmployeeTaxComputation.cs and
        // Application/Services/Taxation/*. Indian Income Tax offers
        // salaried employees a choice each Financial Year between the Old
        // Regime (higher slab rates, but Chapter VI-A deductions such as
        // 80C/80D/HRA/24(b) are allowed) and the New Regime (lower slab
        // rates, but almost no deductions) - see
        // TaxComputationService.ComputeAsync for the exact calculation.
        public enum TaxRegime
        {
            Old = 1,
            New = 2
        }

        // Employee self-service investment declaration workflow - NOT a
        // Maker-Checker (segregation-of-duties) feature like
        // ProbationConfirmationStatus above, since the "maker" (the
        // employee) is declaring facts about themselves, not proposing an
        // action on someone else - the closest existing pattern is
        // WfhRequestStatus's employee-submits/manager-or-HR-approves shape.
        // Draft -> Submitted (by the employee) -> Verified/Rejected (by
        // HR, holding Approve permission on TAX_DECLARATION). Only a
        // Verified declaration feeds EmployeeTaxComputation.
        public enum TaxDeclarationStatus
        {
            Draft = 1,
            Submitted = 2,
            Verified = 3,
            Rejected = 4
        }

        // =====================================================
        // PERFORMANCE IMPROVEMENT PLAN (PIP) (Maker-Checker)
        // =====================================================
        // Phase 2 of the "Probation & Confirmation" module - see
        // Domain/Entities/PipRecord.cs. A PipRecord is only ever created
        // as a system/HR hand-off from an Approved ProbationConfirmation
        // row with Recommendation == PlaceOnPIP (no maker-checker gate on
        // creation itself). The maker-checker gate applies to the FINAL
        // OUTCOME RESOLUTION instead - see PipService.ProposeOutcomeAsync/
        // ApproveOutcomeAsync/RejectOutcomeAsync for the actingUserId !=
        // MakerId invariant (identical to ProbationConfirmationStatus's,
        // no override). Kept as its own enum rather than reusing
        // ProbationConfirmationStatus, per this codebase's per-module enum
        // convention (see WfhRequestStatus/OnDutyRequestStatus/
        // ShortLeaveRequestStatus above).
        public enum PipOutcomeStatus
        {
            PendingChecker = 1,
            Approved = 2,
            Rejected = 3
        }

        // The PIP's live/proposed final outcome - see
        // PipRecord.FinalOutcome (live, starts InProgress) vs.
        // PipRecord.ProposedFinalOutcome (staged Maker proposal, cleared
        // on Reject).
        public enum PipFinalOutcome
        {
            InProgress = 1,    // PIP still running - no outcome resolved yet
            Successful = 2,    // Approved as Successful - mirrors Confirm: Employee.ConfirmationDate set, EmploymentType Probation -> Permanent
            Unsuccessful = 3   // Approved as Unsuccessful - mirrors Terminate: Employee.RelievingDate set
        }

        // =====================================================
        // EMPLOYEE TRANSFER (Maker-Checker)
        // =====================================================
        // Phase 3 of the "Probation & Confirmation" (Employee Lifecycle)
        // module - see Domain/Entities/EmployeeTransfer.cs. A MAKER (HR
        // staff holding Create permission on
        // AppFeatureConstants.EMPLOYEE_TRANSFER) proposes new Company/
        // Branch/Department/Designation/ReportingManager values for an
        // Employee, and a DIFFERENT person acting as CHECKER (holding
        // Approve permission) must Approve or Reject it before the change
        // is applied to the live Employee record - see
        // EmployeeTransferService.ApproveAsync/RejectAsync for the
        // actingUserId != MakerId invariant (identical to
        // ProbationConfirmationStatus's/PipOutcomeStatus's, no override).
        // Kept as its own enum rather than reusing either of those, per
        // this codebase's per-module enum convention.
        public enum TransferStatus
        {
            PendingChecker = 1,
            Approved = 2,
            Rejected = 3
        }

        // =====================================================
        // EMPLOYEE FEEDBACK
        // =====================================================
        // Phase 4 of the "Probation & Confirmation" (Employee Lifecycle)
        // module - see Domain/Entities/EmployeeFeedback.cs. UNLIKE the
        // preceding three phases (ProbationConfirmation/PipRecord/
        // EmployeeTransfer), this feature has NO maker-checker workflow -
        // Feedback is general ongoing performance feedback usable any time
        // for any employee, plain CRUD only. Category classifies the kind
        // of feedback being given - see EmployeeFeedbackService for the
        // Reporting-Manager-or-HR authorization rules and the
        // IsVisibleToEmployee-driven self-service visibility filter.
        public enum FeedbackCategory
        {
            General = 1,
            Performance = 2,
            Behavioral = 3,
            Skill = 4,
            Attendance = 5,
            Other = 6
        }
    }

    public static class EnumHelper
    {
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
