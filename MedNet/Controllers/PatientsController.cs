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
using MedNet.ViewModels;
using Microsoft.CodeAnalysis.Differencing;
using System.Threading.Tasks.Dataflow;
using Omnibasis.BigchainCSharp.Model;

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
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID( indexViewModel.PatientPHN);
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
            PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, userAsset.data.Data.SignPublicKey);
            var password = indexViewModel.password;
            if (EncryptionService.verifyPassword(password, userMetadata.hashedPassword))
            {
                HttpContext.Session.SetString(Globals.currentPSPriK, signPrivateKey);
                HttpContext.Session.SetString(Globals.currentPAPriK, agreePrivateKey);
                HttpContext.Session.SetString(Globals.currentPSPubK, userAsset.data.Data.SignPublicKey);
                HttpContext.Session.SetString(Globals.currentPAPubK, userAsset.data.Data.AgreePublicKey);
                HttpContext.Session.SetString(Globals.currentUserName, $"{userMetadata.FirstName} {userMetadata.LastName}");
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
                return RedirectToAction("Login");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

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
        public IActionResult EditProfile()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("Login");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                //Description: page where patient can edit their basic personal information
                return View(userMetadata);
            }
        }

        [HttpPost]
        public IActionResult EditProfile(PatientCredMetadata patientCredMetadata) 
        {
            
            Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));
            var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
            var transaction = _bigChainDbService.GetMetadataIDFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);
            var transID = transaction.Id ?? userAsset.id;
            patientCredMetadata.hashedPassword = transaction.Metadata.data.hashedPassword;
            var newMetadata = new MetaDataSaved<PatientCredMetadata> { 
                data = patientCredMetadata
            };
            _bigChainDbService.SendTransferTransactionToDataBase(userAsset.id, newMetadata,
    patientSignPrivateKey, patientSignPublicKey, transID);
            return RedirectToAction("PatientOverview");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View();
        }

        public IActionResult PatientRecords()
        {
            ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("Login");
            else
            {
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentUserID));

                var patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
                var patientAgreePrivateKey = HttpContext.Session.GetString(Globals.currentPAPriK);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);

                var doctorNotesList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string,double>
                    (AssetType.DoctorNote, patientSignPublicKey);
                var prescriptionsList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string,PrescriptionMetadata>
                    (AssetType.Prescription, patientSignPublicKey);
                var doctorNotes = new List<DoctorNoteFullData>();
                var prescriptions = new List<PrescriptionFullData>();
                foreach (var doctorNote in doctorNotesList)
                {
                    var hashedKey = doctorNote.metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(doctorNote.data.Data, dataDecryptionKey);
                    var newEntry = new DoctorNoteFullData
                    {
                        assetData = JsonConvert.DeserializeObject<DoctorNote>(data),
                        Metadata = doctorNote.metadata.data,
                        assetID = doctorNote.id,
                        transID = doctorNote.transID
                    };
                    doctorNotes.Add(newEntry);
                }
                foreach (var prescription in prescriptionsList)
                {
                    var hashedKey = prescription.metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(prescription.data.Data, dataDecryptionKey);
                    var newEntry = new PrescriptionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<Prescription>(data),
                        Metadata = prescription.metadata.data,
                        assetID = prescription.id,
                        transID = prescription.transID
                    };
                    prescriptions.Add(newEntry);
                }
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    DoctorNotes = doctorNotes.OrderByDescending(d => d.assetData.DateOfRecord).ToList(),
                    Prescriptions = prescriptions.OrderByDescending(p => p.assetData.PrescribingDate).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        public IActionResult EditAccess(string? transID)
        {
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("Login");
            else
            {
                ViewBag.UserName = HttpContext.Session.GetString(Globals.currentUserName);
                ViewBag.UserID = HttpContext.Session.GetString(Globals.currentUserID);
                var viewModel = new EditAccessViewModel();
                if (transID != null && transID != "")
                {
                    var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, object>(transID);
                    viewModel.reportType = result.data.Type;
                }
                return View(viewModel);
            }
        }

        public JsonResult GetAccessListTransID(string transID)
        {
            var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, object>(transID);
            var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            var accessList = result.metadata.AccessList.Keys.ToList();
            accessList.Remove(patientSignPublicKey);
            List<UserInfo> userInfoList = _bigChainDbService.GetUserInfoList(accessList.ToArray());
            return Json(userInfoList);
        }

        [HttpPost]
        public JsonResult GrantAccessToUser(EditAccessViewModel editAccessViewModel)
        {
            if (editAccessViewModel.UserType == null || editAccessViewModel.UserType == "")
                return Json(new { message = "Please select a user type."});
            // Searches for a patient with the specified PHN
            AssetType type = editAccessViewModel.UserType == "Doctor" ? AssetType.Doctor :
                editAccessViewModel.UserType == "Pharmacist" ? AssetType.Pharmacist : AssetType.MLT;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(type, editAccessViewModel.UserID);
            if (userAsset == null)
            {
                return Json(new { message = ("We could not find a " + editAccessViewModel.UserType + " with ID: " + editAccessViewModel.UserID) });
            }

            string patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            string patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
            string patientAgreePrivateKey = HttpContext.Session.GetString(Globals.currentPAPriK);
            string doctorSignPublicKey = userAsset.data.Data.SignPublicKey;
            string doctorAgreePublicKey = userAsset.data.Data.AgreePublicKey;
            string userName = userAsset.data.Data.FirstName + " " + userAsset.data.Data.LastName;

            if(editAccessViewModel.TransID != null && editAccessViewModel.TransID != "")
            {
                var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string,object>(editAccessViewModel.TransID);
                MetaDataSaved<object> metadata = result.metadata;
                if (!metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    var hashedKey = metadata.AccessList[patientSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, patientAgreePrivateKey);
                    var newHash = EncryptionService.getEncryptedEncryptionKey(dataDecryptionKey, patientAgreePrivateKey, doctorAgreePublicKey);
                    metadata.AccessList[doctorSignPublicKey] = newHash;
                    var newTransID = _bigChainDbService.SendTransferTransactionToDataBase(result.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, result.transID);
                    return Json(new { message = (userName + " (" + editAccessViewModel.UserID + ") was added to the record."), newtransid = newTransID });
                }
                else 
                {
                    return Json(new { message = (userName + " (" + editAccessViewModel.UserID + ") is already added to the record.") });
                }
            }

            // Choose the types of records we want to get
            List<AssetType> typeList = new List<AssetType>();
            if(type == AssetType.Doctor)
                typeList.AddRange( new List<AssetType> { AssetType.DoctorNote, AssetType.Prescription });
            else if(type == AssetType.Pharmacist)
                typeList.AddRange(new List<AssetType> { AssetType.DoctorNote, AssetType.Prescription });
            else
                typeList.AddRange(new List<AssetType> { });

            var recordList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                (typeList.ToArray(), patientSignPublicKey);
            int counter = 0;
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
                    counter++;
                }
            }

            return Json(new { message = (userName + " ("+ editAccessViewModel.UserID +") was added to " +counter.ToString()+ " records.")});
        }

        [HttpPost]
        public JsonResult RevokeAccessFromUser(EditAccessViewModel editAccessViewModel)
        {
            if (editAccessViewModel.UserType == null || editAccessViewModel.UserType == "")
                return Json(new { message = "Please select a user type." });
            // Searches for a patient with the specified PHN
            AssetType type = editAccessViewModel.UserType == "Doctor" ? AssetType.Doctor :
                editAccessViewModel.UserType == "Pharmacist" ? AssetType.Pharmacist : AssetType.MLT;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(type, editAccessViewModel.UserID);
            if (userAsset == null)
            {
                return Json(new { message = ("We could not find a " + editAccessViewModel.UserType + " with ID: " + editAccessViewModel.UserID) });
            }

            string patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
            string patientSignPrivateKey = HttpContext.Session.GetString(Globals.currentPSPriK);
            string doctorSignPublicKey = userAsset.data.Data.SignPublicKey;
            string userName = userAsset.data.Data.FirstName + " " + userAsset.data.Data.LastName;

            if (editAccessViewModel.TransID != null && editAccessViewModel.TransID != "")
            {
                var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, object>(editAccessViewModel.TransID);
                MetaDataSaved<object> metadata = result.metadata;
                if (metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    metadata.AccessList.Remove(doctorSignPublicKey);
                    var newTransID = _bigChainDbService.SendTransferTransactionToDataBase(result.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, result.transID);
                    return Json(new { message = (userName + " (" + editAccessViewModel.UserID + ") was removed from the record."), newtransid = newTransID });
                }
                else
                {
                    return Json(new { message = (userName + " (" + editAccessViewModel.UserID + ") was already removed from the record.")});
                }
            }

            // Choose the types of records we want to get
            List<AssetType> typeList = new List<AssetType>();
            if (type == AssetType.Doctor)
                typeList.AddRange(new List<AssetType> { AssetType.DoctorNote, AssetType.Prescription });
            else if (type == AssetType.Pharmacist)
                typeList.AddRange(new List<AssetType> { AssetType.DoctorNote, AssetType.Prescription });
            else
                typeList.AddRange(new List<AssetType> { });

            var recordList = _bigChainDbService.GetAllTypeRecordsFromPPublicKey<string>
                (typeList.ToArray(), patientSignPublicKey);
            int counter = 0;
            foreach (var record in recordList)
            {
                MetaDataSaved<object> metadata = record.metadata;
                if (metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    metadata.AccessList.Remove(doctorSignPublicKey);
                    _bigChainDbService.SendTransferTransactionToDataBase(record.id, metadata,
                        patientSignPrivateKey, patientSignPublicKey, record.transID);
                    counter++;
                }
            }

            return Json(new { message = (userName + " (" + editAccessViewModel.UserID + ") was removed from " + counter.ToString() + " records.") });
        }

        public JsonResult GetAllTypeIDs(string type) 
        {
            if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return Json("{}");
            else
            {
                if (type == null || type == "")
                    return Json("{}");
                AssetType assetType = type == "Doctor" ? AssetType.Doctor :
                                type == "Pharmacist" ? AssetType.Pharmacist : AssetType.MLT;
                var phns = _bigChainDbService.GetAllTypeIDs(assetType);
                return Json(phns);
            }
        }
    }
}
