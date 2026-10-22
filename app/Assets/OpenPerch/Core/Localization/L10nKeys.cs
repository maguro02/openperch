namespace OpenPerch.App.Core
{
    /// <summary>
    /// Keys of the UI strings in <c>StreamingAssets/i18n/{lang}.json</c>. Code refers to UI strings only through these
    /// constants, and every constant must have an entry in each locale file.
    /// </summary>
    public static class L10nKeys
    {
        public const string CommonOk = "common.ok";
        public const string CommonCancel = "common.cancel";

        public const string MenuAvatars = "menu.avatars";
        public const string MenuExpressions = "menu.expressions";
        public const string MenuGestures = "menu.gestures";
        public const string MenuPomodoro = "menu.pomodoro";
        public const string MenuDisplaySize = "menu.displaySize";
        public const string MenuHideAvatar = "menu.hideAvatar";
        public const string MenuMonitor = "menu.monitor";
        public const string MenuSettings = "menu.settings";
        public const string MenuQuit = "menu.quit";
        public const string MenuBack = "menu.back";

        public const string StartTitle = "start.title";
        public const string StartStep1 = "start.step1";
        public const string StartStep2 = "start.step2";
        public const string StartStep3 = "start.step3";
        public const string StartOpenGuide = "start.openGuide";

        public const string HintsTitle = "hints.title";
        public const string HintsRightClick = "hints.rightClick";
        public const string HintsDrag = "hints.drag";
        public const string HintsWindowTop = "hints.windowTop";
        public const string HintsAltPassThrough = "hints.altPassThrough";
        public const string HintsTray = "hints.tray";

        public const string SettingsTitle = "settings.title";
        public const string SettingsLibrary = "settings.library";
        public const string SettingsAvatar = "settings.avatar";
        public const string SettingsBehavior = "settings.behavior";
        public const string SettingsPomodoro = "settings.pomodoro";
        public const string SettingsNotifications = "settings.notifications";
        public const string SettingsLanguage = "settings.language";
        public const string SettingsLanguageSystem = "settings.languageSystem";
        public const string SettingsFpsCap = "settings.fpsCap";
        public const string SettingsAutostart = "settings.autostart";
        public const string SettingsCheckUpdates = "settings.checkUpdates";
        public const string SettingsData = "settings.data";
        public const string SettingsLicenses = "settings.licenses";
        public const string SettingsDiagnostics = "settings.diagnostics";

        public const string ParamsTitle = "params.title";
        public const string ParamsInternal = "params.internal";

        public const string LibraryImport = "library.import";
        public const string LibraryRemove = "library.remove";
        public const string LibraryInUse = "library.inUse";
        public const string LibraryNeedsReimport = "library.needsReimport";

        public const string ImportUntrustedTitle = "import.untrustedTitle";

        /// <summary>Placeholder: <c>{name}</c>, the package's display name.</summary>
        public const string ImportUntrustedBody = "import.untrustedBody";

        public const string ImportLineageTitle = "import.lineageTitle";

        /// <summary>Placeholder: <c>{name}</c>, the incoming package's display name.</summary>
        public const string ImportLineageBody = "import.lineageBody";

        public const string ImportLineageReplace = "import.lineageReplace";
        public const string ImportLineageAddNew = "import.lineageAddNew";
        public const string ImportElevated = "import.elevated";

        /// <summary>Placeholder: <c>{code}</c>, the message code of the failure.</summary>
        public const string ImportFailed = "import.failed";

        public const string PomodoroStart = "pomodoro.start";
        public const string PomodoroPause = "pomodoro.pause";
        public const string PomodoroResume = "pomodoro.resume";
        public const string PomodoroSkip = "pomodoro.skip";
        public const string PomodoroStop = "pomodoro.stop";
        public const string PomodoroWorking = "pomodoro.working";
        public const string PomodoroShortBreak = "pomodoro.shortBreak";
        public const string PomodoroLongBreak = "pomodoro.longBreak";
        public const string PomodoroWorkEnded = "pomodoro.workEnded";
        public const string PomodoroBreakEnded = "pomodoro.breakEnded";

        /// <summary>Placeholder: <c>{minutes}</c>, whole minutes since a notice held back while hidden was due.</summary>
        public const string PomodoroEndedAgo = "pomodoro.endedAgo";

        public const string BalloonNoMotions = "balloon.noMotions";
    }
}
