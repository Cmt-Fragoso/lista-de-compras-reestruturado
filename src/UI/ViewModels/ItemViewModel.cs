using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ListaCompras.UI.ViewModels
{
    public class ItemViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _nome;
        private decimal _quantidade;
        private string _unidade;
        private decimal _preco;
        private string _observacao;
        private int? _categoriaId;
        private string _categoriaNome;

        public event PropertyChangedEventHandler PropertyChanged;

        public int Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Nome
        {
            get => _nome;
            set
            {
                if (_nome != value)
                {
                    _nome = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Quantidade
        {
            get => _quantidade;
            set
            {
                if (_quantidade != value)
                {
                    _quantidade = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public string Unidade
        {
            get => _unidade;
            set
            {
                if (_unidade != value)
                {
                    _unidade = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Preco
        {
            get => _preco;
            set
            {
                if (_preco != value)
                {
                    _preco = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public string Observacao
        {
            get => _observacao;
            set
            {
                if (_observacao != value)
                {
                    _observacao = value;
                    OnPropertyChanged();
                }
            }
        }

        public int? CategoriaId
        {
            get => _categoriaId;
            set
            {
                if (_categoriaId != value)
                {
                    _categoriaId = value;
                    OnPropertyChanged();
                }
            }
        }

        public string CategoriaNome
        {
            get => _categoriaNome;
            set
            {
                if (_categoriaNome != value)
                {
                    _categoriaNome = value;
                    OnPropertyChanged();
                }
            }
        }

        public decimal Total => Quantidade * Preco;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static string[] UnidadesPadrao => new[]
        {
            "Un",
            "Kg",
            "g",
            "L",
            "ml",
            "Pct",
            "Cx",
            "Dz"
        };
    }
}