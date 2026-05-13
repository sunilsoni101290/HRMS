using Application.DTOs.Employee;
using Domain.Entities;
using Domain.Enums;
using Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;
using static Domain.Enums.EnumExtensions;

namespace Application.Mappings
{
    public static class EmployeeMapper
    {
        public static Employee ToEntity(EmployeeDto dto)
        {
            return new Employee
            {
                Id = dto.Id ?? IDManager.GetNewId(new Employee()),

                FirstName = dto.FirstName,
                LastName = dto.LastName,

                TenantId = dto.TenantId,
                CompanyId = dto.CompanyId,
                BranchId = dto.BranchId,

                DepartmentId = dto.DepartmentId,
                DesignationId = dto.DesignationId,
                ReportingManagerId = dto.ReportingManagerId,

                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                MaritalStatus = dto.MaritalStatus,

                Email = dto.Email,
                Phone = dto.Phone,
                EmergencyContact = dto.EmergencyContact,

                Address = dto.Address,
                Pincode = dto.Pincode,

                PANNumber = dto.PANNumber,
                AadharNumber = dto.AadharNumber,

                JoiningDate = dto.JoiningDate,
                ConfirmationDate = dto.ConfirmationDate,
                RelievingDate = dto.RelievingDate,

                EmploymentType = dto.EmploymentType,
                CreatedBy="System"
            };
        }

        public static void UpdateEntity(Employee entity, EmployeeDto dto)
        {
            entity.FirstName = dto.FirstName;
            entity.LastName = dto.LastName;

            entity.CompanyId = dto.CompanyId;
            entity.BranchId = dto.BranchId;
            entity.DepartmentId = dto.DepartmentId;
            entity.DesignationId = dto.DesignationId;

            entity.ReportingManagerId = dto.ReportingManagerId;

            entity.DateOfBirth = dto.DateOfBirth;
            entity.Gender = dto.Gender;
            entity.MaritalStatus = dto.MaritalStatus;

            entity.Email = dto.Email;
            entity.Phone = dto.Phone;
            entity.EmergencyContact = dto.EmergencyContact;

            entity.Address = dto.Address;
            entity.Pincode = dto.Pincode;

            entity.PANNumber = dto.PANNumber;
            entity.AadharNumber = dto.AadharNumber;

            entity.JoiningDate = dto.JoiningDate;
            entity.ConfirmationDate = dto.ConfirmationDate;
            entity.RelievingDate = dto.RelievingDate;

            entity.EmploymentType = dto.EmploymentType;
            entity.ModifiedBy = "System";
            entity.ModifiedOn = DateTime.UtcNow;
        }

        // ==============================
        // 🔹 ENTITY → DTO (IMPORTANT)
        // ==============================
        public static EmployeeListDto ToDto(Employee entity)
        {
            if (entity == null) return null;

            return new EmployeeListDto
            {
                Id = entity.Id,
                EmployeeCode = entity.EmployeeCode,
                FirstName = entity.FirstName,
                LastName = entity.LastName,

                TenantId = entity.TenantId,

                CompanyId = entity.CompanyId,
                CompanyName = entity.Company != null
                    ? entity.Company.Name
                    : null,

                BranchId = entity.BranchId,
                BrnachName = entity.Branch != null
                    ? entity.Branch.Name
                    : null,

                DepartmentId = entity.DepartmentId,
                DepartmentName = entity.Department != null
                    ? entity.Department.Name
                    : null,

                DesignationId = entity.DesignationId,
                DesignationName = entity.Designation != null
                    ? entity.Designation.Name
                    : null,

                ReportingManagerId = entity.ReportingManagerId,
                ReportingManagerName = entity.ReportingManager != null
                    ? entity.ReportingManager.FirstName + " " + entity.ReportingManager.LastName
                    : null,

                DateOfBirth = entity.DateOfBirth,

                Gender = EnumHelper.GetEnumName<Gender>((int)entity.Gender),

                MaritalStatus = EnumHelper.GetEnumName<MaritalStatus>((int)entity.MaritalStatus),

                Email = entity.Email,
                Phone = entity.Phone,
                EmergencyContact = entity.EmergencyContact,

                Address = entity.Address,
                Pincode = entity.Pincode,

                PANNumber = entity.PANNumber,
                AadharNumber = entity.AadharNumber,

                JoiningDate = entity.JoiningDate,
                ConfirmationDate = entity.ConfirmationDate,
                RelievingDate = entity.RelievingDate,

                EmploymentType = EnumHelper.GetEnumName<EmploymentType>((int)entity.EmploymentType),
            };
        }
    }
}
