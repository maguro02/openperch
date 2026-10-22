using System;
using System.Collections.Generic;

namespace OpenPerch.App.Core
{
    /// <summary>
    /// Navigates the pages of an expression menu and writes the parameters its controls drive, following the semantics of the
    /// VRChat Expressions Menu.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Page 0 is the root. Values reach the <see cref="IParamSink"/> as plain floats: converting them to the declared type of the FX
    /// parameter and clamping expression values is the sink's job. The controller itself clamps only puppet input.
    /// </para>
    /// <para>
    /// The pages are read as given and must not be modified while the controller is in use. Call <see cref="Update"/> every frame,
    /// including while the menu is hidden, so that pressed buttons return to 0 on time.
    /// </para>
    /// </remarks>
    public sealed class ExpressionMenuController
    {
        /// <summary>How long a pressed button holds its value before its parameter returns to 0.</summary>
        public const double ButtonHoldSeconds = 0.6;

        private readonly IReadOnlyList<ExpressionMenuPage> _pages;
        private readonly IParamSink _sink;
        private readonly ITimeSource _time;

        // The sub-menu control that opened each level is kept alongside the page, so leaving a level resets exactly the parameter
        // that entering it set.
        private readonly List<int> _pageStack = new List<int>();
        private readonly List<ExpressionMenuControl> _subMenuStack = new List<ExpressionMenuControl>();

        // Keyed by parameter rather than by control. The return to 0 stands for letting go of the button, which comes before
        // whatever the user does next, so a later write to the same parameter, another button's included, replaces the pending
        // return instead of being undone by it.
        private readonly Dictionary<string, double> _buttonReleaseAt = new Dictionary<string, double>();
        private readonly List<string> _released = new List<string>();

        private ExpressionMenuControl _activePuppet;

        /// <param name="pages">The menu's pages; <see cref="ExpressionMenuControl.SubMenuIndex"/> indexes into this list.</param>
        /// <param name="sink">Receives every parameter write and answers what a toggle shows.</param>
        /// <param name="time">Clock for the button hold.</param>
        public ExpressionMenuController(IReadOnlyList<ExpressionMenuPage> pages, IParamSink sink, ITimeSource time)
        {
            _pages = pages ?? throw new ArgumentNullException(nameof(pages));
            _sink = sink ?? throw new ArgumentNullException(nameof(sink));
            _time = time ?? throw new ArgumentNullException(nameof(time));
        }

        /// <summary>Index of the page being shown, or -1 when the menu has no pages.</summary>
        public int CurrentPageIndex
        {
            get
            {
                if (_pageStack.Count > 0) return _pageStack[_pageStack.Count - 1];
                return _pages.Count > 0 ? 0 : -1;
            }
        }

        /// <summary>The page being shown, or null when there is none.</summary>
        public ExpressionMenuPage CurrentPage
        {
            get
            {
                var index = CurrentPageIndex;
                return index >= 0 && index < _pages.Count ? _pages[index] : null;
            }
        }

        /// <summary>Number of sub-menus entered below the root page.</summary>
        public int Depth => _pageStack.Count;

        /// <summary>Number of controls on the current page, at most <see cref="ExpressionMenuPage.MaxControls"/>.</summary>
        public int ControlCount
        {
            get
            {
                var controls = CurrentPage?.Controls;
                return controls == null ? 0 : Math.Min(controls.Length, ExpressionMenuPage.MaxControls);
            }
        }

        /// <summary>The puppet being operated, or null. While a puppet is open, <see cref="Press"/> does nothing.</summary>
        public ExpressionMenuControl ActivePuppet => _activePuppet;

        /// <summary>
        /// Returns the control at <paramref name="index"/> on the current page, or null for an empty or out-of-range index.
        /// </summary>
        public ExpressionMenuControl GetControl(int index)
        {
            if (index < 0 || index >= ControlCount) return null;
            return CurrentPage.Controls[index];
        }

        /// <summary>
        /// Whether the toggle at <paramref name="index"/> shows as on, which is when its parameter currently equals its value,
        /// whoever wrote it. Always false for other kinds of control.
        /// </summary>
        public bool IsOn(int index)
        {
            var control = GetControl(index);
            return control != null && control.Type == ExpressionMenuControlType.Toggle && IsToggleOn(control);
        }

