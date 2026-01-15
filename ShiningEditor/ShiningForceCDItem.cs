using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceCDItem
    {
        #region - Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion


        #region - Constructors -
        public ShiningForceCDItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForceCDItem()
            : this(string.Empty, string.Empty)
        {

        }
        #endregion
    }
}
