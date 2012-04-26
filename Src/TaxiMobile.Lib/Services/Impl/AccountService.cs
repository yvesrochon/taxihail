﻿using System;
using TaxiMobile.Lib.Data;
using TaxiMobile.Lib.Infrastructure;
using TaxiMobile.Lib.Practices;
using TaxiMobile.Lib.Services.IBS;
using TaxiMobile.Lib.Services.Mapper;
#if MONO_DROID
using Android.Runtime;
#endif
#if MONO_TOUCH
using MonoTouch.Foundation;
#endif

namespace TaxiMobile.Lib.Services.Impl
{
    public class AccountService : BaseService<WebAccount3Service>, IAccountService
    {
        protected override string GetUrl()
        {
            return base.GetUrl() + "IWebAccount3";
        }

#if !TEST
        [Preserve]
#endif
        public AccountService()
        {
        }

        public AccountData GetAccount(string email, string password, out string error)
        {
            error = "";
            string resultError = "";
            
            AccountData data = null;
            UseService(service =>
            {

                try
                {
                    Logger.StartStopwatch("WS GetAccount : " + email.ToLower());

                    var account = service.GetWEBAccount3(userNameApp, passwordApp, 0, email, password);

                    Logger.StopStopwatch("WS GetAccount : " + email.ToLower());
                    
                    var result = new AccountData();

                    var loggedUser = ServiceLocator.Current.GetInstance<IAppContext>().LoggedUser;

                    result = new AccountMapping().ToData(loggedUser, account);

                    //var orderExisting = service.GetOrdersList(sessionId, email, password);
                    //if ((orderExisting.Error == ErrorCode.NoError) && (orderExisting.OrderInfos != null))
                    //{
                    //    orders.AddRange(orderExisting.OrderInfos);
                    //}

                    //if (orders.Count > 0)
                    //{
                    //    new OrderMapping().UpdateHistory(result, orders.ToArray(), _vehicules, _companies, _payments);
                    //}

                    result.Password = password;
                    new SettingMapper().SetSetting(result, account);
                    data = result;
                }
                catch (Exception ex)
                {
                    ServiceLocator.Current.GetInstance<ILogger>().LogError(ex);
                }
            });

            error = resultError;
            return data;
        }

        public bool ResetPassword(ResetPasswordData data)
        {
            bool isSuccess = false;
            UseService(service =>
            {
                var result = service.ChangeAccountLogin(userNameApp, passwordApp, data.OldEmail, data.OldPassword, data.Email, data.NewPassword);
                isSuccess = result == 1;
            });

            return isSuccess;
        }

        public bool CreateAccount(CreateAccountData data, out string error)
        {
            bool isSuccess = false;
            UseService(service =>
            {
                var account = new TBookAccount3();
                account.Email2 = data.Email;
                account.Title = data.Title;
                account.FirstName = data.FirstName;
                account.LastName = data.LastName;
                account.Phone = data.Phone;
                account.MobilePhone = data.Mobile;
                //account.Language = ServiceLocator.Current.GetInstance<IAppResource>().CurrentLanguage == AppLanguage.English ? "E" : "F";
                account.WEBPassword = data.Password;

                var result = service.SaveAccount3(userNameApp, passwordApp, account);
                isSuccess = result == 1;
            });
            error = null;
            return isSuccess;
        }

        public AccountData UpdateUser(AccountData data)
        {
            AccountData r = null;
            UseService(service =>
            {
                Logger.LogMessage("Update user");

                var account = service.GetWEBAccount3(userNameApp, passwordApp, 0, data.Email, data.Password);
               
                Logger.LogMessage("Update user : No error");

                var toUpdate = new AccountMapping().ToWSData(account, data);
                new SettingMapper().SetWSSetting(toUpdate, data);
                //toUpdate.Password = data.Password;
                
                var result = service.SaveAccount3(userNameApp, passwordApp, toUpdate);
                if(result == 1)
                {
                    var loggedUser = ServiceLocator.Current.GetInstance<IAppContext>().LoggedUser;
                    r = new AccountMapping().ToData(loggedUser, toUpdate);
                }

            });
            return r;
        }

        public ListItem[] GetCompaniesList()
        {
           return null;
        }
        
        public ListItem[] GetVehiclesList()
        {
            return null;
        }

        public ListItem[] GetPaymentsList()
        {
            return null;
        }
    }
}


