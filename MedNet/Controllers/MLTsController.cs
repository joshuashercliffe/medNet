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
                return RedirectToAction("Home");
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

        public IActionResult Home()
        {
            // DEBUG: need to fix
            ViewBag.DoctorName = HttpContext.Session.GetString(Globals.currentUserName);
            if (HttpContext.Session.GetString(Globals.currentDSPriK) == null
                || HttpContext.Session.GetString(Globals.currentDAPriK) == null)
                return RedirectToAction("Login");
            else
                return View();
        }
    }
}
