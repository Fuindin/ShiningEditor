using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceCharacterItem
    {
        #region - Class Fields -
        private string name;
        private string levelLoc;
        private string attackLoc;
        private string defenseLoc;
        private string agilityLoc;
        private string moveLoc;
        private string experienceLoc;
        private string currentHPLoc;
        private string maxHPLoc;
        private string currentMPLoc;
        private string maxMPLoc;
        private string[] itemsLocs;
        private string[] magicLocs;
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
        public string AttackLoc
        {
            get { return attackLoc; }
            set { attackLoc = value; }
        }
        public string DefenseLoc
        {
            get { return defenseLoc; }
            set { defenseLoc = value; }
        }
        public string AgilityLoc
        {
            get { return agilityLoc; }
            set { agilityLoc = value; }
        }
        public string MoveLoc
        {
            get { return moveLoc; }
            set { moveLoc = value; }
        }
        public string ExperienceLoc
        {
            get { return experienceLoc; }
            set { experienceLoc = value; }
        }
        public string CurrentHPLoc
        {
            get { return currentHPLoc; }
            set { currentHPLoc = value; }
        }
        public string MaxHPLoc
        {
            get { return maxHPLoc; }
            set { maxHPLoc = value; }
        }
        public string CurrentMPLoc
        {
            get { return currentMPLoc; }
            set { currentMPLoc = value; }
        }
        public string MaxMPLoc
        {
            get { return maxMPLoc; }
            set { maxMPLoc = value; }
        }
        public string[] ItemsLocs
        {
            get { return itemsLocs; }
            set { itemsLocs = value; }
        }
        public string[] MagicLocs
        {
            get { return magicLocs; }
            set { magicLocs = value; }
        }
        #endregion

        #region - Class Constructors -
        public ShiningForceCharacterItem(string name,
            string levelLoc,
            string attackLoc,
            string defenseLoc,
            string agilityLoc,
            string moveLoc,
            string experienceLoc,            
            string maxHPLoc,
            string currentHPLoc,
            string currentMPLoc,
            string maxMPLoc,
            string[] itemsLocs,
            string[] magicLocs)
        {
            Name = name;
            LevelLoc = levelLoc;
            AttackLoc = attackLoc;
            DefenseLoc = defenseLoc;
            AgilityLoc = agilityLoc;
            MoveLoc = moveLoc;
            ExperienceLoc = experienceLoc;
            CurrentHPLoc = currentHPLoc;
            MaxHPLoc = maxHPLoc;
            CurrentMPLoc = currentMPLoc;
            MaxMPLoc = maxMPLoc;
            ItemsLocs = itemsLocs;
            MagicLocs = magicLocs;
        }

        public ShiningForceCharacterItem() 
            : this("",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                "",
                new string[] { },
                new string[] { })
        {

        }
        #endregion
    }
}
