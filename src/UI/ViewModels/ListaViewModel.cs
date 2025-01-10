using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ListaCompras.Core.Models;

namespace ListaCompras.UI.ViewModels
{
    public class ListaViewModel : INotifyPropertyChanged
    {
        private int _id;
        private string _nome;
        private decimal _valorTotal;
        private int _quantidadeItens;
        private DateTime _dataAtualizacao;
        private bool _isConcluida;
        private List<ItemModel> _itens;

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

        public decimal ValorTotal
        {
            get => _valorTotal;
            set
            {
                if (_valorTotal != value)
                {
                    _valorTotal = value;
                    OnPropertyChanged();
                }
            }
        }

        public int QuantidadeItens
        {
            get => _quantidadeItens;
            set
            {
                if (_quantidadeItens != value)
                {
                    _quantidadeItens = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime DataAtualizacao
        {
            get => _dataAtualizacao;
            set
            {
                if (_dataAtualizacao != value)
                {
                    _dataAtualizacao = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsConcluida
        {
            get => _isConcluida;
            set
            {
                if (_isConcluida != value)
                {
                    _isConcluida = value;
                    OnPropertyChanged();
                }
            }
        }

        public List<ItemModel> Itens
        {
            get => _itens;
            set
            {
                if (_itens != value)
                {
                    _itens = value;
                    OnPropertyChanged();
                    QuantidadeItens = _itens?.Count ?? 0;
                    CalcularValorTotal();
                }
            }
        }

        private void CalcularValorTotal()
        {
            ValorTotal = 0;
            if (_itens != null)
            {
                foreach (var item in _itens)
                {
                    ValorTotal += item.Preco * item.Quantidade;
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}