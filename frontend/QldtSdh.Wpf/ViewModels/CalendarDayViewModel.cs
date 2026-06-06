using System;
using System.Collections.Generic;
using QldtSdh.Shared;

namespace QldtSdh.Wpf.ViewModels
{
    public class CalendarDayViewModel
    {
        public int DayNumber { get; set; }
        public DateTime Date { get; set; }
        public bool IsCurrentMonth { get; set; }
        public bool IsToday { get; set; }
        public List<CaseDto> ActiveCases { get; set; } = new();
    }
}
