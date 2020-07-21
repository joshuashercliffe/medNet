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
using Microsoft.AspNetCore.Authentication;

namespace MedNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class PharmacistsController : Controller
    {
        private readonly ILogger<PharmacistsController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;

        public PharmacistsController(ILogger<PharmacistsController> logger)
        {
            _logger = logger;
            string[] nodes = Globals.nodes;
            _bigChainDbService = new BigChainDbService(nodes);
            _random = new Random();
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Login(PharmacistLoginViewModel indexViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(indexViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Pharmacist, indexViewModel.PharmaNum);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(indexViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(indexViewModel.PharmaNum, indexViewModel.PharmaKeyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
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
                HttpContext.Session.SetString(Globals.currentDSPriK, signPrivateKey);
                HttpContext.Session.SetString(Globals.currentDAPriK, agreePrivateKey);
                HttpContext.Session.SetString(Globals.currentUserName, $"{userAsset.data.Data.FirstName} {userAsset.data.Data.LastName}");
                HttpContext.Session.SetString(Globals.currentUserID, userAsset.data.Data.ID);
                return RedirectToAction("patientLookUp");
            }
            else
            {
                ModelState.AddModelError("", "Password or Keyword incorrect.");
                return View(indexViewModel);
            }
        }


        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        public IActionResult SignUp(pharmacistSignUpViewModel pharmacistSignUpViewModel)
        {
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey = null, agreePublicKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Doctor, pharmacistSignUpViewModel.PharmaNum);
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Doctor profile with that MINC already exists");
                return View(pharmacistSignUpViewModel);
            }
            var passphrase = pharmacistSignUpViewModel.PharmaKeyWord;
            var password = pharmacistSignUpViewModel.Password;
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out signPublicKey, out agreePrivateKey, out agreePublicKey);
            var userAssetData = new UserCredAssetData
            {
                FirstName = pharmacistSignUpViewModel.FirstName,
                LastName = pharmacistSignUpViewModel.LastName,
                ID = pharmacistSignUpViewModel.PharmaNum,
                Email = pharmacistSignUpViewModel.Email,
                PrivateKeys = EncryptionService.encryptPrivateKeys(pharmacistSignUpViewModel.PharmaNum, passphrase, signPrivateKey, agreePrivateKey),
                DateOfRecord = DateTime.Now,
                SignPublicKey = signPublicKey,
                AgreePublicKey = agreePublicKey,
                FingerprintData = new List<string>(),
            };
            var userMetadata = new UserCredMetadata
            {
                hashedPassword = EncryptionService.hashPassword(password)
            };
            var asset = new AssetSaved<UserCredAssetData>
            {
                Type = AssetType.Pharmacist,
                Data = userAssetData,
                RandomId = _random.Next(0, 100000)
            };
            var metadata = new MetaDataSaved<UserCredMetadata>
            {
                data = userMetadata
            };

            _bigChainDbService.SendCreateTransactionToDataBase(asset, metadata, signPrivateKey);
            return RedirectToAction("Login");
        }

        public IActionResult PatientLookUp()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null
                || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else
                return View();
        }

        [HttpPost]
        public IActionResult PatientLookUp(PatientLookupViewModel patientLookupViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(patientLookupViewModel);
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, patientLookupViewModel.PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(patientLookupViewModel);
            }
            HttpContext.Session.SetString(Globals.currentPSPubK, userAsset.data.Data.SignPublicKey);
            HttpContext.Session.SetString(Globals.currentPAPubK, userAsset.data.Data.AgreePublicKey);
            HttpContext.Session.SetString(Globals.currentPPHN, userAsset.data.Data.ID);
            return RedirectToAction("PatientOverview");
        }

        public IActionResult PatientOverview()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string>
                    (AssetType.DoctorNote, doctorSignPublicKey, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string>
                    (AssetType.Prescription, doctorSignPublicKey, patientSignPublicKey);
                var doctorNotes = new List<DoctorNote>();
                var prescriptions = new List<Prescription>();
                //foreach (var doctorNote in doctorNotesList)
                //{
                //    var hashedKey = doctorNote.metadata.data[doctorSignPublicKey];
                //    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                //    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                //    doctorNotes.Add(JsonConvert.DeserializeObject<DoctorNote>(data));
                //}
                //foreach (var prescription in prescriptionsList)
                //{
                //    var hashedKey = prescription.metadata.data[doctorSignPublicKey];
                //    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                //    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                //    prescriptions.Add(JsonConvert.DeserializeObject<Prescription>(data));
                //}
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }
    }
}
