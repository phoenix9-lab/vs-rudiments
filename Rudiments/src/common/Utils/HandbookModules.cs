using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace Rudiments.Utils
{
    /// <summary>
    /// Fits the handbook to the module switches, client side.
    ///
    /// <list type="bullet">
    /// <item>Guide pages of a switched-off group are dropped.</item>
    /// <item>A paragraph in a remaining page can start with <c>[[Group,!Group]]</c>; it is kept only
    /// when every named group is on (and every <c>!</c>-named one is off), and the tag is removed.</item>
    /// <item>A link to a page, item or block that is hidden is unwrapped to its plain text.</item>
    /// <item>An item's handbook text that links to a dropped guide page is removed.</item>
    /// </list>
    /// Pages are rebuilt from assets each time the handbook opens, so this runs on fresh objects.
    /// </summary>
    public static class HandbookModules
    {
        private static readonly Regex Tag = new(@"^\[\[([A-Za-z!,]+)\]\]", RegexOptions.Compiled);
        private static readonly Regex Link = new(@"<a href=""handbook://([^""]+)"">(.*?)</a>", RegexOptions.Compiled | RegexOptions.Singleline);

        public static void Register(ICoreClientAPI capi)
        {
            var handbook = capi.ModLoader.GetModSystem<ModSystemSurvivalHandbook>();
            if (handbook == null) return;
            handbook.OnInitCustomPages += pages => Apply(capi, pages);
        }

        /// <summary>Whether a guide page (by page code) belongs to a group that is on.</summary>
        private static bool PageOn(string pageCode) => pageCode switch
        {
            "craftinginfo-flaxfiberextraction" => RudimentsModules.Fibre,
            "rudiments-ware" => RudimentsModules.WareTiers,
            "rudiments-handling" => RudimentsModules.ClayBreakage,
            "rudiments-water" => RudimentsModules.ClaySeepage,
            "rudiments-kilns" => RudimentsModules.Kilns,
            "rudiments-porcelain" => RudimentsModules.Porcelain,
            "rudiments-glaze" => RudimentsModules.Glazes,
            "rudiments-lead" => RudimentsModules.Glazes && RudimentsModSystem.LeadPoisoningEnabled,
            _ => true,
        };

        private static bool GroupOn(ICoreClientAPI capi, string name)
        {
            if (name == "LeadPoisoning") return RudimentsModules.Glazes && RudimentsModSystem.LeadPoisoningEnabled;
            bool? on = RudimentsModules.IsOn(name);
            if (on == null) capi.Logger.Warning("[Rudiments] Handbook: unknown group [[{0}]] in a paragraph tag; showing it.", name);
            return on ?? true;
        }

        private static void Apply(ICoreClientAPI capi, List<GuiHandbookPage> pages)
        {
            pages.RemoveAll(p => p is GuiHandbookTextPage t && !PageOn(t.PageCode));
            DropItemTextsLinkingHiddenPages();

            foreach (GuiHandbookPage page in pages)
            {
                if (page is not GuiHandbookTextPage text || string.IsNullOrEmpty(text.Text)) continue;
                if (!IsGuidePage(text.PageCode)) continue;

                string edited = Edit(capi, text.Text);
                if (edited == text.Text) continue;
                text.Text = edited;
                text.Init(capi);   // rich text is built from Text in Init
            }
        }

        private static string Edit(ICoreClientAPI capi, string source)
        {
            var kept = new List<string>();
            foreach (string paragraph in source.Split("\n\n"))
            {
                string body = paragraph;
                Match m = Tag.Match(paragraph);
                if (m.Success)
                {
                    body = paragraph.Substring(m.Length);
                    bool ok = m.Groups[1].Value.Split(',').All(g => g.StartsWith("!") ? !GroupOn(capi, g.Substring(1)) : GroupOn(capi, g));
                    if (!ok) continue;
                }
                kept.Add(Link.Replace(body, link => LinkVisible(capi, link.Groups[1].Value) ? link.Value : link.Groups[2].Value));
            }
            return string.Join("\n\n", kept);
        }

        /// <summary>
        /// An item's handbook text ("item-handbooktext-flaxfibers") is a plain lang entry, read when
        /// its page opens. One that points to a guide page that was dropped is removed, with its
        /// title, from every loaded language, so the item page shows no dead section.
        /// </summary>
        private static void DropItemTextsLinkingHiddenPages()
        {
            foreach (ITranslationService language in Lang.AvailableLanguages.Values)
            {
                IDictionary<string, string> entries = language.GetAllEntries();   // the live table
                var dead = entries
                    .Where(e => e.Key.Contains("-handbooktext-") && e.Value.Contains("handbook://")
                        && Link.Matches(e.Value).Any(m => IsGuidePage(m.Groups[1].Value) && !PageOn(m.Groups[1].Value)))
                    .Select(e => e.Key)
                    .ToList();
                foreach (string key in dead)
                {
                    entries.Remove(key);
                    entries.Remove(key.Replace("-handbooktext-", "-handbooktitle-"));
                }
            }
        }

        private static bool IsGuidePage(string target) => target.StartsWith("rudiments-") || target == "craftinginfo-flaxfiberextraction";

        private static bool LinkVisible(ICoreClientAPI capi, string target)
        {
            if (IsGuidePage(target)) return PageOn(target);

            bool isItem = target.StartsWith("item-");
            if (!isItem && !target.StartsWith("block-")) return true;

            var code = new AssetLocation(target.Substring(target.IndexOf('-') + 1));
            CollectibleObject coll = isItem ? capi.World.GetItem(code) : capi.World.GetBlock(code);
            string group = ModuleContentSystem.GroupOf(coll);
            return group == null || ModuleContentSystem.IsOn(group);
        }
    }
}
