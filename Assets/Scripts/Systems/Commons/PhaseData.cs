[System.Serializable]
public class PhaseData
{
    public int Day;
    public PhaseType Phase;

    public PhaseData()
    {
        Day = 1;
        Phase = PhaseType.Preparation;
    }
}