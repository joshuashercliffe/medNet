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
using System.IO;

namespace MedNet.Controllers
{
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public class MLTsController : Controller
    {
        private readonly ILogger<MLTsController> _logger;
        private BigChainDbService _bigChainDbService;
        private Random _random;

        public MLTsController(ILogger<MLTsController> logger)
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
        public IActionResult Login(MLTLoginViewModel indexViewModel)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (!ModelState.IsValid)
                return View(indexViewModel);
            string signPrivateKey = null, agreePrivateKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.MLT, indexViewModel.CSMLSID);
            if (userAsset == null)
            {
                ModelState.AddModelError("", "We could not find a matching user");
                return View(indexViewModel);
            }
            var hashedKeys = userAsset.data.Data.PrivateKeys;
            try
            {
                EncryptionService.getPrivateKeyFromIDKeyword(indexViewModel.CSMLSID, indexViewModel.MLTKeyword, hashedKeys, out signPrivateKey, out agreePrivateKey);
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
                return RedirectToAction("PatientLookUp");
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
        public IActionResult SignUp(mltSignUpViewModel mltSignUpViewModel)
        {
            string signPrivateKey = null, agreePrivateKey = null, signPublicKey = null, agreePublicKey = null;
            Assets<UserCredAssetData> userAsset = _bigChainDbService.GetUserAssetFromTypeID(AssetType.Doctor, mltSignUpViewModel.CSMLSID);
            if (userAsset != null)
            {
                ModelState.AddModelError("", "A Doctor profile with that MINC already exists");
                return View(mltSignUpViewModel);
            }
            var passphrase = mltSignUpViewModel.MLTKeyWord;
            var password = mltSignUpViewModel.Password;
            EncryptionService.getNewBlockchainUser(out signPrivateKey, out signPublicKey, out agreePrivateKey, out agreePublicKey);
            var userAssetData = new UserCredAssetData
            {
                FirstName = mltSignUpViewModel.FirstName,
                LastName = mltSignUpViewModel.LastName,
                ID = mltSignUpViewModel.CSMLSID,
                Email = mltSignUpViewModel.Email,
                PrivateKeys = EncryptionService.encryptPrivateKeys(mltSignUpViewModel.CSMLSID, passphrase, signPrivateKey, agreePrivateKey),
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
                Type = AssetType.MLT,
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

        public IActionResult RequestAccess()
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
                ViewBag.PatientName = userMetadata.FirstName + " " + userMetadata.LastName;
                ViewBag.PatientID = userAsset.data.Data.ID;
                return View();
            }
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
            AssetType[] typeList = { AssetType.TestRequisition };
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
                ModelState.AddModelError("", "We could not find a matching user.");
                if (sugg_phn != "")
                {
                    ModelState.AddModelError("", "Did you mean: " + sugg_phn + "?");
                }
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

                var testRequisitionList = _bigChainDbService.GetAllTypeRecordsFromDPublicPPublicKey<string, double>
                    (AssetType.TestRequisition, doctorSignPublicKey, patientSignPublicKey);
                Dictionary<string, string> testResults = null;
                if(testRequisitionList.Any())
                {
                    testResults = _bigChainDbService.GetAssociatedTestResults(testRequisitionList);
                }
                var testRequisitions = new List<TestRequisitionFullData>();
                foreach (var testrequisition in testRequisitionList)
                {
                    var hashedKey = testrequisition.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(testrequisition.data.Data, dataDecryptionKey);
                    var newEntry = new TestRequisitionFullData
                    {
                        assetData = JsonConvert.DeserializeObject<TestRequisitionAsset>(data),
                        Metadata = testrequisition.metadata.data,
                        assetID = testrequisition.id,
                        transID = testrequisition.transID
                    };
                    if(testResults != null && testResults.Keys.Contains(testrequisition.id))
                    {
                        var decryptedResultFile = EncryptionService.getDecryptedAssetData(testResults[testrequisition.id], dataDecryptionKey);
                        newEntry.ResultFile = JsonConvert.DeserializeObject<FileData>(decryptedResultFile);
                    }
                    testRequisitions.Add(newEntry);
                }
                var patientInfo = userAsset.data.Data;
                var patientOverviewViewModel = new PatientOverviewViewModel
                {
                    PatientAsset = patientInfo,
                    PatientMetadata = userMetadata,
                    PatientAge = patientInfo.DateOfBirth.CalculateAge(),
                    TestRequisitions = testRequisitions.OrderByDescending(p => p.assetData.DateOrdered).ToList()
                };

                return View(patientOverviewViewModel);
            }
        }