        /// <summary>
        /// Activates the control at <paramref name="index"/>. A button writes its value and returns the parameter to 0 after
        /// <see cref="ButtonHoldSeconds"/>; a toggle switches between its value and 0; a sub-menu writes its value and opens its page;
        /// a puppet opens with its parameter at 1.
        /// </summary>
        /// <returns>
        /// False when nothing happened: a puppet is open, the index is empty, the button or toggle has no parameter,
        /// the sub-menu's page does not exist, or the control type is unknown.
        /// </returns>
        /// <remarks>
        /// A button's return to 0 is cancelled by any write the controller makes to the same parameter before it is due, such as
        /// pressing a toggle, entering a sub-menu or moving a puppet. Pressing a button on that parameter again restarts the hold.
        /// </remarks>
        public bool Press(int index)
        {
            if (_activePuppet != null) return false;

            var control = GetControl(index);
            if (control == null) return false;

            switch (control.Type)
            {
                case ExpressionMenuControlType.Button:
                    if (string.IsNullOrEmpty(control.Parameter)) return false;
                    Write(control.Parameter, control.Value);
                    _buttonReleaseAt[control.Parameter] = _time.MonotonicSeconds + ButtonHoldSeconds;
                    return true;

                case ExpressionMenuControlType.Toggle:
                    if (string.IsNullOrEmpty(control.Parameter)) return false;
                    Write(control.Parameter, IsToggleOn(control) ? 0f : control.Value);
                    return true;

                case ExpressionMenuControlType.SubMenu:
                    if (control.SubMenuIndex < 0 || control.SubMenuIndex >= _pages.Count) return false;
                    Write(control.Parameter, control.Value);
                    _pageStack.Add(control.SubMenuIndex);
                    _subMenuStack.Add(control);
                    return true;

                case ExpressionMenuControlType.TwoAxisPuppet:
                case ExpressionMenuControlType.FourAxisPuppet:
                case ExpressionMenuControlType.RadialPuppet:
                    _activePuppet = control;
                    Write(control.Parameter, 1f);
                    return true;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Sets the open radial puppet to <paramref name="value"/>, clamped to 0..1. Ignored unless a radial puppet is open,
        /// and when the value is NaN.
        /// </summary>
        public void SetRadial(float value)
        {
            if (_activePuppet == null || _activePuppet.Type != ExpressionMenuControlType.RadialPuppet || float.IsNaN(value)) return;

            SetSubParameter(0, Clamp(value, 0f, 1f));
        }

        /// <summary>
        /// Sets the open two- or four-axis puppet from a stick position whose y axis points up. A two-axis puppet takes each axis
        /// clamped to -1..1; a four-axis puppet splits the position into up, right, down and left values in 0..1.
        /// Ignored unless such a puppet is open, and when either component is NaN.
        /// </summary>
        public void SetAxes(Vec2 axes)
        {
            if (_activePuppet == null || float.IsNaN(axes.x) || float.IsNaN(axes.y)) return;

            switch (_activePuppet.Type)
            {
                case ExpressionMenuControlType.TwoAxisPuppet:
                    SetSubParameter(0, Clamp(axes.x, -1f, 1f));
                    SetSubParameter(1, Clamp(axes.y, -1f, 1f));
                    break;

                case ExpressionMenuControlType.FourAxisPuppet:
                    SetSubParameter(0, Clamp(axes.y, 0f, 1f));
                    SetSubParameter(1, Clamp(axes.x, 0f, 1f));
                    SetSubParameter(2, Clamp(-axes.y, 0f, 1f));
                    SetSubParameter(3, Clamp(-axes.x, 0f, 1f));
                    break;
            }
        }

        /// <summary>
        /// Closes the open puppet, or otherwise leaves the current sub-menu and returns its parameter to 0. Puppet values are kept.
        /// </summary>
        /// <returns>False when already on the root page with no puppet open.</returns>
        public bool Back()
        {
            if (_activePuppet != null)
            {
                ClosePuppet();
                return true;
            }

            if (_pageStack.Count == 0) return false;

            LeaveSubMenu();
            return true;
        }

        /// <summary>
        /// Closes the open puppet and leaves every sub-menu, innermost first, as when the menu is dismissed.
        /// Puppet values are kept, and pressed buttons still return to 0 on time.
        /// </summary>
        public void Close()
        {
            if (_activePuppet != null) ClosePuppet();
            while (_pageStack.Count > 0) LeaveSubMenu();
        }

        /// <summary>
        /// Sets the parameter of every button whose hold has run out back to 0. A parameter the controller has written since the
        /// button was pressed is left as it is.
        /// </summary>
        public void Update()
        {
            if (_buttonReleaseAt.Count == 0) return;

            var now = _time.MonotonicSeconds;
            foreach (var pending in _buttonReleaseAt)
            {
                if (now >= pending.Value) _released.Add(pending.Key);
            }

            foreach (var parameter in _released) Write(parameter, 0f);
            _released.Clear();
        }

        private bool IsToggleOn(ExpressionMenuControl toggle) =>
            !string.IsNullOrEmpty(toggle.Parameter) && _sink.TryGet(toggle.Parameter, out var current) && current == toggle.Value;

        private void ClosePuppet()
        {
            var puppet = _activePuppet;
            _activePuppet = null;
            Write(puppet.Parameter, 0f);
        }

        private void LeaveSubMenu()
        {
            var top = _pageStack.Count - 1;
            var subMenu = _subMenuStack[top];
            _pageStack.RemoveAt(top);
            _subMenuStack.RemoveAt(top);
            Write(subMenu.Parameter, 0f);
        }

        private void SetSubParameter(int index, float value)
        {
            var names = _activePuppet.SubParameters;
            if (names == null || index >= names.Length) return;
            Write(names[index], value);
        }

        // Every parameter write goes through here so that each one cancels a pending button return on the same parameter.
        private void Write(string parameter, float value)
        {
            if (string.IsNullOrEmpty(parameter)) return;
            _buttonReleaseAt.Remove(parameter);
            _sink.Set(parameter, value);
        }

        // Anything not above the minimum comes out as the minimum itself, so negating a centred stick for the down and left axes
        // writes 0 rather than -0.
        private static float Clamp(float value, float min, float max) => value > min ? (value < max ? value : max) : min;
    }
}
