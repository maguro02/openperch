namespace OpenPerch.App.Core
{
    /// <summary>
    /// Keys of the UI strings in <c>StreamingAssets/i18n/{lang}.json</c>. Code refers to UI strings only through these
    /// constants, and every constant must have an entry in each locale file.
    /// </summary>
    public static class L10nKeys
    {
        /// <summary>Dialog button that confirms.</summary>
        public const string CommonOk = "common.ok";

        /// <summary>Dialog button that closes the dialog without acting.</summary>
        public const string CommonCancel = "common.cancel";

        /// <summary>Tray menu: submenu listing the avatars, each checked while it is shown.</summary>
        public const string MenuAvatars = "menu.avatars";

        /// <summary>Context menu: opens the radial expression menu.</summary>
        public const string MenuExpressions = "menu.expressions";

        /// <summary>Context menu: opens the hand gesture choices.</summary>
        public const string MenuGestures = "menu.gestures";

        /// <summary>Tray and context menu: opens the pomodoro controls.</summary>
        public const string MenuPomodoro = "menu.pomodoro";

        /// <summary>Context menu: changes the display height of the avatar.</summary>
        public const string MenuDisplaySize = "menu.displaySize";

        /// <summary>Context menu: hides the avatar the menu was opened on.</summary>
        public const string MenuHideAvatar = "menu.hideAvatar";

        /// <summary>Tray and context menu: submenu that picks the monitor the avatars are shown on.</summary>
        public const string MenuMonitor = "menu.monitor";

        /// <summary>Tray and context menu: opens the settings window.</summary>
        public const string MenuSettings = "menu.settings";

        /// <summary>Tray and context menu: quits the app.</summary>
        public const string MenuQuit = "menu.quit";

        /// <summary>Radial expression menu: returns from a submenu to its parent.</summary>
        public const string MenuBack = "menu.back";

        /// <summary>Getting-started panel: title.</summary>
        public const string StartTitle = "start.title";

        /// <summary>Getting-started panel: step 1, adding the exporter to the avatar project with VCC.</summary>
        public const string StartStep1 = "start.step1";

        /// <summary>Getting-started panel: step 2, exporting the avatar from Unity.</summary>
        public const string StartStep2 = "start.step2";

        /// <summary>Getting-started panel: step 3, importing the exported file or dropping it on the panel.</summary>
        public const string StartStep3 = "start.step3";

        /// <summary>Getting-started panel: button that opens the guide.</summary>
        public const string StartOpenGuide = "start.openGuide";

        /// <summary>First-run hints: title.</summary>
        public const string HintsTitle = "hints.title";

        /// <summary>First-run hints: right-clicking the avatar opens the menu.</summary>
        public const string HintsRightClick = "hints.rightClick";

        /// <summary>First-run hints: dragging picks the avatar up and releasing throws it.</summary>
        public const string HintsDrag = "hints.drag";

        /// <summary>First-run hints: an avatar put on a window walks and sits there.</summary>
        public const string HintsWindowTop = "hints.windowTop";

        /// <summary>First-run hints: holding Alt makes clicks pass through the avatar.</summary>
        public const string HintsAltPassThrough = "hints.altPassThrough";

        /// <summary>First-run hints: settings and quitting are on the notification area icon.</summary>
        public const string HintsTray = "hints.tray";

        /// <summary>Settings window: title.</summary>
        public const string SettingsTitle = "settings.title";

        /// <summary>Settings window: library section.</summary>
        public const string SettingsLibrary = "settings.library";

        /// <summary>Settings window: avatar section (display height and compositing mode).</summary>
        public const string SettingsAvatar = "settings.avatar";

        /// <summary>Settings window: behavior section.</summary>
        public const string SettingsBehavior = "settings.behavior";

        /// <summary>Settings window: pomodoro section.</summary>
        public const string SettingsPomodoro = "settings.pomodoro";

        /// <summary>Settings window: notifications section.</summary>
        public const string SettingsNotifications = "settings.notifications";

        /// <summary>Settings window: language choice.</summary>
        public const string SettingsLanguage = "settings.language";

        /// <summary>Settings window: language option that follows the Windows display language.</summary>
        public const string SettingsLanguageSystem = "settings.languageSystem";

        /// <summary>Settings window: frame rate limit.</summary>
        public const string SettingsFpsCap = "settings.fpsCap";

        /// <summary>Settings window: toggle for starting the app when Windows starts.</summary>
        public const string SettingsAutostart = "settings.autostart";

        /// <summary>Settings window: toggle for the update check.</summary>
        public const string SettingsCheckUpdates = "settings.checkUpdates";

        /// <summary>Settings window: data section (storage use, data folder, deleting everything).</summary>
        public const string SettingsData = "settings.data";

        /// <summary>Settings window: licenses section.</summary>
        public const string SettingsLicenses = "settings.licenses";

        /// <summary>Settings window: diagnostics section.</summary>
        public const string SettingsDiagnostics = "settings.diagnostics";

        /// <summary>Parameter panel: title.</summary>
        public const string ParamsTitle = "params.title";

        /// <summary>Parameter panel: folded group of the parameters that are not in the expression menu.</summary>
        public const string ParamsInternal = "params.internal";

        /// <summary>Library: button that imports a package.</summary>
        public const string LibraryImport = "library.import";

        /// <summary>Library: button that removes a package.</summary>
        public const string LibraryRemove = "library.remove";

        /// <summary>Library: shown when removing a package that an avatar on the desktop still uses.</summary>
        public const string LibraryInUse = "library.inUse";

        /// <summary>Library: status of an entry whose file is missing or damaged.</summary>
        public const string LibraryNeedsReimport = "library.needsReimport";

        /// <summary>Import confirmation for a file downloaded or copied from another computer: title.</summary>
        public const string ImportUntrustedTitle = "import.untrustedTitle";

        /// <summary>
        /// Import confirmation for a file downloaded or copied from another computer: body. Placeholder: <c>{name}</c>, the
        /// package's display name.
        /// </summary>
        public const string ImportUntrustedBody = "import.untrustedBody";

        /// <summary>Import choice for a new export of an avatar already in the library: title.</summary>
        public const string ImportLineageTitle = "import.lineageTitle";

        /// <summary>
        /// Import choice for a new export of an avatar already in the library: body. Placeholder: <c>{name}</c>, the incoming
        /// package's display name.
        /// </summary>
        public const string ImportLineageBody = "import.lineageBody";

        /// <summary>Import choice: replaces the library entry as an update and keeps its settings.</summary>
        public const string ImportLineageReplace = "import.lineageReplace";

        /// <summary>Import choice: adds the file as a separate avatar.</summary>
        public const string ImportLineageAddNew = "import.lineageAddNew";

        /// <summary>Shown when importing and loading are off because the app runs as administrator.</summary>
        public const string ImportElevated = "import.elevated";

        /// <summary>Shown when an import fails. Placeholder: <c>{code}</c>, the message code of the failure.</summary>
        public const string ImportFailed = "import.failed";

        /// <summary>Pomodoro controls: starts the timer.</summary>
        public const string PomodoroStart = "pomodoro.start";

        /// <summary>Pomodoro controls: pauses the timer.</summary>
        public const string PomodoroPause = "pomodoro.pause";

        /// <summary>Pomodoro controls: resumes the paused timer.</summary>
        public const string PomodoroResume = "pomodoro.resume";

        /// <summary>Pomodoro controls: ends the current phase early.</summary>
        public const string PomodoroSkip = "pomodoro.skip";

        /// <summary>Pomodoro controls: stops the timer.</summary>
        public const string PomodoroStop = "pomodoro.stop";

        /// <summary>Pomodoro status: work phase.</summary>
        public const string PomodoroWorking = "pomodoro.working";

        /// <summary>Pomodoro status: short break.</summary>
        public const string PomodoroShortBreak = "pomodoro.shortBreak";

        /// <summary>Pomodoro status: long break.</summary>
        public const string PomodoroLongBreak = "pomodoro.longBreak";

        /// <summary>Pomodoro notice: the work phase has ended.</summary>
        public const string PomodoroWorkEnded = "pomodoro.workEnded";

        /// <summary>Pomodoro notice: the break has ended.</summary>
        public const string PomodoroBreakEnded = "pomodoro.breakEnded";

        /// <summary>
        /// Pomodoro notice shown late, after being held back while the avatars were hidden. Placeholder: <c>{minutes}</c>,
        /// whole minutes since the notice was due.
        /// </summary>
        public const string PomodoroEndedAgo = "pomodoro.endedAgo";

        /// <summary>Speech balloon shown while no motion pack has been imported.</summary>
        public const string BalloonNoMotions = "balloon.noMotions";
    }
}
