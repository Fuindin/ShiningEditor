using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningCharacterItem
    {
        #region - Class Fields -
        private string name;
        private string levelLoc;
        private string expLoc;
        private string curHPLoc;
        private string maxHPLoc;
        private string curMPLoc;
        private string maxMPLoc;
        private string iqLoc;
        private string speedLoc;
        private string luckLoc;
        private string attackLoc;
        private string defLoc;
        #endregion

        #region - Class Properties -
        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public string LevelLoc
        {
            get { return levelLoc; }
            set { levelLoc = value; }
        }
        public string ExpLoc
        {
            get { return expLoc; }
            set { expLoc = value; }
        }
        public string CurHPLoc
        {
            get { return curHPLoc; }
            set { curHPLoc = value; }
        }
        public string MaxHPLoc
        {
            get { return maxHPLoc; }
            set { maxHPLoc = value; }
        }
        public string CurMPLoc
        {
            get { return curMPLoc; }
            set { curMPLoc = value; }
        }
        public string MaxMPLoc
        {
            get { return maxMPLoc; }
            set { maxMPLoc = value; }
        }
        public string IQLoc
        {
            get { return iqLoc; }
            set { iqLoc = value; }
        }
        public string SpeedLoc
        {
            get { return speedLoc; }
            set { speedLoc = value; }
        }
        public string LuckLoc
        {
            get { return luckLoc; }
            set { luckLoc = value; }
        }
        public string AttackLoc
        {
            get { return attackLoc; }
            set { attackLoc = value; }
        }
        public string DefLoc
        {
            get { return defLoc; }
            set { defLoc = value; }
        }
        #endregion

        #region - Class Constructors -
        public ShiningCharacterItem(string name, string levelLoc, string expLoc, string curHPLoc, string maxHPLoc, string curMPLoc, string maxMPLoc, string iqLoc, string speedLoc, string luckLoc, string attackLoc, string defLoc)
        {
            Name = name;
            LevelLoc = levelLoc;
            ExpLoc = expLoc;
            CurHPLoc = curHPLoc;
            MaxHPLoc = maxHPLoc;
            CurMPLoc = curMPLoc;
            MaxMPLoc = maxMPLoc;
            IQLoc = iqLoc;
            SpeedLoc = speedLoc;
            LuckLoc = luckLoc;
            AttackLoc = attackLoc;
            DefLoc = defLoc;
        }

        public ShiningCharacterItem()
            : this(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty)
        {

        }
        #endregion
    }
}
