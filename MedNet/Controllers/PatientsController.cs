using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using MedNet.Models;
using MedNet.Data.Models.Models;
using System;
using MedNet.Data.Services;
using MedNet.Data.Models;
using System.Collections.Generic;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Drawing;
using Microsoft.AspNetCore.Authentication;

namespace MedNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PatientsController : Controller
    {
        private readonly ILogger<PatientsController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;
        public IActionResult Index()
        {
            return View();
        }

        public PatientsController(ILogger<PatientsController> logger)
        {
            _logger = logger;
            string[] nodes = Globals.nodes;
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(PatientLoginViewModel indexViewModel)
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(indexViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, indexViewModel.PatientPHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(indexViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(indexViewModel.PatientPHN, indexViewModel.PatientKeyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(indexViewModel);
            }
            UserCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<UserCredMetadata>(userAsset.id, EncryptionService.getSignPublicKeyStringFromPrivate(signPrivateKey));
            var password = indexViewModel.password;
            if (EncryptionService.verifyPassword(password, userMetadata.hashedPassword))
            {
                HttpContext.Session.SetString(Globals.currentPSPriK, signPrivateKey);
                HttpContext.Session.SetString(Globals.currentPAPriK, agreePrivateKey);
                HttpContext.Session.SetString(Globals.currentUserName, $"{userAsset.data.Data.FirstName} {userAsset.data.Data.LastName}");
                HttpContext.Session.SetString(Globals.currentUserID, userAsset.data.Data.ID);
                return RedirectToAction("PatientOverview");
            }
            else
            {
                ModelState.AddModelError("", "Password or Keyword incorrect.");
                return View(indexViewModel);
            }
        }

        public IActionResult PatientOverview()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("patientLookUp");
            else
            {
                Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
                var patientAgreePrivateKey = HttpContext.Session.GetString(Globals.currentPAPriK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                    (AssetType.DoctorNote, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                    (AssetType.Prescription, patientSignPublicKey);
                var doctorNotes = new List<DoctorNote>();
                var prescriptions = new List<Prescription>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.data[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                    doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(data));
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.data[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    prescriptions.Add(JsonConvert.DeserializeObject<Prescription>(data));
                }
                var patientInfo = userAsset.data.Data;
                var patientAge = DateTime.Now.Year - patientInfo.DateOfBirth.Year;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientName = $"{patientInfo.FirstName} {patientInfo.LastName}",
                    PatientPHN = patientInfo.ID,
                    PatientDOB = patientInfo.DateOfBirth,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }
    }
}
