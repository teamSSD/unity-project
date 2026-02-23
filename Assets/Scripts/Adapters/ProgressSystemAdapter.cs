public class ProgressSystemAdapter : IPhaseProgressor
{
    public void PassPhase()
    {
        if (ProgressSystem.instance != null)
        {
            ProgressSystem.instance.PassPhase();
        }
    }
}
