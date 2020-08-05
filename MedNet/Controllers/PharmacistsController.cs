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
                var sugg_phn = _bigChainDbService.GetSuggPatientPHN(patientLookupViewModel.PHN);
                string sugg_msg = "";
                if (sugg_phn != "")
                {
                    sugg_msg = "Did you mean: " + sugg_phn + "?";
                }
                ModelState.AddModelError("", "We could not find a matching user. " + sugg_msg);
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

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult PatientRecords()
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

                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string, PrescriptionMetadata>
                    (AssetType.Prescription, doctorSignPublicKey, patientSignPublicKey);
                var prescriptions = new List<PrescriptionFullData>();
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    var newEntry = new PrescriptionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<Prescription>(data),
                        Metadata = prescription.metadata.data,
                        transID = prescription.transID,
                        assetID = prescription.id
                    };
                    prescriptions.Add(newEntry);
                }
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.assetData.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult RequestAccess()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
                return View();
        }

        [HttpPost]
        public IActionResult RequestAccess(RequestAccessViewModel requestAccessViewModel)
        {
            // Description: Authenticates a patient's identity when a Doctor requests access to their medical information
            // Get's the Doctor's information for current session
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(requestAccessViewModel);
            string PHN = HttpContext.Session.GetString(Globals.currentPPHN);
            string patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            string doctorSignPrivatekey = HttpContext.Session.GetString(Globals.currentDSPriK);
            string doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivatekey);
            string doctorAgreePrivatekey = HttpContext.Session.GetString(Globals.currentDAPriK);
            string doctorAgreePublicKey = EncryptionService.getAgreePublicKeyStringFromPrivate(doctorAgreePrivatekey);
            string keyword = requestAccessViewModel.keyword;

            // Searches for a patient with the specified PHN
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "Could not find a patient profile with PHN: " + PHN);
                return View(requestAccessViewModel);
            }

            // Send request to the Client Computer to authenticate with fingerprint
            int numScans = 1;
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", numScans); // DEBUG: Jacob's Computer 

            // Check if fingerprint data is valid
            if (fpList.Count < numScans)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(requestAccessViewModel);
            }
            Image fpImg = fpList[0];

            // Decrypt the patient's fingerprint data stored in the Blockchain
            byte[] dbFpData = null;
            string patientSignPrivateKey, patientAgreePrivateKey;
            List<string> dbList = userAsset.data.Data.FingerprintData;
            List<Image> dbfpList = new List<Image>();
            try
            {
                foreach (string db in dbList)
                {
                    EncryptionService.decryptFingerprintData(PHN, keyword, db, out dbFpData);
                    dbfpList.Add(FingerprintService.byteToImg(dbFpData));
                }
                EncryptionService.getPrivateKeyFromIDKeyword(PHN, keyword, userAsset.data.Data.PrivateKeys, out patientSignPrivateKey, out patientAgreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(requestAccessViewModel);
            }

            // Compare the scanned fingerprint with the one saved in the database 
            if (!FingerprintService.compareFP(fpImg, fpList))
            {
                ModelState.AddModelError("", "The fingerprint did not match, try again.");
                return View(requestAccessViewModel);
            }

            // Choose the types of records we want to get
            AssetType[] typeList = { AssetType.Prescription };
            var recordList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                (typeList, patientSignPublicKey);
            foreach (var record in recordList)
            {
                MetaDataSaved<object> metadata = record.metadata;
                if (!metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    var hashedKey = metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var newHash = EncryptionService.getEncryptedEncryptionKey(dataDecryptionKey, patientAgreePrivateKey, doctorAgreePublicKey);
                    metadata.AccessList[doctorSignPublicKey] = newHash;
                    _bigChainDbService.SendTransferTransactionToDataBase(record.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, record.transID);
                }
            }
            return RedirectToAction("PatientOverview");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public IActionResult FillPrescription(FillPrescriptionViewModel fillPrescriptionViewModel)
        {
            // Description: Authenticates a patient's identity when a Doctor requests access to their medical information
            // Get's the Doctor's information for current session
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            var oldViewModel = JsonConvert.DeserializeObject<FillPrescriptionViewModel>(TempData["viewModel"] as string);
            fillPrescriptionViewModel.PrescriptionData = oldViewModel.PrescriptionData;
            fillPrescriptionViewModel.PatientAsset = oldViewModel.PatientAsset;
            fillPrescriptionViewModel.PatientMetadata = oldViewModel.PatientMetadata;
            TempData["viewModel"] = JsonConvert.SerializeObject(fillPrescriptionViewModel);

            if (!ModelState.IsValid)
                return View(fillPrescriptionViewModel);
            string PHN = HttpContext.Session.GetString(Globals.currentPPHN);
            string patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            string keyword = fillPrescriptionViewModel.PatientKeyword;

            // Searches for a patient with the specified PHN
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Patient, PHN);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "Could not find a patient profile with PHN: " + PHN);
                return View(fillPrescriptionViewModel);
            }

            // Send request to the Client Computer to authenticate with fingerprint
            int numScans = 1;
            List<Image> fpList = FingerprintService.authenticateFP("24.84.225.22", numScans); // DEBUG: Jacob's Computer 

            // Check if fingerprint data is valid
            if (fpList.Count < numScans)
            {
                ModelState.AddModelError("", "Something went wrong with the fingerprint scan, try again.");
                return View(fillPrescriptionViewModel);
            }
            Image fpImg = fpList[0];

            // Decrypt the patient's fingerprint data stored in the Blockchain
            byte[] dbFpData = null;
            string patientSignPrivateKey, patientAgreePrivateKey;
            List<string> dbList = userAsset.data.Data.FingerprintData;
            List<Image> dbfpList = new List<Image>();
            try
            {
                foreach (string db in dbList)
                {
                    EncryptionService.decryptFingerprintData(PHN, keyword, db, out dbFpData);
                    dbfpList.Add(FingerprintService.byteToImg(dbFpData));
                }
                EncryptionService.getPrivateKeyFromIDKeyword(PHN, keyword, userAsset.data.Data.PrivateKeys, out patientSignPrivateKey, out patientAgreePrivateKey);
            }
            catch
            {
                ModelState.AddModelError("", "Keyword may be incorrect");
                return View(fillPrescriptionViewModel);
            }

            // Compare the scanned fingerprint with the one saved in the database 
            if (!FingerprintService.compareFP(fpImg, fpList))
            {
                ModelState.AddModelError("", "The fingerprint did not match, try again.");
                return View(fillPrescriptionViewModel);
            }

            var prescriptionData = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, PrescriptionMetadata>
                (fillPrescriptionViewModel.PrescriptionData.transID);
            var oldMetadata = prescriptionData.metadata;

            if (fillPrescriptionViewModel.PrescriptionData.assetData.EndDate.CompareTo(DateTime.Now) < 0)
            {
                ModelState.AddModelError("", "The Prescription seems to have expired. Cannot fill this prescription.");
                return View(fillPrescriptionViewModel);
            }

            if(fillPrescriptionViewModel.PrescriptionData.Metadata.RefillRemaining < fillPrescriptionViewModel.QtyFilled)
            {
                ModelState.AddModelError("", "Connot issue more than remaining refills.");
            }

            MetaDataSaved<PrescriptionMetadata> newMetadata = oldMetadata;
            newMetadata.data.LastIssueQty = fillPrescriptionViewModel.QtyFilled;
            newMetadata.data.LastIssueDate = DateTime.Now;
            newMetadata.data.RefillRemaining = fillPrescriptionViewModel.PrescriptionData.Metadata.RefillRemaining - fillPrescriptionViewModel.QtyFilled;

            _bigChainDbService.SendTransferTransactionToDataBase<PrescriptionMetadata>(fillPrescriptionViewModel.PrescriptionData.assetID, 
                newMetadata,patientSignPrivateKey,patientSignPublicKey,fillPrescriptionViewModel.PrescriptionData.transID);
            return RedirectToAction("PatientRecords");
        }

        public IActionResult FillPrescription(string transID)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var patientInfo = userAsset.data.Data;

                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);

                var prescriptionData = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, PrescriptionMetadata>(transID);
                var hashedKey = prescriptionData.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                var data = EncryptionService.getDecryptedAssetData(prescriptionData.data.Data, dataDecryptionKey);
                var viewModel = new FillPrescriptionViewModel()
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PrescriptionData = new PrescriptionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<Prescription>(data),
                        Metadata = prescriptionData.metadata.data,
                        transID = prescriptionData.transID,
                        assetID = prescriptionData.id
                    }
                };
                TempData["viewModel"] = JsonConvert.SerializeObject(viewModel);
                return View(viewModel);
            }
        }

        public JsonResult GetAllPatientIDs()
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return Json("{}");
            else
            {
                var phns = _bigChainDbService.GetAllTypeIDs(AssetType.Patient);
                return Json(phns);
            }
        }
    }
}
