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
    }
}
