using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace Riftborn.Characters.Runes
{
    public enum RunePageType { DEX, STR, WIS, ISP, FORT }
    [Serializable]
    public sealed class RuneSelection { public int level; public int nodeIndex; public string effectId; }
    [CreateAssetMenu(menuName = "Riftborn/Runes/Rune Page")]
    public sealed class RunePageData : ScriptableObject
    {
        [SerializeField] private RunePageType pageType;
        [SerializeField] private List<RuneSelection> selections = new();
        public RunePageType PageType => pageType;
        public IReadOnlyList<RuneSelection> Selections => selections;
    }
    [Serializable]
    public sealed class RuneController
    {
        public const int PageCount = 5, LevelsPerPage = 5, NodesPerLevel = 3;
        [SerializeField] private RunePageData equippedPage;
        private readonly Dictionary<int, RuneSelection> activeSelections = new();
        public event Action<RunePageData> PageEquipped, PageRemoved;
        public event Action<RuneSelection> RuneEffectRegistered, RuneEffectRemoved;
        public bool EquipPage(RunePageData page)
        {
            if (page == null || !ValidatePage(page)) return false;
            RemovePage(); equippedPage = page;
            foreach (var selection in page.Selections) { activeSelections[selection.level] = selection; RuneEffectRegistered?.Invoke(selection); }
            PageEquipped?.Invoke(page); return true;
        }
        public void RemovePage()
        {
            if (equippedPage == null) return;
            foreach (var selection in activeSelections.Values.ToList()) RuneEffectRemoved?.Invoke(selection);
            RunePageData removed = equippedPage; activeSelections.Clear(); equippedPage = null; PageRemoved?.Invoke(removed);
        }
        public static bool ValidatePage(RunePageData page)
        {
            if (page == null || page.Selections.Count > LevelsPerPage) return false;
            return page.Selections.All(s => s.level >= 0 && s.level < LevelsPerPage && s.nodeIndex >= 0 && s.nodeIndex < NodesPerLevel) && page.Selections.GroupBy(s => s.level).All(g => g.Count() == 1);
        }
    }
}
