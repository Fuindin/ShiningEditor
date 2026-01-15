using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForce2Item
    {
        #region - Properties -
        public string Name { get; set; }
        public string ID { get; set; }
        #endregion

        #region - Constructors -
        public ShiningForce2Item(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForce2Item()
            : this(string.Empty, string.Empty)
        {

        }
        #endregion
    }
}
