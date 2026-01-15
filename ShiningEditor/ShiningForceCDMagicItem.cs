using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceCDMagicItem
    {
        #region - Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion

        #region - Constructors -
        public ShiningForceCDMagicItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForceCDMagicItem()
            : this(string.Empty, string.Empty)
        {

        }
        #endregion
    }
}
