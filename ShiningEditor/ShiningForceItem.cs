using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceItem
    {
        #region - Class Fields -
        private string name;
        private string id;
        #endregion

        #region - Class Properties -
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string ID
        {
            get { return id; }
            set { id = value; }
        }
        #endregion

        #region - Class Constructors -
        public ShiningForceItem(string name, string id)
        {
            Name = name;
            ID = id;
        }

        public ShiningForceItem() 
            : this ("", "")
        {

        }
        #endregion
    }
}
