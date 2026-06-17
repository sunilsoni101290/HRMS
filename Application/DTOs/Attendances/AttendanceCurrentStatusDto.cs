using System;
using System.Collections.Generic;
using System.Text;

namespace Application.DTOs.Attendances
{
    public class AttendanceCurrentStatusDto
    {
        // =====================================================
        // BUTTONS
        // =====================================================

        public bool CanPunchIn { get; set; }

        public bool CanPunchOut { get; set; }

        public bool CanBreakIn { get; set; }

        public bool CanBreakOut { get; set; }

        // =====================================================
        // STATUS
        // =====================================================

        public string CurrentStatus { get; set; }

        public string LastAction { get; set; }

        // =====================================================
        // SHIFT INFO
        // =====================================================

        public string ShiftName { get; set; }

        public string ShiftTime { get; set; }

        public string CurrentTime { get; set; }

        // =====================================================
        // ATTENDANCE INFO
        // =====================================================

        public DateTime? FirstIn { get; set; }

        public DateTime? LastOut { get; set; }

        public decimal WorkingHours { get; set; }

        public decimal BreakHours { get; set; }

        public decimal OvertimeHours { get; set; }

        // =====================================================
        // FLAGS
        // =====================================================

        public bool IsLate { get; set; }

        public bool IsEarlyExit { get; set; }
    }
}
