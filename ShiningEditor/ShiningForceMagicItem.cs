using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceMagicItem
    {
        #region - Class Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion

        #region - Class Constructors -
        public ShiningForceMagicItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForceMagicItem()
            : this ("", "")
        {

        }
        #endregion
    }
}
