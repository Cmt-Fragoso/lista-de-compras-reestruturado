using System.Windows.Forms;

namespace ListaCompras.UI.Controls
{
    public class PriceChart : Control
    {
        private readonly System.Windows.Forms.Timer updateTimer;
        private readonly System.Windows.Forms.Timer animationTimer;
        
        // Corrigido o método Invalidate
        public new void Invalidate()
        {
            base.Invalidate();
        }
        
        // ... resto do código
    }
}