        [HttpPost]
        public IActionResult UploadResult(UploadResultViewModel uploadResultViewModel)
        {
            // Get's the Doctor's information for current session
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            var oldViewModel = JsonConvert.DeserializeObject<UploadResultViewModel>(TempData["viewModel"] as string);
            uploadResultViewModel.TestData = oldViewModel.TestData;
            uploadResultViewModel.PatientAsset = oldViewModel.PatientAsset;
            uploadResultViewModel.PatientMetadata = oldViewModel.PatientMetadata;
            TempData["viewModel"] = JsonConvert.SerializeObject(uploadResultViewModel);

            if (!ModelState.IsValid)
                return View(uploadResultViewModel);

            var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
            var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
            var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
            var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);

            var transID = uploadResultViewModel.TestData.transID;
            var testRequisition = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, double>(transID);
            //Check MLT has access to upload data
            if (testRequisition.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
            {
                //Get hash key for requisition. We will use the same key for result
                var hashedKey = testRequisition.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                if (uploadResultViewModel.ResultFile != null)
                {
                    var file = uploadResultViewModel.ResultFile;
                    string base64FileString = "";

                    // Convert "file" into base64 string "base64FileString" to save into database
                    using (var ms = new MemoryStream())
                    {
                        file.CopyTo(ms);
                        var fileBytes = ms.ToArray();
                        base64FileString = Convert.ToBase64String(fileBytes);
                    }
                    //encrypt file data and save to ipfs
                    var encryptFileData = EncryptionService.getEncryptedAssetDataKey(base64FileString, dataDecryptionKey);
                    var cid = _bigChainDbService.UploadTextToIPFS(encryptFileData);
                    var resultFile = new FileData()
                    {
                        Data = cid,
                        Type = file.ContentType,
                        Extension = file.ContentType.Split('/').Last(),
                        Name = file.FileName
                    };
                    //Encrypt the file using the same key
                    var encryptedFile = EncryptionService.getEncryptedAssetDataKey(JsonConvert.SerializeObject(resultFile), dataDecryptionKey);
                    
                    var asset = new AssetSaved<TestResultAsset>
                    {
                        Data = new TestResultAsset
                        {
                            RequisitionAssetID = testRequisition.id,
                            EncryptedResult = encryptedFile
                        },
                        RandomId = _random.Next(0, 100000),
                        Type = AssetType.TestResult
                    };
                    //Access is managed by requisition asset
                    var metadata = new MetaDataSaved<double>();
                    metadata.AccessList = new Dictionary<string, string>();

                    _bigChainDbService.SendCreateTransferTransactionToDataBase(asset, metadata, doctorSignPrivateKey, patientSignPublicKey);
                    return RedirectToAction("PatientRecords");
                }
                else 
                {
                    ModelState.AddModelError("", "Missing test result file.");
                    return View(uploadResultViewModel);
                }
            }
            else
            {
                ModelState.AddModelError("", "You do not have permission to upload test result.");
                return View(uploadResultViewModel);
            } 
        }

