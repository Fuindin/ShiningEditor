using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForce2MagicItem
    {
        #region - Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion

        #region - Constructors -
        public ShiningForce2MagicItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForce2MagicItem()
            : this(string.Empty, string.Empty)
        {

        }
        #endregion
    }
}
