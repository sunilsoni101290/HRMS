using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Helper
{
    public static class AppFeatureConstants
    {
        // =====================================================
        // COMMON
        // =====================================================

        public const string HRMS = "HRMS";

        // Common Actions
        public const string ACTION_INDEX = "Index";
        public const string ACTION_CREATE = "Create";
        public const string ACTION_EDIT = "Edit";
        public const string ACTION_DELETE = "Delete";
        public const string ACTION_DETAILS = "Details";
        public const string ACTION_APPROVE = "Approve";
        public const string ACTION_EXPORT = "Export";
        public const string ACTION_PRINT = "Print";

        // =====================================================
        // DASHBOARD
        // =====================================================

        public const string DASHBOARD = "DASHBOARD";
        public const string DASHBOARD_CONTROLLER = "Dashboard";
        public const string DASHBOARD_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_DASHBOARD = "EMPLOYEE_DASHBOARD";
        public const string EMPLOYEE_DASHBOARD_CONTROLLER = "EmployeeDashboard";
        public const string EMPLOYEE_DASHBOARD_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_TASK = "EMPLOYEE_TASK";
        public const string EMPLOYEE_TASK_CONTROLLER = "EmployeeTask";
        public const string EMPLOYEE_TASK_ACTION = ACTION_INDEX;

        // =====================================================
        // MASTER DATA
        // =====================================================

        public const string MASTER = "MASTER";

        public const string COUNTRY = "COUNTRY";
        public const string COUNTRY_CONTROLLER = "Country";
        public const string COUNTRY_ACTION = ACTION_INDEX;

        public const string STATE = "STATE";
        public const string STATE_CONTROLLER = "State";
        public const string STATE_ACTION = ACTION_INDEX;

        public const string CITY = "CITY";
        public const string CITY_CONTROLLER = "City";
        public const string CITY_ACTION = ACTION_INDEX;

        public const string HOLIDAY_GROUP = "HOLIDAY_GROUP";
        public const string HOLIDAY_GROUP_CONTROLLER = "HolidayGroup";
        public const string HOLIDAY_GROUP_ACTION = ACTION_INDEX;

        public const string HOLIDAY = "HOLIDAY";
        public const string HOLIDAY_CONTROLLER = "Holiday";
        public const string HOLIDAY_ACTION = ACTION_INDEX;

        public const string WEEKOFF = "Week Off";
        public const string WEEKOFF_CONTROLLER = "WeekOff";
        public const string WEEKOFF_ACTION = ACTION_INDEX;

        public const string FINANCIAL_YEAR = "FINANCIAL_YEAR";
        public const string FINANCIAL_YEAR_CONTROLLER = "FinancialYear";
        public const string FINANCIAL_YEAR_ACTION = ACTION_INDEX;

        // =====================================================
        // SECURITY
        // =====================================================

        public const string SECURITY = "SECURITY";

        public const string ROLE = "ROLE";
        public const string ROLE_CONTROLLER = "Role";
        public const string ROLE_ACTION = ACTION_INDEX;

        public const string USER = "USER";
        public const string USER_CONTROLLER = "User";
        public const string USER_ACTION = ACTION_INDEX;

        public const string PERMISSION = "PERMISSION";
        public const string PERMISSION_CONTROLLER = "Permission";
        public const string PERMISSION_ACTION = ACTION_INDEX;

        public const string LOGIN_HISTORY = "LOGIN_HISTORY";
        public const string LOGIN_HISTORY_CONTROLLER = "LoginHistory";
        public const string LOGIN_HISTORY_ACTION = ACTION_INDEX;

        public const string APP_FEATURE = "APP_FEATURE";
        public const string APP_FEATURE_CONTROLLER = "AppFeatures";
        public const string APP_FEATURE_ACTION = ACTION_INDEX;

        // Centralized Error Log - System Configurator/Super Admin only (no
        // dedicated "Auditor" role exists in this codebase, same gap noted
        // on LOAN_ADVANCE_AUDIT_LOG above; Super Admin/System Configurator
        // cover it via DbSeeder.ReconcilePermissionsAsync's fullAccessRoles
        // loop - no HR Manager grant is added for this feature). Populated
        // automatically by API/Middleware/ExceptionMiddleware for every
        // unhandled exception, plus explicit IErrorLogService.LogAsync
        // calls from background/non-HTTP code (BiometricAgent Worker, etc.)
        // - see Application/Services/ErrorLogs/ErrorLogService.cs.
        public const string ERROR_LOG = "ERROR_LOG";
        public const string ERROR_LOG_CONTROLLER = "ErrorLog";
        public const string ERROR_LOG_ACTION = ACTION_INDEX;

        // =====================================================
        // EMPLOYEE MANAGEMENT
        // =====================================================

        public const string EMPLOYEE_MANAGEMENT = "EMPLOYEE_MANAGEMENT";

        public const string EMPLOYEE = "EMPLOYEE";
        public const string EMPLOYEE_CONTROLLER = "Employee";
        public const string EMPLOYEE_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_DOCUMENT = "EMPLOYEE_DOCUMENT";
        public const string EMPLOYEE_DOCUMENT_CONTROLLER = "EmployeeDocument";
        public const string EMPLOYEE_DOCUMENT_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_SHIFT = "EMPLOYEE_SHIFT";
        public const string EMPLOYEE_SHIFT_CONTROLLER = "EmployeeShift";
        public const string EMPLOYEE_SHIFT_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_BANK = "EMPLOYEE_BANK";
        public const string EMPLOYEE_BANK_CONTROLLER = "EmployeeBank";
        public const string EMPLOYEE_BANK_ACTION = ACTION_INDEX;

        public const string EMPLOYEE_PF_ESIC = "EMPLOYEE_PF_ESIC";
        public const string EMPLOYEE_PF_ESIC_CONTROLLER = "EmployeePFESIC";
        public const string EMPLOYEE_PF_ESIC_ACTION = ACTION_INDEX;

        // =====================================================
        // ORGANIZATION
        // =====================================================

        public const string ORGANIZATION = "ORGANIZATION";

        public const string COMPANY = "COMPANY";
        public const string COMPANY_CONTROLLER = "Company";
        public const string COMPANY_ACTION = ACTION_INDEX;

        public const string DEPARTMENT = "DEPARTMENT";
        public const string DEPARTMENT_CONTROLLER = "Department";
        public const string DEPARTMENT_ACTION = ACTION_INDEX;

        public const string DESIGNATION = "DESIGNATION";
        public const string DESIGNATION_CONTROLLER = "Designation";
        public const string DESIGNATION_ACTION = ACTION_INDEX;

        public const string BRANCH = "BRANCH";
        public const string BRANCH_CONTROLLER = "Branch";
        public const string BRANCH_ACTION = ACTION_INDEX;

        public const string LOCATION = "LOCATION";
        public const string LOCATION_CONTROLLER = "Location";
        public const string LOCATION_ACTION = ACTION_INDEX;

        // =====================================================
        // ATTENDANCE
        // =====================================================

        public const string ATTENDANCE_MANAGEMENT = "ATTENDANCE_MANAGEMENT";

        public const string ATTENDANCE = "ATTENDANCE";
        public const string ATTENDANCE_CONTROLLER = "Attendance";
        public const string ATTENDANCE_ACTION = ACTION_INDEX;

        public const string ATTENDANCE_LOG = "ATTENDANCE_LOG";
        public const string ATTENDANCE_LOG_CONTROLLER = "AttendanceLog";
        public const string ATTENDANCE_LOG_ACTION = ACTION_INDEX;

        public const string SHIFT = "SHIFT";
        public const string SHIFT_CONTROLLER = "Shift";
        public const string SHIFT_ACTION = ACTION_INDEX;

        public const string BIOMETRIC_DEVICE = "BIOMETRIC_DEVICE";
        public const string BIOMETRIC_DEVICE_CONTROLLER = "BiometricDevice";
        public const string BIOMETRIC_DEVICE_ACTION = ACTION_INDEX;

        public const string BIOMETRIC_DEVICE_HEALTH = "BIOMETRIC_DEVICE_HEALTH";
        public const string BIOMETRIC_DEVICE_HEALTH_CONTROLLER = "BiometricDevice";
        public const string BIOMETRIC_DEVICE_HEALTH_ACTION = "Health";

        public const string EMPLOYEE_BIOMETRIC_MAPPING = "EMPLOYEE_BIOMETRIC_MAPPING";
        public const string EMPLOYEE_BIOMETRIC_MAPPING_CONTROLLER = "EmployeeBiometricMapping";
        public const string EMPLOYEE_BIOMETRIC_MAPPING_ACTION = ACTION_INDEX;

        // eSSL eTimeTrackLite1 direct-SQL attendance integration - one admin
        // screen (Settings / Sync Logs / Unmapped Employees as tabs, not
        // separate features) under Attendance Management, same grouping as
        // Biometric Devices above. See EsslAttendanceController (API + APP).
        public const string ESSL_INTEGRATION = "ESSL_INTEGRATION";
        public const string ESSL_INTEGRATION_CONTROLLER = "EsslAttendance";
        public const string ESSL_INTEGRATION_ACTION = ACTION_INDEX;

        public const string ATTENDANCE_REGULARIZATION = "ATTENDANCE_REGULARIZATION";
        public const string ATTENDANCE_REGULARIZATION_CONTROLLER = "AttendanceRegularization";
        public const string ATTENDANCE_REGULARIZATION_ACTION = ACTION_INDEX;

        // Company-wide attendance rules (separate from Shift's per-shift
        // timing) - see Domain/Entities/AttendancePolicy.cs.
        public const string ATTENDANCE_POLICY = "ATTENDANCE_POLICY";
        public const string ATTENDANCE_POLICY_CONTROLLER = "AttendancePolicy";
        public const string ATTENDANCE_POLICY_ACTION = ACTION_INDEX;

        // Calendar/Team/Summary/Dashboard aggregation endpoints - see
        // API/Controllers/AttendanceInsightsController.cs.
        public const string ATTENDANCE_INSIGHTS = "ATTENDANCE_INSIGHTS";
        public const string ATTENDANCE_INSIGHTS_CONTROLLER = "AttendanceInsights";
        public const string ATTENDANCE_INSIGHTS_ACTION = ACTION_INDEX;

        // Menu-visible screens backed by AttendanceInsightsController - each
        // maps to its own APP-side controller/action so they show up as
        // separate sidebar links under Attendance Management.
        public const string ATTENDANCE_CALENDAR = "ATTENDANCE_CALENDAR";
        public const string ATTENDANCE_CALENDAR_CONTROLLER = "Attendance";
        public const string ATTENDANCE_CALENDAR_ACTION = "Calendar";

        public const string TEAM_ATTENDANCE = "TEAM_ATTENDANCE";
        public const string TEAM_ATTENDANCE_CONTROLLER = "TeamAttendance";
        public const string TEAM_ATTENDANCE_ACTION = ACTION_INDEX;

        public const string ATTENDANCE_SUMMARY = "ATTENDANCE_SUMMARY";
        public const string ATTENDANCE_SUMMARY_CONTROLLER = "AttendanceSummary";
        public const string ATTENDANCE_SUMMARY_ACTION = ACTION_INDEX;

        public const string ATTENDANCE_DASHBOARD = "ATTENDANCE_DASHBOARD";
        public const string ATTENDANCE_DASHBOARD_CONTROLLER = "AttendanceDashboard";
        public const string ATTENDANCE_DASHBOARD_ACTION = ACTION_INDEX;

        // Work From Home request - employee-submitted date-range request,
        // single-level approval (Reporting Manager or HR/Admin override) -
        // see Domain/Entities/WfhRequest.cs.
        public const string WFH_REQUEST = "WFH_REQUEST";
        public const string WFH_REQUEST_CONTROLLER = "WfhRequest";
        public const string WFH_REQUEST_ACTION = ACTION_INDEX;

        // On Duty request - employee-submitted date-range request for
        // official work carried out away from the office (client visit/site
        // visit/training, etc.), single-level approval (Reporting Manager
        // or HR/Admin override) - near-exact mirror of WFH_REQUEST above,
        // see Domain/Entities/OnDutyRequest.cs.
        public const string ON_DUTY_REQUEST = "ON_DUTY_REQUEST";
        public const string ON_DUTY_REQUEST_CONTROLLER = "OnDutyRequest";
        public const string ON_DUTY_REQUEST_ACTION = ACTION_INDEX;

        // Short Leave request - employee-submitted request for a FEW HOURS
        // off during a working day, single-level approval (Reporting
        // Manager or HR/Admin override) - same dual-audience shape as
        // WFH_REQUEST/ON_DUTY_REQUEST above. Unlike those, approval deducts
        // a fractional day from a chosen LeaveType's balance instead of
        // writing back to Attendance - see
        // Domain/Entities/ShortLeaveRequest.cs.
        public const string SHORT_LEAVE_REQUEST = "SHORT_LEAVE_REQUEST";
        public const string SHORT_LEAVE_REQUEST_CONTROLLER = "ShortLeaveRequest";
        public const string SHORT_LEAVE_REQUEST_ACTION = ACTION_INDEX;

        // Comp Off ("System-detected, HR-approved") - candidates are
        // auto-created by API/BackgroundServices/CompOffDetectionService.cs
        // (a background job, never a user action) when an employee works
        // extra hours on a Holiday/WeekOff; HR individually reviews each one
        // (View/Approve) via CompOffController - see
        // Domain/Entities/CompOffCandidate.cs / CompOffService. Unlike
        // WFH_REQUEST/ON_DUTY_REQUEST/SHORT_LEAVE_REQUEST above, this is
        // HR-only for acting (Approve/Reject) - the Employee grant below is
        // View-only, for their own history.
        public const string COMP_OFF = "COMP_OFF";
        public const string COMP_OFF_CONTROLLER = "CompOff";
        public const string COMP_OFF_ACTION = ACTION_INDEX;

        // =====================================================
        // LEAVE MANAGEMENT
        // =====================================================

        public const string LEAVE_MANAGEMENT = "LEAVE_MANAGEMENT";

        public const string LEAVE_TYPE = "LEAVE_TYPE";
        public const string LEAVE_TYPE_CONTROLLER = "LeaveType";
        public const string LEAVE_TYPE_ACTION = ACTION_INDEX;

        public const string LEAVE_APPLICATION = "LEAVE_APPLICATION";
        public const string LEAVE_APPLICATION_CONTROLLER = "LeaveApplication";
        public const string LEAVE_APPLICATION_ACTION = ACTION_INDEX;

        public const string LEAVE_BALANCE = "LEAVE_BALANCE";
        public const string LEAVE_BALANCE_CONTROLLER = "LeaveBalance";
        public const string LEAVE_BALANCE_ACTION = ACTION_INDEX;

        public const string LEAVE_APPROVAL = "LEAVE_APPROVAL";
        public const string LEAVE_APPROVAL_CONTROLLER = "LeaveApproval";
        public const string LEAVE_APPROVAL_ACTION = ACTION_INDEX;

        public const string LEAVE_CALENDAR = "LEAVE_CALENDAR";
        public const string LEAVE_CALENDAR_CONTROLLER = "LeaveApplication";
        public const string LEAVE_CALENDAR_ACTION = "Calendar";

        // Out-of-office proxy approver (Level 1/2 delegation) - see
        // Domain/Entities/ApprovalDelegation.cs.
        public const string APPROVAL_DELEGATION = "APPROVAL_DELEGATION";
        public const string APPROVAL_DELEGATION_CONTROLLER = "ApprovalDelegation";
        public const string APPROVAL_DELEGATION_ACTION = ACTION_INDEX;

        // =====================================================
        // PAYROLL
        // =====================================================

        public const string PAYROLL = "PAYROLL";
        public const string PAYROLL_CONTROLLER = "Payroll";
        public const string PAYROLL_ACTION = ACTION_INDEX;

        public const string SALARY_COMPONENT = "SALARY_COMPONENT";
        public const string SALARY_COMPONENT_CONTROLLER = "SalaryComponent";
        public const string SALARY_COMPONENT_ACTION = ACTION_INDEX;

        public const string SALARY_STRUCTURE = "SALARY_STRUCTURE";
        public const string SALARY_STRUCTURE_CONTROLLER = "SalaryStructure";
        public const string SALARY_STRUCTURE_ACTION = ACTION_INDEX;

        // NOTE: rectified — payroll processing/list lives on the "Payroll" controller.
        public const string PAYROLL_PROCESS = "PAYROLL_PROCESS";
        public const string PAYROLL_PROCESS_CONTROLLER = "Payroll";
        public const string PAYROLL_PROCESS_ACTION = ACTION_INDEX;

        public const string PAYROLL_DASHBOARD = "PAYROLL_DASHBOARD";
        public const string PAYROLL_DASHBOARD_CONTROLLER = "Payroll";
        public const string PAYROLL_DASHBOARD_ACTION = "Dashboard";

        public const string PAYROLL_REGISTER = "PAYROLL_REGISTER";
        public const string PAYROLL_REGISTER_CONTROLLER = "Payroll";
        public const string PAYROLL_REGISTER_ACTION = "Report";

        // Payslip is opened per-payroll (Payroll/Payslip), not a top-level menu.
        public const string PAYSLIP = "PAYSLIP";
        public const string PAYSLIP_CONTROLLER = "Payroll";
        public const string PAYSLIP_ACTION = "Payslip";

        // Payslip Request approval workflow (Employee -> Reporting Manager
        // -> Finance) - see Domain/Entities/PayslipRequest.cs. The Approve
        // action on THIS feature is the hand-rolled permission check that
        // makes someone "Finance" for PayslipRequestService's purposes
        // (there is no fixed "Finance" role concept in this codebase - a
        // tenant creates one via the existing Role/Permission admin screens
        // and grants Approve on this feature, same convention as every
        // other permission-gated override here).
        public const string PAYROLL_PAYSLIP_REQUEST = "PAYROLL_PAYSLIP_REQUEST";
        public const string PAYROLL_PAYSLIP_REQUEST_CONTROLLER = "PayslipRequest";
        public const string PAYROLL_PAYSLIP_REQUEST_ACTION = ACTION_INDEX;

        // =====================================================
        // RECRUITMENT
        // =====================================================

        public const string RECRUITMENT = "RECRUITMENT";

        public const string JOB_OPENING = "JOB_OPENING";
        public const string JOB_OPENING_CONTROLLER = "JobOpening";
        public const string JOB_OPENING_ACTION = ACTION_INDEX;

        public const string CANDIDATE = "CANDIDATE";
        public const string CANDIDATE_CONTROLLER = "Candidate";
        public const string CANDIDATE_ACTION = ACTION_INDEX;

        public const string CANDIDATE_APPLICATION = "CANDIDATE_APPLICATION";
        public const string CANDIDATE_APPLICATION_CONTROLLER = "CandidateApplication";
        public const string CANDIDATE_APPLICATION_ACTION = ACTION_INDEX;

        // NOTE: rectified — the MVC controller is "InterviewSchedule".
        public const string INTERVIEW = "INTERVIEW";
        public const string INTERVIEW_CONTROLLER = "InterviewSchedule";
        public const string INTERVIEW_ACTION = ACTION_INDEX;

        public const string RECRUITMENT_DASHBOARD = "RECRUITMENT_DASHBOARD";
        public const string RECRUITMENT_DASHBOARD_CONTROLLER = "RecruitmentDashboard";
        public const string RECRUITMENT_DASHBOARD_ACTION = ACTION_INDEX;

        // =====================================================
        // ASSET MANAGEMENT
        // =====================================================

        public const string ASSET_MANAGEMENT = "ASSET_MANAGEMENT";

        public const string ASSET_CATEGORY = "ASSET_CATEGORY";
        public const string ASSET_CATEGORY_CONTROLLER = "AssetCategory";
        public const string ASSET_CATEGORY_ACTION = ACTION_INDEX;

        public const string ASSET = "ASSET";
        public const string ASSET_CONTROLLER = "Asset";
        public const string ASSET_ACTION = ACTION_INDEX;

        public const string ASSET_ALLOCATION = "ASSET_ALLOCATION";
        public const string ASSET_ALLOCATION_CONTROLLER = "AssetAllocation";
        public const string ASSET_ALLOCATION_ACTION = ACTION_INDEX;

        public const string ASSET_HISTORY = "ASSET_HISTORY";
        public const string ASSET_HISTORY_CONTROLLER = "AssetHistory";
        public const string ASSET_HISTORY_ACTION = ACTION_INDEX;

        // =====================================================
        // EMPLOYEE ONBOARDING
        // =====================================================

        public const string ONBOARDING_MANAGEMENT = "ONBOARDING_MANAGEMENT";

        public const string ONBOARDING = "ONBOARDING";
        public const string ONBOARDING_CONTROLLER = "Onboarding";
        public const string ONBOARDING_ACTION = ACTION_INDEX;

        public const string ONBOARDING_TEMPLATE = "ONBOARDING_TEMPLATE";
        public const string ONBOARDING_TEMPLATE_CONTROLLER = "OnboardingTemplate";
        public const string ONBOARDING_TEMPLATE_ACTION = ACTION_INDEX;

        // =====================================================
        // PROBATION & CONFIRMATION (Maker-Checker)
        // =====================================================
        // New top-level menu group (a sibling of ONBOARDING_MANAGEMENT/
        // ASSET_MANAGEMENT/RECRUITMENT above, NOT nested under
        // ATTENDANCE_MANAGEMENT or EMPLOYEE_MANAGEMENT) - an Employee
        // Management concern, but broken out on its own since later
        // features (PIP, Employee Transfer) will be added here as sibling
        // children too. This is the first module in this codebase to use
        // the Maker-Checker (segregation of duties) authorization pattern -
        // see ProbationConfirmationService for the actingUserId != MakerId
        // invariant. LATER AGENTS building PIP/Transfer: reuse this exact
        // PROBATION_CONFIRMATION_MANAGEMENT parent constant/menu group
        // (seeded in DbSeeder.ReconcileModulesAsync) rather than creating a
        // new one.
        public const string PROBATION_CONFIRMATION_MANAGEMENT = "PROBATION_CONFIRMATION_MANAGEMENT";

        // Probation Confirmation - the first child feature under the new
        // parent above. MAKER: anyone holding Create permission on this
        // feature proposes Confirm/Extend/PlaceOnPIP/Terminate for an
        // employee whose probation is due; CHECKER: a DIFFERENT person
        // holding Approve permission must Approve/Reject it - see
        // Domain/Entities/ProbationConfirmation.cs / ProbationConfirmationService.
        public const string PROBATION_CONFIRMATION = "PROBATION_CONFIRMATION";
        public const string PROBATION_CONFIRMATION_CONTROLLER = "ProbationConfirmation";
        public const string PROBATION_CONFIRMATION_ACTION = ACTION_INDEX;

        // PIP (Performance Improvement Plan) - Phase 2, a SIBLING child
        // feature under the SAME PROBATION_CONFIRMATION_MANAGEMENT parent
        // above (do not create a new parent). Creation (hand-off from an
        // Approved ProbationConfirmation with Recommendation ==
        // PlaceOnPIP) has NO maker-checker gate; the gate applies to the
        // final outcome resolution instead - see
        // Domain/Entities/PipRecord.cs / PipService.
        public const string PIP = "PIP";
        public const string PIP_CONTROLLER = "Pip";
        public const string PIP_ACTION = ACTION_INDEX;

        // Employee Transfer - Phase 3, a SIBLING child feature under the
        // SAME PROBATION_CONFIRMATION_MANAGEMENT parent above (not a new
        // parent). MAKER: anyone holding Create permission on this feature
        // proposes new Company/Branch/Department/Designation/
        // ReportingManager values for an Employee; CHECKER: a DIFFERENT
        // person holding Approve permission must Approve/Reject it before
        // the change is applied to the live Employee record - see
        // Domain/Entities/EmployeeTransfer.cs / EmployeeTransferService.
        public const string EMPLOYEE_TRANSFER = "EMPLOYEE_TRANSFER";
        public const string EMPLOYEE_TRANSFER_CONTROLLER = "EmployeeTransfer";
        public const string EMPLOYEE_TRANSFER_ACTION = ACTION_INDEX;

        // Employee Feedback - Phase 4, a SIBLING child feature under the
        // SAME PROBATION_CONFIRMATION_MANAGEMENT parent above (not a new
        // parent). UNLIKE the three phases above, this feature has NO
        // maker-checker workflow - plain CRUD. Create authorization: the
        // acting user's own linked Employee is the target Employee's
        // current ReportingManagerId, OR they hold Create permission on
        // this feature (HR/Admin) - see
        // Domain/Entities/EmployeeFeedback.cs / EmployeeFeedbackService.
        public const string EMPLOYEE_FEEDBACK = "EMPLOYEE_FEEDBACK";
        public const string EMPLOYEE_FEEDBACK_CONTROLLER = "EmployeeFeedback";
        public const string EMPLOYEE_FEEDBACK_ACTION = ACTION_INDEX;

        // Rejoining - Phase 5 (final) of the "Probation & Confirmation"
        // module, a SIBLING child feature under the SAME
        // PROBATION_CONFIRMATION_MANAGEMENT parent above (not a new
        // parent). Like Phase 4 (Employee Feedback), this feature has NO
        // maker-checker workflow - a single-step, HR-permission-gated
        // rehire action for a FORMER employee (Employee.RelievingDate !=
        // null, IsDeleted == false). HR-only (no Employee self-service
        // grant), View/Create permissions only - rejoining history is an
        // append-only audit log, not editable/deletable - see
        // Domain/Entities/RejoiningHistory.cs / RejoiningService.
        public const string REJOINING = "REJOINING";
        public const string REJOINING_CONTROLLER = "Rejoining";
        public const string REJOINING_ACTION = ACTION_INDEX;

        // =====================================================
        // TAXATION (Income Tax / TDS - India)
        // =====================================================
        // New top-level module (sibling of Probation & Confirmation/
        // Onboarding/Payroll above) - see Domain/Entities/TaxSlab.cs,
        // TaxDeclaration.cs, EmployeeTaxComputation.cs and
        // Application/Services/Taxation/*.
        public const string TAXATION_MANAGEMENT = "TAXATION_MANAGEMENT";

        // Tax Slabs - Admin-only master data (income slab rates per
        // Financial Year + Regime), plain CRUD, no maker-checker/self-
        // service concept.
        public const string TAX_SLAB = "TAX_SLAB";
        public const string TAX_SLAB_CONTROLLER = "TaxSlab";
        public const string TAX_SLAB_ACTION = ACTION_INDEX;

        // Tax Declaration - employee self-service annual investment
        // declaration (Draft -> Submitted by the employee), verified or
        // rejected by HR/Payroll (holding Approve permission on this
        // feature) - see TaxDeclarationService. Broader audience than
        // Probation & Confirmation: every Employee needs Create/View on
        // their OWN declaration in addition to the HR/Admin grant below.
        public const string TAX_DECLARATION = "TAX_DECLARATION";
        public const string TAX_DECLARATION_CONTROLLER = "TaxDeclaration";
        public const string TAX_DECLARATION_ACTION = ACTION_INDEX;

        // Tax Computation - HR/Payroll-only read+trigger surface over
        // EmployeeTaxComputation (annual tax liability / TDS projection)
        // - see TaxComputationService. No employee self-service grant;
        // employees view their own numbers through Tax Declaration
        // Details instead, not this feature directly.
        public const string TAX_COMPUTATION = "TAX_COMPUTATION";
        public const string TAX_COMPUTATION_CONTROLLER = "TaxComputation";
        public const string TAX_COMPUTATION_ACTION = ACTION_INDEX;

        // =====================================================
        // COMMUNICATION
        // =====================================================

        public const string COMMUNICATION = "COMMUNICATION";

        public const string ANNOUNCEMENT = "ANNOUNCEMENT";
        public const string ANNOUNCEMENT_CONTROLLER = "Announcement";
        public const string ANNOUNCEMENT_ACTION = ACTION_INDEX;

        public const string EVENT = "EVENT";
        public const string EVENT_CONTROLLER = "Event";
        public const string EVENT_ACTION = ACTION_INDEX;

        public const string EVENT_PARTICIPANT = "EVENT_PARTICIPANT";
        public const string EVENT_PARTICIPANT_CONTROLLER = "EventParticipant";
        public const string EVENT_PARTICIPANT_ACTION = ACTION_INDEX;

        // =====================================================
        // NOTIFICATION
        // =====================================================

        public const string NOTIFICATION = "NOTIFICATION";
        public const string NOTIFICATION_CONTROLLER = "Notification";
        public const string NOTIFICATION_ACTION = ACTION_INDEX;

        public const string NOTIFICATION_GROUP = "NOTIFICATION_GROUP";
        public const string NOTIFICATION_GROUP_CONTROLLER = "NotificationGroup";
        public const string NOTIFICATION_GROUP_ACTION = ACTION_INDEX;

        public const string NOTIFICATION_LOG = "NOTIFICATION_LOG";
        public const string NOTIFICATION_LOG_CONTROLLER = "NotificationLog";
        public const string NOTIFICATION_LOG_ACTION = ACTION_INDEX;

        // =====================================================
        // HELP & SUPPORT
        // =====================================================

        public const string HELP_SUPPORT = "HELP_SUPPORT";

        public const string SUPPORT_TICKET = "SUPPORT_TICKET";
        public const string SUPPORT_TICKET_CONTROLLER = "SupportTicket";
        public const string SUPPORT_TICKET_ACTION = ACTION_INDEX;

        public const string FAQ = "FAQ";
        public const string FAQ_CONTROLLER = "Faq";
        public const string FAQ_ACTION = ACTION_INDEX;


        // =====================================================
        // TAXATION
        // =====================================================

        public const string TAXATION = "TAXATION";

        public const string INCOME_TAX = "INCOME_TAX";
        public const string INCOME_TAX_CONTROLLER = "IncomeTax";
        public const string INCOME_TAX_ACTION = ACTION_INDEX;

        public const string INVESTMENT_DECLARATION = "INVESTMENT_DECLARATION";
        public const string INVESTMENT_DECLARATION_CONTROLLER = "InvestmentDeclaration";
        public const string INVESTMENT_DECLARATION_ACTION = ACTION_INDEX;

        public const string HOUSE_PROPERTY = "HOUSE_PROPERTY";
        public const string HOUSE_PROPERTY_CONTROLLER = "HouseProperty";
        public const string HOUSE_PROPERTY_ACTION = ACTION_INDEX;

        public const string OTHER_INCOME = "OTHER_INCOME";
        public const string OTHER_INCOME_CONTROLLER = "OtherIncome";
        public const string OTHER_INCOME_ACTION = ACTION_INDEX;

        public const string TDS_PROJECTION = "TDS_PROJECTION";
        public const string TDS_PROJECTION_CONTROLLER = "TDSProjection";
        public const string TDS_PROJECTION_ACTION = ACTION_INDEX;

        public const string TAX_REGIME = "TAX_REGIME";
        public const string TAX_REGIME_CONTROLLER = "TaxRegime";
        public const string TAX_REGIME_ACTION = ACTION_INDEX;

        public const string FORM_16 = "FORM_16";
        public const string FORM_16_CONTROLLER = "Form16";
        public const string FORM_16_ACTION = ACTION_INDEX;

        // =====================================================
        // LOAN & ADVANCE MANAGEMENT
        // =====================================================
        // New top-level module (sibling of TAXATION_MANAGEMENT/
        // PROBATION_CONFIRMATION_MANAGEMENT above). Masters (LOAN_TYPE/
        // ADVANCE_TYPE/LOAN_POLICY) are HR/Admin-only CRUD. EMPLOYEE_LOAN/
        // EMPLOYEE_ADVANCE are dual-audience like TAX_DECLARATION: every
        // Employee needs Create/View on their OWN request (self-service
        // submission), while HR/Finance additionally hold View/Approve
        // tenant-wide for the Maker-Checker workflow - see
        // EmployeeLoanService/EmployeeAdvanceService's actingUserId !=
        // MakerId invariant (same shape as ProbationConfirmationService,
        // but resolved against a configurable N-level matrix instead of a
        // single fixed checker - see Domain/Entities/LoanPolicyApprovalLevel.cs).
        public const string LOAN_ADVANCE_MANAGEMENT = "LOAN_ADVANCE_MANAGEMENT";

        public const string LOAN_TYPE = "LOAN_TYPE";
        public const string LOAN_TYPE_CONTROLLER = "LoanType";
        public const string LOAN_TYPE_ACTION = ACTION_INDEX;

        public const string ADVANCE_TYPE = "ADVANCE_TYPE";
        public const string ADVANCE_TYPE_CONTROLLER = "AdvanceType";
        public const string ADVANCE_TYPE_ACTION = ACTION_INDEX;

        public const string LOAN_POLICY = "LOAN_POLICY";
        public const string LOAN_POLICY_CONTROLLER = "LoanPolicy";
        public const string LOAN_POLICY_ACTION = ACTION_INDEX;

        // MAKER: the Employee themselves (self-service Create on their own
        // request) OR HR holding Create permission on their behalf.
        // CHECKER: whoever the request's resolved LoanPolicyApprovalLevel
        // for the CURRENT level identifies (Reporting Manager / a specific
        // Role's holder / a specific User) - see
        // EmployeeLoanService.ResolveCurrentLevelApprovers.
        public const string EMPLOYEE_LOAN = "EMPLOYEE_LOAN";
        public const string EMPLOYEE_LOAN_CONTROLLER = "EmployeeLoan";
        public const string EMPLOYEE_LOAN_ACTION = ACTION_INDEX;

        // Disbursement/Pre-Closure/Settlement are Finance-only actions on
        // an already-Approved EmployeeLoan - gated by Approve permission
        // on this SAME feature (no separate feature constant), consistent
        // with PAYROLL_PAYSLIP_REQUEST's "Approve on this feature is what
        // makes someone Finance" convention above.
        public const string EMPLOYEE_ADVANCE = "EMPLOYEE_ADVANCE";
        public const string EMPLOYEE_ADVANCE_CONTROLLER = "EmployeeAdvance";
        public const string EMPLOYEE_ADVANCE_ACTION = ACTION_INDEX;

        public const string LOAN_ADVANCE_DASHBOARD = "LOAN_ADVANCE_DASHBOARD";
        public const string LOAN_ADVANCE_DASHBOARD_CONTROLLER = "LoanAdvanceDashboard";
        public const string LOAN_ADVANCE_DASHBOARD_ACTION = ACTION_INDEX;

        public const string LOAN_ADVANCE_REPORT = "LOAN_ADVANCE_REPORT";
        public const string LOAN_ADVANCE_REPORT_CONTROLLER = "LoanAdvanceReport";
        public const string LOAN_ADVANCE_REPORT_ACTION = ACTION_INDEX;

        // Read-only, Auditor/Admin-gated - see Domain/Entities/LoanAdvanceAuditLog.cs.
        public const string LOAN_ADVANCE_AUDIT_LOG = "LOAN_ADVANCE_AUDIT_LOG";
        public const string LOAN_ADVANCE_AUDIT_LOG_CONTROLLER = "LoanAdvanceAuditLog";
        public const string LOAN_ADVANCE_AUDIT_LOG_ACTION = ACTION_INDEX;

    }
}
