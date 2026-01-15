using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningItem
    {
        #region - Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion

        #region - Constructors -
        public ShiningItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningItem()
            : this("", "")
        {

        }
        #endregion
    }
}
