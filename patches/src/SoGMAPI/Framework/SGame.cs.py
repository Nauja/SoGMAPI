from __future__ import annotations
from patches import ActionList, SMAPI_PROJECTS, ReplaceText, RemoveRegex


def build() -> ActionList | None:
    actions = ActionList()

    for a, b in (
        (
            """namespace SoGModdingAPI.Framework
{""",
            """namespace SoGModdingAPI.Framework
{
    internal class LocalizedContentManager : ContentManager
    {
        public static IDictionary<string, string> localizedAssetNames => new Dictionary<string, string>();

        public readonly CultureInfo CurrentCulture;
        public LanguageCode GetCurrentLanguage() { return LanguageCode.en; }

        public enum LanguageCode
        {
            en
        }

        public static LanguageCode CurrentLanguageCode = LanguageCode.en;

        protected LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory, CultureInfo currentCulture)
            : base(serviceProvider, rootDirectory)
        {
            // init
            this.CurrentCulture = currentCulture;
        }

        public LocalizedContentManager(IServiceProvider serviceProvider, string rootDirectory) : this(serviceProvider, rootDirectory, CultureInfo.CurrentCulture)
        {
        }

        public virtual T Load<T>(string assetName, LanguageCode language)
        {
            return base.Load<T>(assetName);
        }

        public virtual T LoadBase<T>(string assetName)
        {
            return base.Load<T>(assetName);
        }

        public string LanguageCodeString(LanguageCode language)
        {
            return "en"; // @todo
        }

        public virtual LocalizedContentManager CreateTemporary()
        {
            return this;
        }

    }""",
        ),
        (
            """using SoG;""",
            """using SoGModdingAPI.Framework.ContentManagers;
using Microsoft.Xna.Framework.Content;""",
        ),
    ):
        actions.add(ReplaceText("", a, b))

    return actions
