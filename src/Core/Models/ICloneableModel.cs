namespace ListaCompras.Core.Models
{
    /// <summary>
    /// Interface para modelos que podem ser clonados
    /// </summary>
    public interface ICloneableModel<T> where T : class
    {
        /// <summary>
        /// Cria uma cópia superficial do objeto
        /// </summary>
        T Clone();

        /// <summary>
        /// Cria uma cópia profunda do objeto
        /// </summary>
        T DeepClone();
    }
}