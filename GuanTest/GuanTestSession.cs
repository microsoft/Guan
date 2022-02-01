namespace GuanTest
{
    internal class GuanTestSession : TestSession
    {
        public GuanTestSession()
            : base("GuanTest", true, new GuanTestDispatcher())
        {
        }
    }
}
