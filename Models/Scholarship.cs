using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace scholarship_Application.Models
{
    public class Scholarship
    {
        public int ScholarshipId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Sponsor { get; set; }
        public decimal AmountPerSem { get; set; }
        public int Slots { get; set; }
        public decimal? MinGwa { get; set; }              // nullable kasi 'yung ibang scholarship ay walang minimum GWA requirement
        public decimal? MaxFamilyIncome { get; set; }     // same dito sa income
        public DateTime OpenDate { get; set; }
        public DateTime CloseDate { get; set; }
        public bool IsActive { get; set; }

        [Browsable(false)]
        public List<Requirement> Requirements { get; set; } = new List<Requirement>();
    }
}
