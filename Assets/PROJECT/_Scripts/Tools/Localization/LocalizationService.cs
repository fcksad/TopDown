using System;
using UnityEngine;
using UnityEngine.Localization.Settings;
using Zenject;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.Localization;

namespace Localization
{
    [Serializable]
    public class LocalizedName
    {
        [SerializeField] private LocalizedString _localizedString;
        public string Name { get; private set; }

        private LocalizedString.ChangeHandler _internalHandler;
        private Action<string> _externalHandler;

        public void Init(Action<string> onChanged = null)
        {
            _externalHandler = onChanged;
            _internalHandler = new LocalizedString.ChangeHandler(OnLocalizedStringChanged);
            _localizedString.StringChanged += _internalHandler;
            _localizedString.RefreshString();
        }

        public void Dispose()
        {
            if (_internalHandler != null)
            {
                _localizedString.StringChanged -= _internalHandler;
                _internalHandler = null;
                _externalHandler = null;
            }
        }

        private void OnLocalizedStringChanged(string value)
        {
            Name = value;
            _externalHandler?.Invoke(value);
        }
    }

    public class LocalizationService : ILocalizationService, IInitializable, IDisposable
    {
        public event Action OnLanguageChangedEvent;

        private ISaveService _saveService;

        [Inject]
        public LocalizationService(ISaveService saveService)
        {
            _saveService = saveService;
        }

        public void Initialize()
        {
            LoadLanguage();
        }

        public void Dispose()
        {
            OnLanguageChangedEvent = null;
        }

        public async Task SetLanguage(string localeCode)
        {
            var locales = LocalizationSettings.AvailableLocales.Locales;
            var targetLocale = locales.Find(locale => locale.Identifier.Code == localeCode);

            if (targetLocale != null)
            {
                LocalizationSettings.SelectedLocale = targetLocale;
                OnLanguageChangedEvent?.Invoke();
            }

            await LocalizationSettings.InitializationOperation.Task;
            SaveLanguage(localeCode);
        }

        public string GetLocalizedString(string key, string tableName = "DefaultTable") //todo
        {
            var table = LocalizationSettings.StringDatabase.GetTable(tableName);

            if (table == null)
            {
                Debug.LogError($"Table {tableName} not found!");
                return key;
            }

            var entry = table.GetEntry(key);

            if (entry == null)
            {
                Debug.LogWarning($"Key {key} not found in table {tableName}!");
                return key;
            }

            return entry.GetLocalizedString();
        }

        public List<string> GetAvailableLanguages()
        {
            var languages = new List<string>();

            var locales = LocalizationSettings.AvailableLocales.Locales;
            foreach (var locale in locales)
            {
                languages.Add(locale.Identifier.Code); 
            }

            return languages;
        }

        public string CurrentLanguageCode => LocalizationSettings.SelectedLocale.Identifier.Code;
        private void SaveLanguage(string localeCode) => _saveService.SettingsData.LocalizationData.Localization = localeCode;
        private async void LoadLanguage() => await SetLanguage(_saveService.SettingsData.LocalizationData.Localization);
    }
}
