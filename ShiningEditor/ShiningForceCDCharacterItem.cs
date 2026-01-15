using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiningEditor
{
    public class ShiningForceCDCharacterItem
    {
        #region - Properties -
        public string Name { get; set; }
        public int BaseOffset { get; set; }
        public string FaceClassOffset { get; set; }
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
        #endregion

        #region - Constructors -
        public ShiningForceCDCharacterItem(string name,
            int baseOffset,
            string faceClassOffset,
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
            string[] magicOffset)
        {
            Name = name;
            BaseOffset = baseOffset;
            FaceClassOffset = faceClassOffset;
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
            PresentMPOffset = presentMPOffset;
            MaximumHPOffset = maximumHPOffset;
            MaximumMPOffset = maximumMPOffset;
            ItemsOffset = itemsOffset;
            MagicOffset = magicOffset;
        }

        public ShiningForceCDCharacterItem()
            : this(string.Empty,
                  0,
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
                  string.Empty,
                  new string[] { },
                  new string[] { })
        {

        }
        #endregion
    }
}
