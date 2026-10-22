namespace OpenPerch.App.Core
{
    public interface IParamSink
    {
        void Set(string name, float value);

        bool TryGet(string name, out float value);
    }
}
