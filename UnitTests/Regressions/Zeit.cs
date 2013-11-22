namespace UnitTests.Regressions.reftype
{
    public struct Zeit
    {
        public Zeit(int minuten)
        {
            Minuten = minuten;
        }

        public readonly int Minuten;
    }
}