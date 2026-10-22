using System;
using OpenPerch.App.Core;
using OpenPerch.App.Tests.Shared;

namespace OpenPerch.Core.Tests.Menu
{
    public static class MenuFactory
    {
        public static FakeTimeSource Clock() => new FakeTimeSource(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), 10);

        public static ExpressionMenuController Controller(RecordingParamSink sink, params ExpressionMenuPage[] pages) =>
            new ExpressionMenuController(pages, sink, Clock());

        public static ExpressionMenuPage Page(params ExpressionMenuControl[] controls) =>
            new ExpressionMenuPage { Name = "Page", Controls = controls };

        public static ExpressionMenuControl Button(string parameter, float value) =>
            new ExpressionMenuControl { Name = "Button", Type = ExpressionMenuControlType.Button, Parameter = parameter, Value = value };

        public static ExpressionMenuControl Toggle(string parameter, float value) =>
            new ExpressionMenuControl { Name = "Toggle", Type = ExpressionMenuControlType.Toggle, Parameter = parameter, Value = value };

        public static ExpressionMenuControl SubMenu(int subMenuIndex, string parameter = "", float value = 0f) =>
            new ExpressionMenuControl
            {
                Name = "SubMenu",
                Type = ExpressionMenuControlType.SubMenu,
                Parameter = parameter,
                Value = value,
                SubMenuIndex = subMenuIndex,
            };

        public static ExpressionMenuControl Puppet(ExpressionMenuControlType type, string parameter, params string[] subParameters) =>
            new ExpressionMenuControl { Name = "Puppet", Type = type, Parameter = parameter, SubParameters = subParameters };
    }
}
