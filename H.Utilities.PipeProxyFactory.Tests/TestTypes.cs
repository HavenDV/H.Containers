using System;

namespace H.Utilities.Tests
{
    public interface ISimpleEventClass
    {
        event EventHandler<int> Event1;

        void RaiseEvent1();
        int Method1(int input);
        string Method2(string input);
    }

    public class SimpleEventClass : ISimpleEventClass
    {
        public event EventHandler<int>? Event1;

        public void RaiseEvent1()
        {
            Event1?.Invoke(this, 777);
        }

        public int Method1(int input)
        {
            return 321 + input;
        }

        public string Method2(string input)
        {
            return $"Hello, input = {input}";
        }
    }
}
