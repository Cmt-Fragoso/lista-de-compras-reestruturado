using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ListaCompras.Core.Models;

namespace ListaCompras.UI.ViewModels
{
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }

    public class ListaViewModel : ViewModelBase
    {
        private int _id;
        private string _nome;
        private DateTime _dataAtualizacao;
        private bool _isConcluida;
        private decimal _valorTotal;
        private List<ItemModel> _itens;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Nome
        {
            get => _nome;
            set => SetField(ref _nome, value);
        }

        public DateTime DataAtualizacao
        {
            get => _dataAtualizacao;
            set => SetField(ref _dataAtualizacao, value);
        }

        public bool IsConcluida
        {
            get => _isConcluida;
            set => SetField(ref _isConcluida, value);
        }

        public decimal ValorTotal
        {
            get => _valorTotal;
            set => SetField(ref _valorTotal, value);
        }

        public List<ItemModel> Itens
        {
            get => _itens;
            set => SetField(ref _itens, value);
        }

        public int QuantidadeItens => Itens?.Count ?? 0;
    }

    public class ItemViewModel : ViewModelBase
    {
        private int _id;
        private string _nome;
        private decimal _quantidade;
        private string _unidade;
        private decimal _preco;
        private string _observacao;
        private int _categoriaId;
        private string _categoriaNome;
        private bool _isComprado;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public string Nome
        {
            get => _nome;
            set => SetField(ref _nome, value);
        }

        public decimal Quantidade
        {
            get => _quantidade;
            set => SetField(ref _quantidade, value);
        }

        public string Unidade
        {
            get => _unidade;
            set => SetField(ref _unidade, value);
        }

        public decimal Preco
        {
            get => _preco;
            set => SetField(ref _preco, value);
        }

        public string Observacao
        {
            get => _observacao;
            set => SetField(ref _observacao, value);
        }

        public int CategoriaId
        {
            get => _categoriaId;
            set => SetField(ref _categoriaId, value);
        }

        public string CategoriaNome
        {
            get => _categoriaNome;
            set => SetField(ref _categoriaNome, value);
        }

        public bool IsComprado
        {
            get => _isComprado;
            set => SetField(ref _isComprado, value);
        }

        public decimal ValorTotal => Quantidade * Preco;
    }

    public class PrecoViewModel : ViewModelBase
    {
        private int _id;
        private int _itemId;
        private decimal _valor;
        private string _local;
        private DateTime _data;
        private string _observacao;
        private decimal _variacao;

        public int Id
        {
            get => _id;
            set => SetField(ref _id, value);
        }

        public int ItemId
        {
            get => _itemId;
            set => SetField(ref _itemId, value);
        }

        public decimal Valor
        {
            get => _valor;
            set
            {
                if (SetField(ref _valor, value))
                    CalcularVariacao();
            }
        }

        public string Local
        {
            get => _local;
            set => SetField(ref _local, value);
        }

        public DateTime Data
        {
            get => _data;
            set => SetField(ref _data, value);
        }

        public string Observacao
        {
            get => _observacao;
            set => SetField(ref _observacao, value);
        }

        public decimal Variacao
        {
            get => _variacao;
            private set => SetField(ref _variacao, value);
        }

        private decimal? _valorAnterior;
        public decimal? ValorAnterior
        {
            get => _valorAnterior;
            set
            {
                if (SetField(ref _valorAnterior, value))
                    CalcularVariacao();
            }
        }

        private void CalcularVariacao()
        {
            if (ValorAnterior.HasValue && ValorAnterior.Value != 0)
            {
                Variacao = ((Valor - ValorAnterior.Value) / ValorAnterior.Value) * 100;
            }
            else
            {
                Variacao = 0;
            }
        }
    }

    public class ConfigViewModel : ViewModelBase
    {
        private bool _temaEscuro;
        private string _formatoExportacao;
        private bool _incluirEstatisticas;
        private bool _incluirGraficos;
        private string _pastaBackup;
        private bool _backupAutomatico;
        private int _intervaloBackup;
        private int _itensPerPage;

        public bool TemaEscuro
        {
            get => _temaEscuro;
            set => SetField(ref _temaEscuro, value);
        }

        public string FormatoExportacao
        {
            get => _formatoExportacao;
            set => SetField(ref _formatoExportacao, value);
        }

        public bool IncluirEstatisticas
        {
            get => _incluirEstatisticas;
            set => SetField(ref _incluirEstatisticas, value);
        }

        public bool IncluirGraficos
        {
            get => _incluirGraficos;
            set => SetField(ref _incluirGraficos, value);
        }

        public string PastaBackup
        {
            get => _pastaBackup;
            set => SetField(ref _pastaBackup, value);
        }

        public bool BackupAutomatico
        {
            get => _backupAutomatico;
            set => SetField(ref _backupAutomatico, value);
        }

        public int IntervaloBackup
        {
            get => _intervaloBackup;
            set => SetField(ref _intervaloBackup, value);
        }

        public int ItensPerPage
        {
            get => _itensPerPage;
            set => SetField(ref _itensPerPage, value);
        }
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private string _title;
        private ViewModelBase _currentView;
        private bool _isLoading;
        private string _statusMessage;
        private bool _hasNotification;
        private string _notificationMessage;

        public string Title
        {
            get => _title;
            set => SetField(ref _title, value);
        }

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetField(ref _currentView, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool HasNotification
        {
            get => _hasNotification;
            set => SetField(ref _hasNotification, value);
        }

        public string NotificationMessage
        {
            get => _notificationMessage;
            set => SetField(ref _notificationMessage, value);
        }
    }

    public class ListasViewViewModel : ViewModelBase
    {
        private List<ListaViewModel> _listas;
        private ListaViewModel _selectedLista;
        private string _searchTerm;
        private string _statusMessage;
        private bool _isLoading;

        public List<ListaViewModel> Listas
        {
            get => _listas;
            set => SetField(ref _listas, value);
        }

        public ListaViewModel SelectedLista
        {
            get => _selectedLista;
            set => SetField(ref _selectedLista, value);
        }

        public string SearchTerm
        {
            get => _searchTerm;
            set => SetField(ref _searchTerm, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetField(ref _isLoading, value);
        }
    }

    public class PrecosViewViewModel : ViewModelBase
    {
        private List<PrecoViewModel> _precos;
        private PrecoViewModel _selectedPreco;
        private ItemViewModel _currentItem;
        private DateTime _startDate;
        private DateTime _endDate;
        private string _localFilter;
        private bool _showChart;
        private string _chartType;

        public List<PrecoViewModel> Precos
        {
            get => _precos;
            set => SetField(ref _precos, value);
        }

        public PrecoViewModel SelectedPreco
        {
            get => _selectedPreco;
            set => SetField(ref _selectedPreco, value);
        }

        public ItemViewModel CurrentItem
        {
            get => _currentItem;
            set => SetField(ref _currentItem, value);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetField(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetField(ref _endDate, value);
        }

        public string LocalFilter
        {
            get => _localFilter;
            set => SetField(ref _localFilter, value);
        }

        public bool ShowChart
        {
            get => _showChart;
            set => SetField(ref _showChart, value);
        }

        public string ChartType
        {
            get => _chartType;
            set => SetField(ref _chartType, value);
        }

        public decimal? MenorPreco => Precos?.Count > 0 ? Precos.Min(p => p.Valor) : null;
        public decimal? MaiorPreco => Precos?.Count > 0 ? Precos.Max(p => p.Valor) : null;
        public decimal? PrecoMedio => Precos?.Count > 0 ? Precos.Average(p => p.Valor) : null;
        public decimal? VariacaoTotal => MenorPreco.HasValue && MaiorPreco.HasValue ? 
            ((MaiorPreco.Value - MenorPreco.Value) / MenorPreco.Value) * 100 : null;
    }

    public class BackupViewModel : ViewModelBase
    {
        private List<BackupInfo> _backups;
        private BackupInfo _selectedBackup;
        private bool _isBackupRunning;
        private int _progress;
        private string _statusMessage;

        public List<BackupInfo> Backups
        {
            get => _backups;
            set => SetField(ref _backups, value);
        }

        public BackupInfo SelectedBackup
        {
            get => _selectedBackup;
            set => SetField(ref _selectedBackup, value);
        }

        public bool IsBackupRunning
        {
            get => _isBackupRunning;
            set => SetField(ref _isBackupRunning, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetField(ref _progress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }
    }

    public class BackupInfo : ViewModelBase
    {
        private string _fileName;
        private DateTime _data;
        private long _size;
        private bool _isAutomatic;
        private string _description;

        public string FileName
        {
            get => _fileName;
            set => SetField(ref _fileName, value);
        }

        public DateTime Data
        {
            get => _data;
            set => SetField(ref _data, value);
        }

        public long Size
        {
            get => _size;
            set => SetField(ref _size, value);
        }

        public bool IsAutomatic
        {
            get => _isAutomatic;
            set => SetField(ref _isAutomatic, value);
        }

        public string Description
        {
            get => _description;
            set => SetField(ref _description, value);
        }

        public string FormattedSize
        {
            get
            {
                string[] sizes = { "B", "KB", "MB", "GB" };
                int order = 0;
                double len = Size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
        }
    }

    public class ExportViewModel : ViewModelBase
    {
        private string _selectedFormat;
        private string _outputPath;
        private bool _includeStats;
        private bool _includeCharts;
        private List<string> _selectedFields;
        private bool _isExporting;
        private int _progress;
        private string _statusMessage;

        public string SelectedFormat
        {
            get => _selectedFormat;
            set => SetField(ref _selectedFormat, value);
        }

        public string OutputPath
        {
            get => _outputPath;
            set => SetField(ref _outputPath, value);
        }

        public bool IncludeStats
        {
            get => _includeStats;
            set => SetField(ref _includeStats, value);
        }

        public bool IncludeCharts
        {
            get => _includeCharts;
            set => SetField(ref _includeCharts, value);
        }

        public List<string> SelectedFields
        {
            get => _selectedFields;
            set => SetField(ref _selectedFields, value);
        }

        public bool IsExporting
        {
            get => _isExporting;
            set => SetField(ref _isExporting, value);
        }

        public int Progress
        {
            get => _progress;
            set => SetField(ref _progress, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetField(ref _statusMessage, value);
        }
    }