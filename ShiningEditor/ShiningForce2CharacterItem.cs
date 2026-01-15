using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForce2CharacterItem
    {
        #region - Properties -
        public string Name { get; set; }
        public string LevelOffset { get; set; }
        public string AttackBaseOffset { get; set; }
        public string AttackEquipOffset { get; set; }
        public string DefenseBaseOffset { get; set; }
        public string DefenseEquipOffset { get; set; }
        public string AgilityBaseOffset { get; set; }
        public string AgilityEquipOffset { get; set; }
        public string MoveBaseOffset { get; set; }
        public string MoveEquipOffset { get; set; }
        public string ExperienceOffset { get; set; }
        public string PresentHPOffset { get; set; }
        public string MaximumHPOffset { get; set; }
        public string PresentMPOffset { get; set; }
        public string MaximumMPOffset { get; set; }
        public string[] ItemsOffset { get; set; }
        public string[] MagicOffset { get; set; }
        public string KillsOffset { get; set; }
        public string DefeatsOffset { get; set; }
        #endregion

        #region - Constructors -
        public ShiningForce2CharacterItem(string name, 
            string levelOffset,
            string attackBaseOffset,
            string attackEquipOffset,
            string defenseBaseOffset, 
            string defenseEquipOffset,
            string agilityBaseOffset, 
            string agilityEquipOffset,
            string moveBaseOffset,
            string moveEquipOffset,
            string experienceOffset,
            string presentHPOffset,
            string maximumHPOffset,
            string presentMPOffset,
            string maximumMPOffset,
            string[] itemsOffset,
            string[] magicOffset,
            string killsOffset,
            string defeatsOffset)
        {
            Name = name;
            LevelOffset = levelOffset;
            AttackBaseOffset = attackBaseOffset;
            AttackEquipOffset = attackEquipOffset;
            DefenseBaseOffset = defenseBaseOffset;
            DefenseEquipOffset = defenseEquipOffset;
            AgilityBaseOffset = agilityBaseOffset;
            AgilityEquipOffset = agilityEquipOffset;
            MoveBaseOffset = moveBaseOffset;
            MoveEquipOffset = moveEquipOffset;
            ExperienceOffset = experienceOffset;
            PresentHPOffset = presentHPOffset;
            MaximumHPOffset = maximumHPOffset;
            PresentMPOffset = presentMPOffset;
            MaximumMPOffset = maximumMPOffset;
            ItemsOffset = itemsOffset;
            MagicOffset = magicOffset;
            KillsOffset = killsOffset;
            DefeatsOffset = defeatsOffset;
        }

        public ShiningForce2CharacterItem()
            : this (string.Empty, 
                  string.Empty,
                  string.Empty,
                  string.Empty,
                  string.Empty,
                  string.Empty,
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  string.Empty, 
                  new string[] { }, 
                  new string[] { }, 
                  string.Empty, 
                  string.Empty)
        {

        }
        #endregion
    }
}
