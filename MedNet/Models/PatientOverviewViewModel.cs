﻿using MedNet.Data.Models.Models;
using System;
using System.Collections.Generic;

namespace MedNet.Models
{
    public class PatientOverviewViewModel
    {
        public PatientCredAssetData PatientAsset{ get; set;}
        public PatientCredMetadata PatientMetadata { get; set; }
        public int PatientAge { get; set; }

        public List<DoctorNote> DoctorNotes { get; set; }

        public List<Prescription> Prescriptions { get; set; }
    }
}
