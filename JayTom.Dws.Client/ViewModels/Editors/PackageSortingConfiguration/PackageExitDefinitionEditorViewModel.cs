using System;
using Prism.Mvvm;
using System.Linq;
using Prism.Commands;
using System.Windows;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using JayTom.Dws.Client.Models.PackageSorting;
using JayTom.Dws.Data.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig;
using JayTom.Dws.Domain.Repository.LocalConf.PackageSortingConfig.ConnectionParams;

namespace JayTom.Dws.Client.ViewModels.Editors.PackageSortingConfiguration
{

    public class PackageExitDefinitionEditorViewModel : BindableBase
    {
        private readonly ICommunicationConnectionConfigRepository _communicationConnectionConfigRepository;
        private string _identifier = string.Empty;
        private long _id;
        private string _exitName = string.Empty;
        private ExitType _type = ExitType.PackageExit;
        private bool _isActive = true;
        private string _remarks = string.Empty;
        private bool _isOk;
        private string _exceptionContent = string.Empty;

        private ObservableCollection<ExitTypeInfoModel> _exitTypeItems = new()
        {
            new ExitTypeInfoModel()
            {
                Name = "异常格口",
                Value = ExitType.AbnormalExit
            },
            new ExitTypeInfoModel()
            {
                Name = "包裹格口",
                Value = ExitType.PackageExit
            },
            new ExitTypeInfoModel()
            {
                Name = "备用格口",
                Value = ExitType.ReservedExit
            },
        };

        private ExitTypeInfoModel _selectExitType = new();
        private ObservableCollection<CommunicationConnectionItemInfoModel> _communicationConnectionItems = new();
        private CommunicationConnectionItemInfoModel _selectConnectionItem = new();
        private long _communicationConnectionId;
        private Visibility _reservedExitVisibility = Visibility.Collapsed;
        private ObservableCollection<PackageExitDefinitionItemInfoModel> _packageExitDefinitionItems = new();
        private PackageExitDefinitionItemInfoModel _selectExitDefinitionInfo = new();

        public PackageExitDefinitionEditorViewModel(ICommunicationConnectionConfigRepository communicationConnectionConfigRepository)
        {
            _communicationConnectionConfigRepository = communicationConnectionConfigRepository;
        }

        public ObservableCollection<CommunicationConnectionItemInfoModel> CommunicationConnectionItems
        {
            get => _communicationConnectionItems;
            set => SetProperty(ref _communicationConnectionItems, value);
        }

        public CommunicationConnectionItemInfoModel SelectConnectionItem
        {
            get => _selectConnectionItem;
            set => SetProperty(ref _selectConnectionItem, value);
        }

        public ObservableCollection<PackageExitDefinitionItemInfoModel> PackageExitDefinitionItems
        {
            get => _packageExitDefinitionItems;
            set => SetProperty(ref _packageExitDefinitionItems, value);
        }

        public PackageExitDefinitionItemInfoModel SelectExitDefinitionInfo
        {
            get => _selectExitDefinitionInfo;
            set => SetProperty(ref _selectExitDefinitionInfo, value);
        }

        /// <summary>
        /// 窗口标识
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set => SetProperty(ref _identifier, value);
        }

        /// <summary>
        /// Id
        /// </summary>
        public long Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        /// <summary>
        /// 格口名称
        /// </summary>
        public string ExitName
        {
            get => _exitName;
            set => SetProperty(ref _exitName, value);
        }

        /// <summary>
        /// 格口类型
        /// </summary>
        public ExitType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        /// <summary>
        /// 连接Id
        /// </summary>
        public long CommunicationConnectionId
        {
            get => _communicationConnectionId;
            set => SetProperty(ref _communicationConnectionId, value);
        }

        /// <summary>
        /// 选中格口
        /// </summary>
        public ExitTypeInfoModel SelectExitType
        {
            get => _selectExitType;
            set => SetProperty(ref _selectExitType, value);
        }

        /// <summary>
        /// 是否生效
        /// </summary>
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remarks
        {
            get => _remarks;
            set => SetProperty(ref _remarks, value);
        }

        public Visibility MainExitVisibility
        {
            get => _reservedExitVisibility;
            set => SetProperty(ref _reservedExitVisibility, value);
        }

        public ObservableCollection<ExitTypeInfoModel> ExitTypeItems
        {
            get => _exitTypeItems;
            set => SetProperty(ref _exitTypeItems, value);
        }

        /// <summary>
        /// 异常内容
        /// </summary>
        public string ExceptionContent
        {
            get => _exceptionContent;
            set => SetProperty(ref _exceptionContent, value);
        }

        public bool IsOk
        {
            get => _isOk;
            set => SetProperty(ref _isOk, value);
        }

        public ICommand SaveCommand
        {
            get => new DelegateCommand(SaveDelegate);
        }

        private void SaveDelegate()
        {
            try
            {
                Type = SelectExitType.Value;
                Pitcher.Throw.ArgumentNull.WhenNullOrEmpty(ExitName, nameof(ExitName));
                Pitcher.Throw.ArgumentNull.WhenNull(Type, nameof(Type));
                Pitcher.Throw.ArgumentNull.WhenNull(SelectConnectionItem, nameof(SelectConnectionItem));
                IsOk = true;
                if (Type == ExitType.ReservedExit &&
                    SelectExitDefinitionInfo?.Id < 1)
                {
                    throw new Exception("备用格口需要关联主格口");
                }
            }
            catch (Exception e)
            {
                IsOk = false;
                ExceptionContent = e.Message;
            }

            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand CancelCommand
        {
            get => new DelegateCommand(CancelDelegate);
        }

        private void CancelDelegate()
        {
            IsOk = false;
            if (DialogHost.IsDialogOpen(Identifier))
            {
                DialogHost.Close(Identifier);
            }
        }

        public ICommand LoadedCommand
        {
            get => new DelegateCommand<object>(LoadedDelegate);
        }

        private async void LoadedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                var exitTypeInfoModel = ExitTypeItems?.FirstOrDefault(f => f.Value.Equals(Type));
                if (exitTypeInfoModel is not null)
                {
                    SelectExitType = exitTypeInfoModel;
                }
                CommunicationConnectionItems.Clear();
                var models = await _communicationConnectionConfigRepository.Select(s => s.Id > 0,
                    o => o.Id);
                var itemInfoModels = models.Select(s => new CommunicationConnectionItemInfoModel()
                {
                    Id = s.Id,
                    ConnectionName = s.ConnectionName,
                }).ToList();
                CommunicationConnectionItems.AddRange(itemInfoModels);

                SelectConnectionItem = CommunicationConnectionItems.FirstOrDefault(f => f.Id.Equals(CommunicationConnectionId)) ?? new CommunicationConnectionItemInfoModel();
            });
        }

        public ICommand ExitTypeSelectionChangedCommand
        {
            get => new DelegateCommand<object>(ExitTypeSelectionChangedDelegate);
        }

        private async void ExitTypeSelectionChangedDelegate(object obj)
        {
            await System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                MainExitVisibility = SelectExitType.Value == ExitType.ReservedExit ? Visibility.Visible : Visibility.Collapsed;
            });
        }
    }
}