            public IActionResult UploadResult(string transID)
        {
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else if (HttpContext.Session.GetString(Globals.currentPSPubK) == null || HttpContext.Session.GetString(Globals.currentPAPubK) == null)
                return RedirectToAction("PatientLookUp");
            else
            {
                ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
                Assets<PatientCredAssetData> userAsset = _bigChainDbService.GetPatientAssetFromID(HttpContext.Session.GetString(Globals.currentPPHN));
                var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
                var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
                var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
                var patientSignPublicKey = HttpContext.Session.GetString(Globals.currentPSPubK);
                PatientCredMetadata userMetadata = _bigChainDbService.GetMetadataFromAssetPublicKey<PatientCredMetadata>(userAsset.id, patientSignPublicKey);
                var patientInfo = userAsset.data.Data;

                var testRequisition = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string,double>(transID);
                if (testRequisition.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
                {
                    var hashedKey = testRequisition.metadata.AccessList[doctorSignPublicKey];
                    var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                    var data = EncryptionService.getDecryptedAssetData(testRequisition.data.Data, dataDecryptionKey);
                    var asset = JsonConvert.DeserializeObject<TestRequisitionAsset>(data);
                    asset.AttachedFile.Data = null;
                    var viewModel = new UploadResultViewModel()
                    {
                        TestData = new TestRequisitionFullData
                        {
                            assetData = asset,
                            Metadata = testRequisition.metadata.data,
                            assetID = testRequisition.id,
                            transID = testRequisition.transID
                        },
                        PatientAsset = patientInfo,
                        PatientMetadata = userMetadata
                    };
                    TempData["viewModel"] = JsonConvert.SerializeObject(viewModel);
                    return View(viewModel);
                }
                else
                    return new EmptyResult();
            }
        }

        public IActionResult GetRequisitionFile(string transID)
        {
            var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, double>(transID);
            var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
            var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
            var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
            if (result.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
            {
                var hashedKey = result.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                var data = EncryptionService.getDecryptedAssetData(result.data.Data, dataDecryptionKey);
                var asset = JsonConvert.DeserializeObject<TestRequisitionAsset>(data);
                //get encrypted file from ipfs
                string encryptedFileData = _bigChainDbService.GetTextFromIPFS(asset.AttachedFile.Data);
                string fileData = EncryptionService.getDecryptedAssetData(encryptedFileData,dataDecryptionKey);

                byte[] fileBytes = Convert.FromBase64String(fileData);
                return File(fileBytes, asset.AttachedFile.Type, asset.AttachedFile.Name);
            }
            return new EmptyResult();
        }

        public IActionResult GetResultFile(string transID)
        {
            var result = _bigChainDbService.GetMetaDataAndAssetFromTransactionId<string, double>(transID);
            var doctorSignPrivateKey = HttpContext.Session.GetString(Globals.currentDSPriK);
            var doctorAgreePrivateKey = HttpContext.Session.GetString(Globals.currentDAPriK);
            var doctorSignPublicKey = EncryptionService.getSignPublicKeyStringFromPrivate(doctorSignPrivateKey);
            if (result.metadata.AccessList.Keys.Contains(doctorSignPublicKey))
            {
                var hashedKey = result.metadata.AccessList[doctorSignPublicKey];
                var dataDecryptionKey = EncryptionService.getDecryptedEncryptionKey(hashedKey, doctorAgreePrivateKey);
                var encryptedFile = _bigChainDbService.GetAssociatedTestResultFile(result.id);
                if (encryptedFile != "")
                {
                    var data = EncryptionService.getDecryptedAssetData(encryptedFile, dataDecryptionKey);
                    var asset = JsonConvert.DeserializeObject<FileData>(data);
                    //get encrypted file from ipfs
                    string encryptedFileData = _bigChainDbService.GetTextFromIPFS(asset.Data);
                    string fileData = EncryptionService.getDecryptedAssetData(encryptedFileData, dataDecryptionKey);

                    byte[] fileBytes = Convert.FromBase64String(fileData);
                    return File(fileBytes, asset.Type, asset.Name);
                }
            }
            return new EmptyResult();
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return View();
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
