using System.Net;
using System.Threading;
using apcurium.MK.Booking.Api.Contract.Requests.Payment;
using apcurium.MK.Booking.IBS;
using apcurium.MK.Booking.ReadModel.Query.Contract;
using apcurium.MK.Booking.Services;
using apcurium.MK.Common.Configuration;
using apcurium.MK.Common.Resources;
using ServiceStack.Common.Web;
using ServiceStack.ServiceInterface;

namespace apcurium.MK.Booking.Api.Services.Payment
{
    public class ProcessPaymentService : Service
    {
        private readonly IPaymentService _paymentService;
        private readonly IAccountDao _accountDao;
        private readonly IIBSServiceProvider _ibsServiceProvider;
        private readonly IOrderDao _orderDao;
        private readonly IServerSettings _serverSettings;

        public ProcessPaymentService(IPaymentService paymentService,
            IIBSServiceProvider ibsServiceProvider,
            IOrderDao orderDao, IAccountDao accountDao,
            IServerSettings serverSettings)
        {
            _accountDao = accountDao;
            _orderDao = orderDao;
            _paymentService = paymentService;
            _ibsServiceProvider = ibsServiceProvider;
            _serverSettings = serverSettings;
        }

        public CommitPreauthorizedPaymentResponse Post(CommitPaymentRequest request)
        {
            if (!_serverSettings.GetPaymentSettings().IsPreAuthEnabled)
            {
                // PreAutorization was not done on create order, so we do it here before processing the payment

                var orderDetail = _orderDao.FindById(request.OrderId);
                if (orderDetail == null)
                {
                    throw new HttpError(HttpStatusCode.NotFound, "Order not found");
                }

                var account = _accountDao.FindById(orderDetail.AccountId);

                var preAuthResponse = _paymentService.PreAuthorize(request.OrderId, account.Email, request.CardToken, request.Amount);
                if (!preAuthResponse.IsSuccessful)
                {
                    return new CommitPreauthorizedPaymentResponse
                    {
                        IsSuccessful = false,
                        Message = string.Format("PreAuthorization Failed: {0}", preAuthResponse.Message)
                    };
                }

                // Wait for OrderPaymentDetail to be created
                Thread.Sleep(500);
            }

            return _paymentService.CommitPayment(request.Amount, request.MeterAmount, request.TipAmount, request.CardToken, request.OrderId, request.IsNoShowFee);
        }

        public DeleteTokenizedCreditcardResponse Delete(DeleteTokenizedCreditcardRequest request)
        {
            return _paymentService.DeleteTokenizedCreditcard(request.CardToken);
        }

        public PairingResponse Post(PairingForPaymentRequest request)
        {
            var response = _paymentService.Pair(request.OrderId, request.CardToken, request.AutoTipPercentage, request.AutoTipAmount);
            if (response.IsSuccessful)
            {
                var order = _orderDao.FindById(request.OrderId);
                var ibsAccountId = _accountDao.GetIbsAccountId(order.AccountId, null);
                if (!UpdateOrderPaymentType(ibsAccountId.Value, order.IBSOrderId.Value))
                {
                    response.IsSuccessful = false;
                    _paymentService.VoidPreAuthorization(request.OrderId);
                }
            }
            return response;
        }

        private bool UpdateOrderPaymentType(int ibsAccountId, int ibsOrderId, string companyKey = null)
        {
            return _ibsServiceProvider.Booking(companyKey).UpdateOrderPaymentType(ibsAccountId, ibsOrderId, _serverSettings.ServerData.IBS.PaymentTypeCardOnFileId);
        }

        public BasePaymentResponse Post(UnpairingForPaymentRequest request)
        {
            return _paymentService.Unpair(request.OrderId);
        }
    }
}