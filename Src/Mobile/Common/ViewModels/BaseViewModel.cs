using apcurium.MK.Booking.Mobile.Extensions;
using apcurium.MK.Booking.Mobile.Messages;
using TinyMessenger;
using apcurium.MK.Common.Diagnostic;
using System.Collections.Generic;
using System;
using TinyIoC;
using System.Runtime.CompilerServices;
using Cirrious.MvvmCross.ViewModels;
using System.Windows.Input;
using System.Threading.Tasks;
using Cirrious.MvvmCross.Platform;
using System.Reactive.Disposables;
using Cirrious.CrossCore;

namespace apcurium.MK.Booking.Mobile.ViewModels
{
	public abstract class BaseViewModel : MvxViewModel 
    {
		private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

        public static Action NoAction = () => { };

        public TinyIoCContainer Container
        {
            get { return TinyIoCContainer.Current; }
        }

        protected ILogger Logger { get { return Container.Resolve<ILogger>(); } }

        public virtual void OnViewLoaded()
        {
        }

        public virtual void OnViewStarted(bool firstTime)
        {
        }

        public virtual void OnViewStopped()
        {
        }

        public virtual void OnViewUnloaded()
        {
			_subscriptions.Dispose();
        }

		protected bool ShowSubViewModel<TViewModel, TResult>(object parameterValuesObject, Action<TResult> onResult)
				where TViewModel : BaseSubViewModel<TResult>
		{
			return ShowSubViewModel<TViewModel, TResult>(parameterValuesObject.ToSimplePropertyDictionary(), onResult);
		}

		protected bool ShowSubViewModel<TViewModel, TResult>(IDictionary<string, string> parameterValues,
                                                               Action<TResult> onResult)
            where TViewModel : BaseSubViewModel<TResult>
        {
            parameterValues = parameterValues ?? new Dictionary<string, string>();

            if (parameterValues.ContainsKey("messageId"))
                throw new ArgumentException("parameterValues cannot contain an item with the key 'messageId'");

            string messageId = Guid.NewGuid().ToString();
            parameterValues["messageId"] = messageId;
            TinyMessageSubscriptionToken token = null;
            // ReSharper disable once RedundantAssignment
            token = this.Services().MessengerHub.Subscribe<SubNavigationResultMessage<TResult>>(msg =>
            {
                // ReSharper disable AccessToModifiedClosure
                if (token != null)
                    this.Services().MessengerHub.Unsubscribe
                        <SubNavigationResultMessage<TResult>>(token);
                // ReSharper restore AccessToModifiedClosure

                onResult(msg.Result);
            },
            msg => msg.MessageId == messageId);

			return ShowViewModel<TViewModel>(parameterValues);
        }

        protected AsyncCommand GetCommand(Action action)
        {
            return new AsyncCommand(action);
        }

		protected AsyncCommand GetCommand(Func<Task> action)
		{
			return new AsyncCommand(action);
		}

        protected AsyncCommand<T> GetCommand<T>(Action<T> action)
        {
            return new AsyncCommand<T>(action);
        }
		
		public ICommand CloseCommand
		{
			get
			{
				return GetCommand(() => Close(this));
			}
		}

        readonly IDictionary<string, AsyncCommand> _commands = new Dictionary<string, AsyncCommand>();
        protected AsyncCommand GetCommand(Action execute, Func<bool> canExecute, [CallerMemberName] string memberName = null)
        {
            if (memberName == null)
            {
                throw new ArgumentNullException("memberName");
            }
            return _commands.ContainsKey(memberName)
                ? _commands[memberName]
                    : (_commands[memberName] = new AsyncCommand(execute, canExecute));

        }

		protected new void RaisePropertyChanged([CallerMemberName]string whichProperty = null)
		{
			base.RaisePropertyChanged(whichProperty);
		}

		protected void ShowViewModel<TViewModel>(Dictionary<string,string> parameters = null, bool removeFromHistory = false) where TViewModel : IMvxViewModel
		{
			parameters = parameters ?? new Dictionary<string,string>();
			if (removeFromHistory)
			{
				parameters.Add("removeFromHistory", "notUsed");
			}
			base.ShowViewModel<TViewModel>(parameters);
		}

		protected TViewModel AddChild<TViewModel>(Func<TViewModel> builder)
			where TViewModel: ChildViewModel
		{
			var viewModel = builder.Invoke();
			viewModel.DisposeWith(_subscriptions);
			return viewModel;
		}

		protected TViewModel AddChild<TViewModel>()
			where TViewModel: ChildViewModel
		{
			return AddChild<TViewModel>(() => Mvx.IocConstruct<TViewModel>());
		}

		protected void Observe<T>(IObservable<T> observable, Action<T> onNext)
		{
			observable
				.Subscribe(x => InvokeOnMainThread(() => onNext(x)))
				.DisposeWith(_subscriptions);
		}


    }

}

