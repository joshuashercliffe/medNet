using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using MedNet.Data.Models.Models;
using System;

namespace MedNet.Models
{
    public class PatientEditProfileViewModel
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public Prov Province { get; set; }
        public string Postal { get; set; }

        public string Allergies { get; set; }
        public List<string> AllergyList { get; set; }
        public string MedHist { get; set; }
        public List<string> MedHistList { get; set; }
        public string Meds { get; set; }
        public List<string> MedList { get; set; }

        // Emergency contact info
        public string emerFirstName { get; set; }
        public string emerLastName { get; set; }
        public string emerPhone { get; set; }
        public string emerAddress { get; set; }
        public string emerCity { get; set; }
        public Prov emerProvince { get; set; }
        public string emerPostal { get; set; }
        public enum Prov
        {
            Alberta,
            BritishColumbia,
            Manitoba,
            NewBrunswick,
            NewfoundlandAndLabrador,
            NovaScotia,
            Ontario,
            PrinceEdwardIsland,
            Saskatchewan,
            Quebec,
            NorthWestTerritories,
            Nunavut,
            Yukon
        }
        public string Relationship { get; set; }
        public string[] Relationships = new[] { "Partner", "Parent", "Sibling", "Relative", "Friend", "Other" }; 

    }
}
