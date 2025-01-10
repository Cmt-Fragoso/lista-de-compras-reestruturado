using System.Windows.Forms;
using System.ComponentModel;

namespace ListaCompras.UI.Controls
{
    public class BaseDataGrid : DataGridView
    {
        private readonly System.Windows.Forms.Timer updateTimer;

        public BaseDataGrid()
        {
            updateTimer = new System.Windows.Forms.Timer
            {
                Interval = 100
            };
            updateTimer.Tick += UpdateTimer_Tick;
        }

        public new void Sort(DataGridViewColumn column, ListSortDirection direction)
        {
            base.Sort(column, direction);
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            // Implementação do timer
        }
    }
}